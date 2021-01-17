using UnityEngine;

#if UNITY_JOBS && UNITY_MATH && UNITY_BURST && UNITY_COLLECTION


namespace Syadeu.ECS
{
    public class ECSNavObstacle : MonoBehaviour
    {
        public Object obj;

        private void OnEnable()
        {
            ECSPathMeshSystem.AddObstacle(obj, gameObject.isStatic);
        }
        private void OnDisable()
        {
            ECSPathMeshSystem.RemoveObstacle(obj.GetInstanceID());
        }
    }
}

#endif