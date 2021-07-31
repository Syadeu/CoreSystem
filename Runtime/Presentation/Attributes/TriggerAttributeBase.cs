using Newtonsoft.Json;
using Syadeu.Mono;
using Syadeu.Presentation.Entities;
using System;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Attributes
{
    public abstract class TriggerAttributeBase : AttributeBase { }

    [AttributeAcceptOnly(typeof(Entities.EntityBase))]
    public sealed class EntityTriggerEventAttribute : TriggerAttributeBase
    {
        [JsonIgnore] public Action OnCreated { get; set; }
        [JsonIgnore] public Action OnCreatedSync { get; set; }
        [JsonIgnore] public Action OnDestroy { get; set; }
        [JsonIgnore] public Action OnDestroySync { get; set; }

        [JsonIgnore] public Action<RecycleableMonobehaviour> OnProxyCreated { get; set; }
        [JsonIgnore] public Action<RecycleableMonobehaviour> OnProxyCreatedSync { get; set; }
        [JsonIgnore] public Action<RecycleableMonobehaviour> OnProxyRemoved { get; set; }
        [JsonIgnore] public Action<RecycleableMonobehaviour> OnProxyRemovedSync { get; set; }
    }
    [Preserve]
    internal sealed class EntityTriggerEventProccessor : AttributeProcessor<EntityTriggerEventAttribute>, IAttributeOnProxy, IAttributeOnProxyCreatedSync, IAttributeOnProxyRemovedSync
    {
        protected override void OnCreated(EntityTriggerEventAttribute attribute, IEntityData entity)
        {
            attribute.OnCreated?.Invoke();
        }
        protected override void OnCreatedSync(EntityTriggerEventAttribute attribute, IEntityData entity)
        {
            attribute.OnCreatedSync?.Invoke();
        }
        protected override void OnDestroy(EntityTriggerEventAttribute attribute, IEntityData entity)
        {
            attribute.OnDestroy?.Invoke();
        }
        protected override void OnDestroySync(EntityTriggerEventAttribute attribute, IEntityData entity)
        {
            attribute.OnDestroySync?.Invoke();
        }

        public void OnProxyCreated(AttributeBase attribute, IEntity entity, RecycleableMonobehaviour monoObj)
        {
            ((EntityTriggerEventAttribute)attribute).OnProxyCreated?.Invoke(monoObj);
        }
        public void OnProxyCreatedSync(AttributeBase attribute, IEntity entity, RecycleableMonobehaviour monoObj)
        {
            ((EntityTriggerEventAttribute)attribute).OnProxyCreatedSync?.Invoke(monoObj);
        }
        public void OnProxyRemoved(AttributeBase attribute, IEntity entity, RecycleableMonobehaviour monoObj)
        {
            ((EntityTriggerEventAttribute)attribute).OnProxyRemoved?.Invoke(monoObj);
        }
        public void OnProxyRemovedSync(AttributeBase attribute, IEntity entity, RecycleableMonobehaviour monoObj)
        {
            ((EntityTriggerEventAttribute)attribute).OnProxyRemovedSync?.Invoke(monoObj);
        }
    }

    [AttributeAcceptOnly(typeof(EntityDataBase))]
    public sealed class EntityDataTriggerEventAttribute : TriggerAttributeBase
    {
        [JsonIgnore] public Action OnCreated { get; set; }
        [JsonIgnore] public Action OnCreatedSync { get; set; }
        [JsonIgnore] public Action OnDestroy { get; set; }
        [JsonIgnore] public Action OnDestroySync { get; set; }
    }
    [Preserve]
    internal sealed class EntityDataTriggerEventProcessor : AttributeProcessor<EntityDataTriggerEventAttribute>
    {
        protected override void OnCreated(EntityDataTriggerEventAttribute attribute, IEntityData entity)
        {
            attribute.OnCreated?.Invoke();
        }
        protected override void OnCreatedSync(EntityDataTriggerEventAttribute attribute, IEntityData entity)
        {
            attribute.OnCreatedSync?.Invoke();
        }
        protected override void OnDestroy(EntityDataTriggerEventAttribute attribute, IEntityData entity)
        {
            attribute.OnDestroy?.Invoke();
        }
        protected override void OnDestroySync(EntityDataTriggerEventAttribute attribute, IEntityData entity)
        {
            attribute.OnDestroySync?.Invoke();
        }
    }
}
