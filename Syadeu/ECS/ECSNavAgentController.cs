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
        [SerializeField] private Transform target;
        [SerializeField] private int typeID;

        private int id;

        private void Start()
        {
            

            id = ECSPathAgentSystem.RegisterPathfinder(transform, typeID);
            ECSPathAgentSystem.SchedulePath(id, target.position, -1);

            ECSPathMeshSystem.AddLink(new Vector3(-9.68f, 2.705f, -11.72f),
                new Vector3(-9.96f, 0, -13.68f), typeID, -1, true, 1);
        }

        private void Update()
        {
            //ECSPathAgentSystem.SchedulePath(id, target.position, 1);
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