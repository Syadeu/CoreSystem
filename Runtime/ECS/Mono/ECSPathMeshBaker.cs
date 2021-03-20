using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Syadeu.ECS
{
    [DisallowMultipleComponent]
    public sealed class ECSPathMeshBaker : MonoBehaviour
    {
        [SerializeField] private Vector3 m_Center = Vector3.zero;
        [SerializeField] private Vector3 m_Size = Vector3.one;

        private IEnumerator Start()
        {
            ECSPathMeshSystem.UpdatePosition(transform.position + m_Center, m_Size, true);

            while (transform != null)
            {
                ECSPathMeshSystem.UpdatePosition(transform.position + m_Center, m_Size);

                yield return null;
            }
        }

        //public void GetPosition()
        //{
        //    ECSPathQuerySystem.ToLocation()
        //}
    }
}