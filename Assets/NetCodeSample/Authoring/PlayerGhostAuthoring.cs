using NetCodeSample.Components;
using Unity.Entities;
using UnityEngine;

namespace NetCodeSample.Authoring
{
    public class PlayerGhostAuthoring : MonoBehaviour
    {
        public float MoveSpeed = 8f;
        public float JumpSpeed = 8f;
        public float Gravity = -25f;

        private class Baker : Baker<PlayerGhostAuthoring>
        {
            public override void Bake(PlayerGhostAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<PlayerTag>(entity);
                AddComponent<PlayerInputCommand>(entity);
                AddComponent(entity, new PlayerMoveSettings
                {
                    MoveSpeed = authoring.MoveSpeed,
                    JumpSpeed = authoring.JumpSpeed,
                    Gravity = authoring.Gravity
                });
                AddComponent<PlayerReplicatedState>(entity);
            }
        }
    }

    public class PlayerGhostPrefabAuthoring : MonoBehaviour
    {
        public GameObject PlayerPrefab;

        private class Baker : Baker<PlayerGhostPrefabAuthoring>
        {
            public override void Bake(PlayerGhostPrefabAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new PlayerGhostPrefab
                {
                    Value = GetEntity(authoring.PlayerPrefab, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}
