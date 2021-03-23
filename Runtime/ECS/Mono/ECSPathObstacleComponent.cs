using UnityEngine;

namespace Syadeu.ECS
{
    [DisallowMultipleComponent]
    public class ECSPathObstacleComponent : MonoBehaviour
    {
        [SerializeField] private Object obj;

        private int ID { get; set; }

        protected virtual void OnEnable()
        {
            ID = ECSPathMeshSystem.AddObstacle(obj);
        }
        protected virtual void OnDisable()
        {
            if (ID == -1) return;

            ECSPathMeshSystem.RemoveObstacle(ID);
            ID = -1;
        }

        public Bounds GetBounds()
        {
            if (obj is MeshFilter mesh)
            {
                return mesh.mesh.bounds;
            }
            else if (obj is Terrain terrain)
            {
                return terrain.terrainData.bounds;
            }
            else throw new CoreSystemException(CoreSystemExceptionFlag.ECS, $"{name}은 현재 연결된 부모 컴포넌트(Mesh Filter or Terrain)가 없습니다.")
        }
    }
}