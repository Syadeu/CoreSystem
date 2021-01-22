using UnityEngine;

namespace Syadeu.ECS
{
    public class ECSNavObstacle : MonoBehaviour
    {
        public Object obj;

        private void OnEnable()
        {
            ECSPathMeshSystem.AddObstacle(obj);
        }
        private void OnDisable()
        {
            ECSPathMeshSystem.RemoveObstacle(obj.GetInstanceID());
        }
    }
}