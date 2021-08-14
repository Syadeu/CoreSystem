using Syadeu.Database;
using Syadeu.Presentation.Entities;
using UnityEngine;
using UnityEngine.AI;

namespace Syadeu.Presentation
{
    public sealed class NavMeshSystem : PresentationSystemEntity<NavMeshSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        

        public void AddBaker(NavMeshComponent component)
        {
            component.m_NavMeshData = new NavMeshData();
            NavMesh.AddNavMeshData(component.m_NavMeshData);
        }

        public void AddObstacle(Entity<IEntity> entity)
        {
            //entity.transform.prefab.GetObjectSetting().m_RefPrefab.

            CoreSystem.Logger.Unmanaged<NavMeshBuildSource>();
        }
    }

    public sealed class NavMeshComponent : MonoBehaviour
    {
        internal NavMeshData m_NavMeshData;

        private void Awake()
        {
            m_NavMeshData = new NavMeshData();
        }
        private void OnEnable()
        {
            
        }
        private void OnDisable()
        {
            
        }
    }
}
