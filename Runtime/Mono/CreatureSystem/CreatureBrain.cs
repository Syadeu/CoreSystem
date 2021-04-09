using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.AI;

namespace Syadeu.Mono
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class CreatureBrain : RecycleableMonobehaviour, IInitialize<int>
    {
        private int m_DataIdx;
        [SerializeField] private NavMeshAgent m_NavMeshAgent;

#if UNITY_EDITOR
        [Space]
        [SerializeField] private string m_Name = null;
        [SerializeField] private string m_Description = null;
#endif
        [Space]
        [SerializeField] private float m_SamplePosDistance = .1f;

        public bool Initialized { get; private set; } = false;

        public void Initialize(int dataIdx)
        {
            m_DataIdx = dataIdx;

            Initialized = true;
        }

        #region Basic Moves

        private CoreRoutine m_MoveRoutine;

        public void MoveTo(Vector3 worldPosition)
        {
            if (NavMesh.SamplePosition(transform.position, out _, m_SamplePosDistance, m_NavMeshAgent.areaMask) &&
                NavMesh.SamplePosition(worldPosition, out _, m_SamplePosDistance, m_NavMeshAgent.areaMask))
            {
                m_NavMeshAgent.enabled = true;
                m_NavMeshAgent.SetDestination(worldPosition);
                m_MoveRoutine = CoreSystem.StartUnityUpdate(this, MoveToPointNavJob(worldPosition));
            }
            else
            {
                m_NavMeshAgent.enabled = false;
                m_MoveRoutine = CoreSystem.StartUnityUpdate(this, MoveToPointJob(worldPosition));
            }
        }
        public bool MoveToDirection(Vector3 direction)
        {
            if (NavMesh.SamplePosition(transform.position + direction, out _, m_SamplePosDistance, m_NavMeshAgent.areaMask) &&
                NavMesh.SamplePosition(transform.position, out _, m_SamplePosDistance, m_NavMeshAgent.areaMask))
            {
                if (m_MoveRoutine.IsRunning) CoreSystem.RemoveUnityUpdate(m_MoveRoutine);
                if (m_NavMeshAgent.isOnNavMesh && (
                    m_NavMeshAgent.pathStatus == NavMeshPathStatus.PathInvalid ||
                    m_NavMeshAgent.pathStatus == NavMeshPathStatus.PathPartial))
                {
                    m_NavMeshAgent.ResetPath();
                }

                m_NavMeshAgent.enabled = true;
                m_NavMeshAgent.Move(direction);

                return true;
            }
            else
            {
                m_NavMeshAgent.enabled = false;
                transform.position += direction * .6f;

                return false;
            }
        }
        private IEnumerator MoveToPointNavJob(Vector3 worldPosition)
        {
            float sqr = (worldPosition - transform.position).sqrMagnitude;

            while (sqr > .25f)
            {
                if (m_NavMeshAgent.pathStatus == NavMeshPathStatus.PathInvalid ||
                    !NavMesh.SamplePosition(transform.position, out _, m_SamplePosDistance, m_NavMeshAgent.areaMask))
                {
                    MoveTo(worldPosition);
                    yield break;
                }

                sqr = (worldPosition - transform.position).sqrMagnitude;

                yield return null;
            }
        }
        private IEnumerator MoveToPointJob(Vector3 worldPosition)
        {
            float sqr = (worldPosition - transform.position).sqrMagnitude;
            Vector3 targetAxis;

            while (sqr > .25f)
            {
                if (NavMesh.SamplePosition(worldPosition, out _, m_SamplePosDistance, m_NavMeshAgent.areaMask) &&
                    NavMesh.SamplePosition(transform.position, out _, m_SamplePosDistance, m_NavMeshAgent.areaMask))
                {
                    MoveTo(worldPosition);
                    yield break;
                }

                sqr = (worldPosition - transform.position).sqrMagnitude;
                targetAxis = (worldPosition - transform.position).normalized * m_NavMeshAgent.speed * 1.87f;

                transform.position = Vector3.Lerp(transform.position, transform.position + targetAxis, Time.deltaTime * m_NavMeshAgent.angularSpeed);

                yield return null;
            }
        }

        #endregion
    }


}
