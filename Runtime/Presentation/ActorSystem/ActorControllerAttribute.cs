using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using System;
using Unity.Collections;
using UnityEngine;

namespace Syadeu.Presentation.Actor
{
    public sealed class ActorControllerAttribute : ActorAttributeBase
    {
        [Header("General")]
        [JsonProperty(Order = 0, PropertyName = "SetAliveOnCreated")]
        internal bool m_SetAliveOnCreated = true;
        [JsonProperty(Order = 1, PropertyName = "OnReceivedEvent")]
        private Reference<ParamAction<IActorEvent>>[] m_OnReceivedEvent = Array.Empty<Reference<ParamAction<IActorEvent>>>();

        [Header("Provider")]
        [JsonProperty(Order = 2, PropertyName = "Providers")] 
        internal Reference<ActorProviderBase>[] m_Providers = Array.Empty<Reference<ActorProviderBase>>();

        [JsonIgnore] internal InstanceArray<ActorProviderBase> m_InstanceProviders;

        public void PostEvent<TEvent>(TEvent ev) where TEvent : unmanaged, IActorEvent
        {
            try
            {
                ev.OnExecute(Parent.CastAs<IEntityData, ActorEntity>());
            }
            catch (Exception ex)
            {
                CoreSystem.Logger.LogError(Channel.Entity, ex);
                return;
            }

            for (int i = 0; i < m_InstanceProviders.Length; i++)
            {
                ExecutePostEvent(m_InstanceProviders[i].Object, ev);
            }
            m_OnReceivedEvent.Execute(ev);
        }
        private void ExecutePostEvent<TEvent>(IActorProvider provider, TEvent ev) where TEvent : unmanaged, IActorEvent
        {
            provider.ReceivedEvent(ev);
        }

        public Instance<T> GetProvider<T>() where T : ActorProviderBase
        {
            for (int i = 0; i < m_InstanceProviders.Length; i++)
            {
                if (m_InstanceProviders[i].Object is T) return m_InstanceProviders[i].Cast<ActorProviderBase, T>();
            }
            return Instance<T>.Empty;
        }
    }
    internal sealed class ActorControllerProcessor : AttributeProcessor<ActorControllerAttribute>
    {
        protected override void OnCreated(ActorControllerAttribute attribute, EntityData<IEntityData> entity)
        {
            Entity<ActorEntity> actor = entity.CastAs<IEntityData, ActorEntity>();

            attribute.m_InstanceProviders = new InstanceArray<ActorProviderBase>(attribute.m_Providers.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < attribute.m_Providers.Length; i++)
            {
                Instance<ActorProviderBase> clone = EntitySystem.CreateInstance(attribute.m_Providers[i]);
                Initialize(actor, clone.Object);
                attribute.m_InstanceProviders[i] = clone;
            }

            for (int i = 0; i < attribute.m_InstanceProviders.Length; i++)
            {
                ExecuteOnCreated(attribute.m_InstanceProviders[i].Object, actor);
            }

            if (attribute.m_SetAliveOnCreated)
            {
                ActorLifetimeChangedEvent ev = new ActorLifetimeChangedEvent(ActorLifetimeChangedEvent.State.Alive);
                attribute.PostEvent(ev);
            }
        }
        protected override void OnDestroy(ActorControllerAttribute attribute, EntityData<IEntityData> entity)
        {
            Entity<ActorEntity> actor = entity.CastAs<IEntityData, ActorEntity>();
            for (int i = 0; i < attribute.m_InstanceProviders.Length; i++)
            {
                ExecuteOnDestroy(attribute.m_InstanceProviders[i].Object, actor);
                EntitySystem.DestroyObject(attribute.m_InstanceProviders[i]);
            }

            attribute.m_InstanceProviders.Dispose();
        }
        private void Initialize(Entity<ActorEntity> parent, IActorProvider provider)
        {
            provider.Bind(parent, EventSystem, EntitySystem, EntitySystem.m_CoroutineSystem);
        }
        private void ExecuteOnCreated(IActorProvider provider, Entity<ActorEntity> entity)
        {
            provider.OnCreated(entity);
        }
        private void ExecuteOnDestroy(IActorProvider provider, Entity<ActorEntity> entity)
        {
            provider.OnDestroy(entity);
        }
    }
}
