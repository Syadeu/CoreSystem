using UnityEngine;

namespace Syadeu.ECS
{
    [DisallowMultipleComponent]
    public class ECSPathObstacleComponent : MonoBehaviour
    {
        [SerializeField] private Object obj;

        private int ID { get; set; }

        private void OnEnable()
        {
            ID = ECSPathMeshSystem.AddObstacle(obj);
        }
        private void OnDisable()
        {
            if (ID == -1) return;

            ECSPathMeshSystem.RemoveObstacle(ID);
            ID = -1;
        }
    }
}