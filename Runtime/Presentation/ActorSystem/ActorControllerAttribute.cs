using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Proxy;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Collections;
using UnityEngine;

namespace Syadeu.Presentation.Actor
{
    [DisplayName("Attribute: Actor Controller")]
    public sealed class ActorControllerAttribute : ActorAttributeBase
    {
        [Header("General")]
        [JsonProperty(Order = 0, PropertyName = "SetAliveOnCreated")]
        internal bool m_SetAliveOnCreated = true;
        [JsonProperty(Order = 1, PropertyName = "OnEventReceived")]
        internal Reference<ParamAction<IActorEvent>>[] m_OnEventReceived = Array.Empty<Reference<ParamAction<IActorEvent>>>();

        [Header("Provider")]
        [JsonProperty(Order = 2, PropertyName = "Providers")] 
        internal Reference<ActorProviderBase>[] m_Providers = Array.Empty<Reference<ActorProviderBase>>();
    }
    internal sealed class ActorControllerProcessor : AttributeProcessor<ActorControllerAttribute>,
        IAttributeOnProxy
    {
        protected override void OnCreated(ActorControllerAttribute attribute, EntityData<IEntityData> entity)
        {
            ActorControllerComponent component = new ActorControllerComponent();
            Entity<ActorEntity> actor = entity.As<IEntityData, ActorEntity>();

            component.m_Parent = actor;
            component.m_InstanceProviders = new InstanceArray<ActorProviderBase>(attribute.m_Providers.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            component.m_OnEventReceived = attribute.m_OnEventReceived.ToBuffer(Allocator.Persistent);
            
            for (int i = 0; i < attribute.m_Providers.Length; i++)
            {
                Instance<ActorProviderBase> clone = EntitySystem.CreateInstance(attribute.m_Providers[i]);
                Initialize(actor, clone.Object);
                component.m_InstanceProviders[i] = clone;
            }

            for (int i = 0; i < component.m_InstanceProviders.Length; i++)
            {
                ExecuteOnCreated(component.m_InstanceProviders[i].Object, actor);
            }

            if (attribute.m_SetAliveOnCreated)
            {
                ActorLifetimeChangedEvent ev = new ActorLifetimeChangedEvent(ActorLifetimeChangedEvent.State.Alive);
                component.PostEvent(ev);
            }

            entity.AddComponent(component);
        }
        protected override void OnDestroy(ActorControllerAttribute attribute, EntityData<IEntityData> entity)
        {
            ActorControllerComponent component = entity.GetComponent<ActorControllerComponent>();

            Entity<ActorEntity> actor = entity.As<IEntityData, ActorEntity>();
            for (int i = 0; i < component.m_InstanceProviders.Length; i++)
            {
                ExecuteOnDestroy(component.m_InstanceProviders[i].Object, actor);
                EntitySystem.DestroyObject(component.m_InstanceProviders[i]);
            }

            component.m_InstanceProviders.Dispose();
            component.m_OnEventReceived.Dispose();
        }
        private void Initialize(Entity<ActorEntity> parent, IActorProvider provider)
        {
            provider.Bind(parent, EventSystem, EntitySystem, EntitySystem.m_CoroutineSystem);

            //if (provider.ReceiveEventOnly != null)
            //{
            //    attribute.m_ProviderAcceptsOnly.Add(provider, provider.ReceiveEventOnly);
            //}
        }
        private static void ExecuteOnCreated(IActorProvider provider, Entity<ActorEntity> entity)
        {
            provider.OnCreated(entity);
        }
        private static void ExecuteOnDestroy(IActorProvider provider, Entity<ActorEntity> entity)
        {
            provider.OnDestroy(entity);
        }

        public void OnProxyCreated(AttributeBase attribute, Entity<IEntity> entity, RecycleableMonobehaviour monoObj)
        {
            //ActorControllerAttribute att = (ActorControllerAttribute)attribute;
            ActorControllerComponent component = entity.GetComponent<ActorControllerComponent>();
            for (int i = 0; i < component.m_InstanceProviders.Length; i++)
            {
                ExecuteOnProxyCreated(component.m_InstanceProviders[i].Object, monoObj);
            }
        }
        public void OnProxyRemoved(AttributeBase attribute, Entity<IEntity> entity, RecycleableMonobehaviour monoObj)
        {
            //ActorControllerAttribute att = (ActorControllerAttribute)attribute;
            ActorControllerComponent component = entity.GetComponent<ActorControllerComponent>();
            for (int i = 0; i < component.m_InstanceProviders.Length; i++)
            {
                ExecuteOnProxyRemoved(component.m_InstanceProviders[i].Object, monoObj);
            }
        }
        private static void ExecuteOnProxyCreated(IActorProvider provider, RecycleableMonobehaviour monoObj)
        {
            provider.OnProxyCreated(monoObj);
        }
        private static void ExecuteOnProxyRemoved(IActorProvider provider, RecycleableMonobehaviour monoObj)
        {
            provider.OnProxyRemoved(monoObj);
        }
    }

    public struct ActorControllerComponent : IEntityComponent
    {
        internal Entity<ActorEntity> m_Parent;
        internal InstanceArray<ActorProviderBase> m_InstanceProviders;
        internal ReferenceArray<Reference<ParamAction<IActorEvent>>> m_OnEventReceived;

        public void ScheduleEvent<TEvent>(TEvent ev)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
        {
            ActorSystem system = PresentationSystem<ActorSystem>.System;
            system.ScheduleEvent(PostEvent, ev);
        }
        public void PostEvent<TEvent>(TEvent ev)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
        {
            try
            {
                ev.OnExecute(m_Parent);
            }
            catch (Exception ex)
            {
                CoreSystem.Logger.LogError(Channel.Entity, ex);
                return;
            }

            if (ev is ActorLifetimeChangedEvent lifeTimeChanged)
            {
                ActorStateAttribute state = m_Parent.GetAttribute<ActorStateAttribute>();
                if (state != null)
                {
                    if (lifeTimeChanged.LifeTime == ActorLifetimeChangedEvent.State.Alive)
                    {
                        if ((state.State & ActorStateAttribute.StateInfo.Spawn) != ActorStateAttribute.StateInfo.Spawn)
                        {
                            state.State = ActorStateAttribute.StateInfo.Spawn;
                        }
                    }
                    else
                    {
                        if ((state.State & ActorStateAttribute.StateInfo.Dead) != ActorStateAttribute.StateInfo.Dead)
                        {
                            state.State = ActorStateAttribute.StateInfo.Dead;
                        }
                    }
                }
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
            //Type evType = TypeHelper.TypeOf<TEvent>.Type;
            //bool executable;
            //if (m_ProviderAcceptsOnly.TryGetValue(provider, out var types))
            //{
            //    executable = false;
            //    for (int i = 0; i < types.Length; i++)
            //    {
            //        if (types[i].IsAssignableFrom(evType))
            //        {
            //            executable = true;
            //            break;
            //        }
            //    }
            //}
            //else executable = true;

            //if (!executable) return;

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
}
