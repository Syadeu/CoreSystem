using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;

using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Transforms;
using Syadeu.Extentions.EditorUtils;
using System.Collections;

namespace Syadeu.ECS
{
    [DisallowMultipleComponent]
    public class ECSNavAgentController : MonoBehaviour
    {
        [SerializeField] private Transform target;

        [SerializeField] private int agentTypeID;
        [SerializeField] private float maxTravelDistance = -1f;
        [SerializeField] private float exitPathNodeSize = -1f;

        [SerializeField] private float speed = 5f;

        [SerializeField] private float radius;

        private ECSPathAgentModule m_ECSPathAgent;

        private void Awake()
        {
            m_ECSPathAgent = new ECSPathAgentModule(transform, agentTypeID, maxTravelDistance, exitPathNodeSize, radius);
        }

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(1.5f);

            m_ECSPathAgent.MoveTo(target.position);
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            var buffer = m_ECSPathAgent.Path;
            for (int i = 0; buffer != null && i < buffer.Length; i++)
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