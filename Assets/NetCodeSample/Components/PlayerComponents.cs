using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace NetCodeSample.Components
{
    public struct PlayerTag : IComponentData { }

    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct PlayerInputCommand : IInputComponentData
    {
        [GhostField] public float Horizontal;
        [GhostField] public byte Jump;
    }

    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct PlayerMoveSettings : IComponentData
    {
        [GhostField] public float MoveSpeed;
        [GhostField] public float JumpSpeed;
        [GhostField] public float Gravity;
    }

    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct PlayerReplicatedState : IComponentData
    {
        [GhostField(Quantization = 1000)] public float3 Position;
        [GhostField(Quantization = 1000)] public float3 Velocity;
    }

    public struct LevelConfig : IComponentData
    {
        public float3 PlayerSpawn;
    }

    public struct PlatformDefinition : IBufferElementData
    {
        public float3 Center;
        public float3 Size;
    }

    public struct PlayerGhostPrefab : IComponentData
    {
        public Entity Value;
    }

    public struct PlayerSpawned : IComponentData { }

    public struct LevelInitialized : IComponentData { }
}
