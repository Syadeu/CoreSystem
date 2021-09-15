using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Syadeu.Presentation.Actor
{
    public sealed class ActorControllerAttribute : ActorAttributeBase
    {
        [Header("General")]
        [JsonProperty(Order = 0, PropertyName = "SetAliveOnCreated")]
        internal bool m_SetAliveOnCreated = true;
        [JsonProperty(Order = 1, PropertyName = "OnEventReceived")]
        private Reference<ParamAction<IActorEvent>>[] m_OnEventReceived = Array.Empty<Reference<ParamAction<IActorEvent>>>();

        [Header("Provider")]
        [JsonProperty(Order = 2, PropertyName = "Providers")] 
        internal Reference<ActorProviderBase>[] m_Providers = Array.Empty<Reference<ActorProviderBase>>();

        [JsonIgnore] internal InstanceArray<ActorProviderBase> m_InstanceProviders;

        [JsonIgnore] internal Dictionary<IActorProvider, Type[]> m_ProviderAcceptsOnly;

        public void PostEvent<TEvent>(TEvent ev)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
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
            m_OnEventReceived.Execute(ev);

            if (ev is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        private void ExecutePostEvent<TEvent>(IActorProvider provider, TEvent ev)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
        {
            Type evType = TypeHelper.TypeOf<TEvent>.Type;
            bool executable;
            if (m_ProviderAcceptsOnly.TryGetValue(provider, out var types))
            {
                executable = false;
                for (int i = 0; i < types.Length; i++)
                {
                    if (types[i].IsAssignableFrom(evType))
                    {
                        executable = true;
                        break;
                    }
                }
            }
            else executable = true;

            if (!executable) return;

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
            attribute.m_ProviderAcceptsOnly = new Dictionary<IActorProvider, Type[]>();
            for (int i = 0; i < attribute.m_Providers.Length; i++)
            {
                Instance<ActorProviderBase> clone = EntitySystem.CreateInstance(attribute.m_Providers[i]);
                Initialize(actor, attribute, clone.Object);
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
        private void Initialize(Entity<ActorEntity> parent, ActorControllerAttribute attribute, IActorProvider provider)
        {
            provider.Bind(parent, attribute, EventSystem, EntitySystem, EntitySystem.m_CoroutineSystem);

            if (provider.ReceiveEventOnly != null)
            {
                attribute.m_ProviderAcceptsOnly.Add(provider, provider.ReceiveEventOnly);
            }
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
