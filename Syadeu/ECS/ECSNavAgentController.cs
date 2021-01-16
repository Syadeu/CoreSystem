using UnityEngine;
using UnityEngine.AI;
using Unity.Transforms;
using UnityEngine.Experimental.AI;

#if UNITY_JOBS && UNITY_MATH && UNITY_BURST && UNITY_COLLECTION

using Unity.Entities;
using Unity.Mathematics;

namespace Syadeu.ECS
{
    public struct ECSNavAgent : IComponentData
    {
        public float height;
        public float radius;
    }
    public struct ECSNavAgentTransform : IComponentData
    {
        public float3 position;
        public quaternion rotation;
    }
    public struct ECSNavAgentPathfinder : IComponentData
    {
        public int agentID;
        public AgentStatus status;
        public bool isActive;

        public int key;

        public int iteration;
        public float3 nextPosition;

        public float remainingDistance;
    }

    [DisallowMultipleComponent][RequireComponent(typeof(NavMeshAgent))]
    public class ECSNavAgentController : MonoBehaviour
    {
        [SerializeField] private NavMeshAgent m_NavMeshAgent;

        public Transform target;
        public float moveSpeed = 3;

        private EntityManager EntityManager { get; set; }
        private Entity Entity { get; set; }

        public void RequestPath(Vector3 target)
        {
            //ECSNavQuerySystem.RequestPath(Entity, target);

            
        }

        private float random()
        {
            return UnityEngine.Random.Range(1, 100);
        }
        PathfinderID id;
        private void Start()
        {
            id = ECSPathQuerySystem.RegisterPathfinder(m_NavMeshAgent);

            for (int i = 0; i < 100; i++)
            {
                ECSPathQuerySystem.SchedulePath(id,
                    new Vector3(random(), 0, random()));
            }
        }

        private void Update()
        {
            //for (int i = 0; i < 10000; i++)
            //{
            //    ECSPathQuerySystem.SchedulePath(id,
            //        new Vector3(random(), 0, random()));
            //}
        }
        //private void Start()
        //{
        //    EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        //    Entity = EntityManager.CreateEntity(
        //        typeof(ECSNavAgentTransform), typeof(ECSNavAgentPathfinder),
        //        typeof(ECSNavAgent));
        //    EntityManager.SetName(Entity, $"{name :: ECSNavAgent}");
        //    EntityManager.AddComponentData(Entity, new ECSNavAgentTransform
        //    {
        //        position = transform.position,
        //        rotation = transform.rotation
        //    });
        //    EntityManager.AddComponentData(Entity, new ECSNavAgent
        //    {
        //        height = m_NavMeshAgent.height,
        //        radius = m_NavMeshAgent.radius
        //    });
        //    EntityManager.AddComponentData(Entity, new ECSNavAgentPathfinder
        //    {
        //        status = AgentStatus.Idle,
        //        isActive = false
        //    });

        //    RequestPath(target.position);
        //}
        //private void Update()
        //{
        //    EntityManager.AddComponentData(Entity, new ECSNavAgentTransform
        //    {
        //        position = transform.position,
        //        rotation = transform.rotation
        //    });
        //}
        //private void LateUpdate()
        //{

        //}
    }
}

#endif