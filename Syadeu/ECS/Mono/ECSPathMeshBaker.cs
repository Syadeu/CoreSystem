using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Syadeu.ECS
{
    [DisallowMultipleComponent]
    public sealed class ECSPathMeshBaker : MonoManager<ECSPathMeshBaker>
    {
        private IEnumerator Start()
        {
            while (transform != null)
            {
                ECSPathMeshSystem.UpdatePosition(transform.position, transform.lossyScale);

                yield return null;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0, 1, 0, .7f);
            Gizmos.DrawWireCube(transform.position, transform.lossyScale);
        }
    }
}