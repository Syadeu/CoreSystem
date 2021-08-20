using UnityEngine;

using System;
using UnityEngine.Scripting;
using Syadeu.Mono;

namespace Syadeu.ECS
{
    [DisallowMultipleComponent]
    public class ECSPathObstacleComponent : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Object obj;

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
    }

    //[Obsolete("임시")]
    //public sealed class ECSPathObstacleAttribute : AttributeBase
    //{
    //}
    //[Preserve, Obsolete("임시")]
    //internal sealed class ECSPathObstacleProcessor : AttributeProcessor<ECSPathObstacleAttribute>,
    //    IAttributeOnProxy
    //{
    //    public void OnProxyCreated(AttributeBase attribute, Entity<IEntity> entity, RecycleableMonobehaviour monoObj)
    //    {
    //        ECSPathObstacleComponent component = monoObj.GetComponent<ECSPathObstacleComponent>();
    //        if (component == null) return;

    //        component.enabled = true;
    //    }
    //    public void OnProxyRemoved(AttributeBase attribute, Entity<IEntity> entity, RecycleableMonobehaviour monoObj)
    //    {
    //        ECSPathObstacleComponent component = monoObj.GetComponent<ECSPathObstacleComponent>();
    //        if (component == null) return;

    //        component.enabled = false;
    //    }
    //}
}