using System.Collections;

using UnityEngine;
using UnityEngine.AI;

namespace Syadeu.Presentation.Map
{
    public sealed class NavMeshComponent : MonoBehaviour
    {
        internal bool m_Registered = false;
        internal NavMeshData m_NavMeshData;
        internal NavMeshDataInstance m_Handle;

        [SerializeField] internal int m_AgentType = 0;
        [SerializeField] private Vector3 m_Center = Vector3.zero;
        [SerializeField] private Vector3 m_Size = Vector3.one;
        internal Bounds m_Bounds;

        private void Awake()
        {
            m_NavMeshData = new NavMeshData();
            m_Bounds = new Bounds(m_Center, m_Size);
        }
        private void OnEnable()
        {
            CoreSystem.StartUnityUpdate(this, Authoring(true));
        }
        private void OnDisable()
        {
            CoreSystem.StartUnityUpdate(this, Authoring(false));
        }

        private IEnumerator Authoring(bool enable)
        {
            while (!PresentationSystem<NavMeshSystem>.IsValid())
            {
                yield return null;
            }

            if (enable)
            {
                PresentationSystem<NavMeshSystem>.System.AddBaker(this);
            }
            else PresentationSystem<NavMeshSystem>.System.RemoveBaker(this);
        }
    }
}
