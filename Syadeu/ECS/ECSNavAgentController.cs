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
            id = ECSPathAgentSystem.RegisterPathfinder(transform, typeID);


            ECSPathAgentSystem.SchedulePath(id, target.position, 1);
        }

        private void Update()
        {
            ECSPathAgentSystem.SchedulePath(id, target.position, 1);

        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            
            var buffer = ECSPathAgentSystem.GetPathPositions(id);
            for (int i = 0; i < buffer.Length; i++)
            {
                Gizmos.DrawSphere(buffer[i], .25f);
                if (i + 1 < buffer.Length)
                {
                    Gizmos.DrawLine(buffer[i], buffer[i + 1]);
                }
            }
        }
    }
}

#endif