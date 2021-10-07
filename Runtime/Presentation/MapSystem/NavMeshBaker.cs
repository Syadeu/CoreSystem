using System.Collections;

using UnityEngine;
using UnityEngine.AI;

namespace Syadeu.Presentation.Map
{
    public sealed class NavMeshBaker : MonoBehaviour
    {
        internal bool m_Registered = false;
        internal NavMeshData m_NavMeshData;
        internal NavMeshDataInstance m_Handle;

        [SerializeField] internal int m_AgentType = 0;
        [SerializeField] private Vector3 m_Center = Vector3.zero;
        [SerializeField] private Vector3 m_Size = Vector3.one;

        internal Bounds Bounds => new Bounds(m_Center, m_Size);

        private void Awake()
        {
            m_NavMeshData = new NavMeshData();
        }
        private void OnEnable()
        {
            CoreSystem.StartUnityUpdate(this, Authoring(true));
        }
        private void OnDisable()
        {
            if (!CoreSystem.BlockCreateInstance)
            {
                PresentationSystem<DefaultPresentationGroup, NavMeshSystem>.System.RemoveBaker(this);
            }
        }

        private IEnumerator Authoring(bool enable)
        {
            yield return PresentationSystem<DefaultPresentationGroup, NavMeshSystem>.GetAwaiter();

            if (enable)
            {
                PresentationSystem<DefaultPresentationGroup, NavMeshSystem>.System.AddBaker(this);
            }
            else PresentationSystem<DefaultPresentationGroup, NavMeshSystem>.System.RemoveBaker(this);
        }
    }
}
