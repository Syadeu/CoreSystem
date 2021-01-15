using UnityEngine;
using UnityEngine.AI;
using Unity.Transforms;

#if UNITY_JOBS && UNITY_MATH && UNITY_BURST && UNITY_COLLECTION

using Unity.Entities;
using Unity.Mathematics;

namespace Syadeu.ECS
{
    //[GenerateAuthoringComponent]
    public struct ECSNavAgent : IComponentData
    {
        public AgentStatus status;

        public float3 position;
        public quaternion rotation;

        public float height;
        public float radius;
    }
    [DisallowMultipleComponent][RequireComponent(typeof(NavMeshAgent))]
    public class ECSNavAgentAuthoring : MonoBehaviour
    {
        [SerializeField] private NavMeshAgent m_NavMeshAgent;

        private EntityManager EntityManager { get; set; }
        private Entity Entity { get; set; }

        private void Start()
        {
            EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            Entity = EntityManager.CreateEntity(typeof(ECSNavAgent));
            EntityManager.SetName(Entity, $"{name :: ECSNavAgent}");
            EntityManager.AddComponentData(Entity, new ECSNavAgent
            {
                status = AgentStatus.Idle,

                position = transform.position,
                rotation = transform.rotation,
                
                height = m_NavMeshAgent.height,
                radius = m_NavMeshAgent.radius
            });
        }
        private void Update()
        {
            EntityManager.AddComponentData(Entity, new ECSNavAgent
            {
                status = AgentStatus.Idle,

                position = transform.position,
                rotation = transform.rotation,

                height = m_NavMeshAgent.height,
                radius = m_NavMeshAgent.radius
            });
        }
    }
}

#endif