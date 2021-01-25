using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;

using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Transforms;
using Syadeu.Extentions.EditorUtils;

namespace Syadeu.ECS
{
    [DisallowMultipleComponent]
    public class ECSNavAgentController : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private int agentTypeID;
        [SerializeField] private float speed = 5f;
        [SerializeField] private float pathNodeSize = 1f;

        private int PathfinderID { get; set; }

        public bool Pause { get; set; } = false;
        private bool IsMoving { get; set; } = false;
        private Vector3 TargetPosition { get; set; } = Vector3.zero;

        private void Awake()
        {
            PathfinderID = ECSPathAgentSystem.RegisterPathfinder(transform, agentTypeID);
        }
        private void Start()
        {
            MoveTo(target.position);
        }

        public void Move(Vector3 dir, int areaMask = -1)
        {
            if (ECSPathAgentSystem.Raycast(out _, agentTypeID, transform.position, dir, areaMask))
            {
                return;
            }


        }
        public void MoveTo(Vector3 target, int areaMask = -1)
        {
            ECSPathAgentSystem.SchedulePath(PathfinderID, target, areaMask);

            TargetPosition = target;
            IsMoving = true;
        }

        private bool IsArrived(Vector3 current, Vector3 pos)
        {
            return
                current.x - pathNodeSize <= pos.x &&
                current.x + pathNodeSize >= pos.x &&

                current.y + pathNodeSize >= pos.y &&
                current.y + pathNodeSize >= pos.y &&

                current.z + pathNodeSize >= pos.z &&
                current.z + pathNodeSize >= pos.z;
        }

        private void Update()
        {
            if (IsMoving && !Pause)
            {
                ECSPathAgentSystem.SchedulePath(PathfinderID, TargetPosition, -1);

                if (ECSPathAgentSystem.TryGetPathPositions(PathfinderID, out var paths))
                {
                    Vector3 current = transform.position;
                    Vector3 nextPos;
                    if (IsArrived(current, paths[1]))
                    {
                        nextPos = paths[2];
                    }
                    else nextPos = paths[1];
                    Vector3 dir = nextPos - transform.position;

                    Vector3 pos = transform.position + (dir.normalized * Time.deltaTime * speed);
                    $"{pos} :: {paths[0]} :: {paths[1]}".ToLog();
                    transform.position = pos;
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            
            var buffer = ECSPathAgentSystem.GetPathPositions(PathfinderID);
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