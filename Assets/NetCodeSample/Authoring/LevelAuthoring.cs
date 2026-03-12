using System;
using NetCodeSample.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace NetCodeSample.Authoring
{
    [Serializable]
    public struct PlatformEntry
    {
        public Vector3 Center;
        public Vector3 Size;
    }

    public class LevelAuthoring : MonoBehaviour
    {
        public Vector3 PlayerSpawn = new(0f, 2f, 0f);
        public PlatformEntry[] Platforms =
        {
            new PlatformEntry { Center = new Vector3(0f, -0.5f, 0f), Size = new Vector3(20f, 1f, 2f) },
            new PlatformEntry { Center = new Vector3(4f, 1.5f, 0f), Size = new Vector3(4f, 1f, 2f) },
            new PlatformEntry { Center = new Vector3(-4f, 3.5f, 0f), Size = new Vector3(4f, 1f, 2f) }
        };

        private class Baker : Baker<LevelAuthoring>
        {
            public override void Bake(LevelAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new LevelConfig
                {
                    PlayerSpawn = (float3)authoring.PlayerSpawn
                });

                var buffer = AddBuffer<PlatformDefinition>(entity);
                foreach (var platform in authoring.Platforms)
                {
                    buffer.Add(new PlatformDefinition
                    {
                        Center = (float3)platform.Center,
                        Size = (float3)platform.Size
                    });
                }
            }
        }
    }
}
