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

        private int m_Idx = -1;
        private CoreRoutine UpdateCoroutine;
        internal NavMeshData m_NavMesh;
        internal NavMeshDataInstance m_NavMeshData;

        internal Vector3 Center { get => m_Center; set => m_Center = value; }
        internal Vector3 Size { get => m_Size; set => m_Size = value; }

        //private void Start()
        //{
        //    //ECSPathMeshSystem.UpdatePosition(m_Idx, true);

        //    while (transform != null)
        //    {
        //        if (!m_DoUpdate)
        //        {
        //            yield return null;
        //            continue;
        //        }

                

        //        yield return null;
        //    }
        //}
        private IEnumerator Updater()
        {
            while (true)
            {
                if (m_Idx >= 0) ECSPathMeshSystem.UpdatePosition(m_Idx);
                yield return null;
            }
        }
        private void OnEnable()
        {
            m_Idx = ECSPathMeshSystem.AddBaker(this);
            UpdateCoroutine = CoreSystem.StartUnityUpdate(this, Updater());
        }
        private void OnDisable()
        {
            ECSPathMeshSystem.RemoveBaker(m_Idx);
            CoreSystem.RemoveUnityUpdate(UpdateCoroutine);
            m_Idx = -1;
        }

        public Bounds GetBounds() => new Bounds(m_Center, m_Size);
    }
}