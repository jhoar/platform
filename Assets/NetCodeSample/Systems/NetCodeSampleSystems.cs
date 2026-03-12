using NetCodeSample.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace NetCodeSample.Systems
{
    public class NetCodeSampleBootstrap : ClientServerBootstrap
    {
        public override bool Initialize(string defaultWorldName)
        {
            AutoConnectPort = 7979;
            return CreateDefaultClientServerWorlds();
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct LevelInitializationSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LevelConfig>();
            state.RequireForUpdate<PlatformDefinition>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.HasSingleton<LevelInitialized>())
            {
                return;
            }

            var levelEntity = SystemAPI.GetSingletonEntity<LevelConfig>();
            var platforms = state.EntityManager.GetBuffer<PlatformDefinition>(levelEntity);
            for (var i = 0; i < platforms.Length; i++)
            {
                CreatePlatform(ref state, platforms[i].Center, platforms[i].Size);
            }

            var marker = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponent<LevelInitialized>(marker);
        }

        private static void CreatePlatform(ref SystemState state, float3 center, float3 size)
        {
            var entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(entity, LocalTransform.FromPositionRotationScale(center, quaternion.identity, 1f));
            state.EntityManager.AddComponentData(entity, new PhysicsCollider
            {
                Value = Unity.Physics.BoxCollider.Create(new BoxGeometry
                {
                    Center = float3.zero,
                    Orientation = quaternion.identity,
                    Size = size,
                    BevelRadius = 0f
                })
            });
            state.EntityManager.AddComponentData(entity, new PhysicsWorldIndex { Value = 0 });
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct PlayerSpawnSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerGhostPrefab>();
            state.RequireForUpdate<LevelConfig>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var prefab = SystemAPI.GetSingleton<PlayerGhostPrefab>().Value;
            var spawnPoint = SystemAPI.GetSingleton<LevelConfig>().PlayerSpawn;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (networkId, entity) in SystemAPI
                         .Query<RefRO<NetworkId>>()
                         .WithAll<NetworkStreamInGame>()
                         .WithNone<PlayerSpawned>()
                         .WithEntityAccess())
            {
                var player = ecb.Instantiate(prefab);
                ecb.SetComponent(player, LocalTransform.FromPosition(spawnPoint));
                ecb.SetComponent(player, new GhostOwner { NetworkId = networkId.ValueRO.Value });
                ecb.SetComponent(player, new PlayerReplicatedState
                {
                    Position = spawnPoint,
                    Velocity = float3.zero
                });

                ecb.AddComponent(entity, new CommandTarget { targetEntity = player });
                ecb.AddComponent<PlayerSpawned>(entity);
            }

            ecb.Playback(state.EntityManager);
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    public partial struct PlayerInputCaptureSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var horizontal = Input.GetAxisRaw("Horizontal");
            var jump = (byte)(Input.GetKey(KeyCode.Space) ? 1 : 0);

            foreach (var input in SystemAPI.Query<RefRW<PlayerInputCommand>>().WithAll<GhostOwnerIsLocal>())
            {
                input.ValueRW.Horizontal = horizontal;
                input.ValueRW.Jump = jump;
            }
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct PlayerPredictedMovementSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (transform, velocity, input, settings) in
                     SystemAPI.Query<RefRW<LocalTransform>, RefRW<PhysicsVelocity>, RefRO<PlayerInputCommand>, RefRO<PlayerMoveSettings>>()
                         .WithAll<Simulate>())
            {
                velocity.ValueRW.Linear.x = input.ValueRO.Horizontal * settings.ValueRO.MoveSpeed;
                velocity.ValueRW.Linear.y += settings.ValueRO.Gravity * deltaTime;

                var grounded = transform.ValueRO.Position.y <= 1.01f && math.abs(velocity.ValueRO.Linear.y) < 0.05f;
                if (grounded && input.ValueRO.Jump == 1)
                {
                    velocity.ValueRW.Linear.y = settings.ValueRO.JumpSpeed;
                }
            }
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSystemGroup))]
    public partial struct PlayerReplicatedStateSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (replicated, transform, velocity) in
                     SystemAPI.Query<RefRW<PlayerReplicatedState>, RefRO<LocalTransform>, RefRO<PhysicsVelocity>>())
            {
                replicated.ValueRW.Position = transform.ValueRO.Position;
                replicated.ValueRW.Velocity = velocity.ValueRO.Linear;
            }
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerPredictedMovementSystem))]
    public partial struct PlayerReconciliationSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (transform, velocity, replicated) in SystemAPI
                         .Query<RefRW<LocalTransform>, RefRW<PhysicsVelocity>, RefRO<PlayerReplicatedState>>()
                         .WithNone<GhostOwnerIsLocal>())
            {
                transform.ValueRW.Position = replicated.ValueRO.Position;
                velocity.ValueRW.Linear = replicated.ValueRO.Velocity;
            }
        }
    }
}
