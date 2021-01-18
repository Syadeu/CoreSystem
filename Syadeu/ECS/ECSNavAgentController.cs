using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;

#if UNITY_JOBS && UNITY_MATH && UNITY_BURST && UNITY_COLLECTION

using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Transforms;

namespace Syadeu.ECS
{
    [DisallowMultipleComponent]
    public class ECSNavAgentController : MonoBehaviour
    {
        [SerializeField] private NavMeshAgent m_NavMeshAgent;
        [SerializeField] private int typeID;

        public Transform target;
        public float moveSpeed = 3;

        private EntityManager entityManager;
        private Entity Entity { get; set; }

        //public void RequestPath(Vector3 target)
        //{
        //    //ECSNavQuerySystem.RequestPath(Entity, target, 1);
        //}

        private float random()
        {
            return UnityEngine.Random.Range(1, 100);
        }
        int id;
        private void Start()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            id = ECSPathQuerySystem.RegisterPathfinder(transform, typeID);

            //for (int i = 0; i < 100; i++)
            //{
            //    ECSPathQuerySystem.SchedulePath(id,
            //        new Vector3(random(), 0, random()));
            //}

            //EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            //Entity = EntityManager.CreateEntity(
            //    typeof(ECSNavAgentTransform), typeof(ECSNavAgentPathfinder),
            //    typeof(ECSNavAgent));
            //EntityManager.SetName(Entity, $"{name:: ECSNavAgent}");
            //EntityManager.AddComponentData(Entity, new ECSNavAgentTransform
            //{
            //    position = transform.position,
            //    rotation = transform.rotation
            //});
            //EntityManager.AddComponentData(Entity, new ECSNavAgent
            //{
            //    height = m_NavMeshAgent.height,
            //    radius = m_NavMeshAgent.radius
            //});
            //EntityManager.AddComponentData(Entity, new ECSNavAgentPathfinder
            //{
            //    status = AgentStatus.Idle,
            //    isActive = false
            //});
            ECSPathQuerySystem.SchedulePath(id, target.position, 1);
        }

        private void Update()
        {
            
            //RequestPath(new Vector3(random(), 0, random()));
            //ECSPathQuerySystem.SchedulePath(id, target.position, 1);
            //for (int i = 0; i < 100; i++)
            //{
            //    ECSPathQuerySystem.SchedulePath(id,
            //        new Vector3(random(), 0, random()));
            //}
        }

        //private void OnDrawGizmos()
        //{
        //    if (!Application.isPlaying) return;

        //    var buffer = entityManager.GetBuffer<ECSPathBuffer>(id.Entity);
        //    for (int i = 0; i < buffer.Length; i++)
        //    {
        //        Gizmos.DrawSphere(buffer[i].position, .25f);
        //        if (i + 1 < buffer.Length)
        //        {
        //            Gizmos.DrawLine(buffer[i].position, buffer[i + 1].position);
        //        }
        //    }
        //}
    }
}

#endif