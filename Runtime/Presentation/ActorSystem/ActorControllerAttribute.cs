#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Internal;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Proxy;
using Syadeu.Presentation.Render;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Collections;
using UnityEngine;

namespace Syadeu.Presentation.Actor
{
    [DisplayName("Attribute: Actor Controller")]
    public sealed class ActorControllerAttribute : ActorAttributeBase,
        INotifyComponent<ActorControllerComponent>
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
        WorldCanvasSystem m_WorldCanvasSystem;

        protected override void OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, WorldCanvasSystem>(Bind);
        }
        private void Bind(WorldCanvasSystem other)
        {
            m_WorldCanvasSystem = other;
        }
        protected override void OnDispose()
        {
            m_WorldCanvasSystem = null;
        }

        protected override void OnCreated(ActorControllerAttribute attribute, EntityData<IEntityData> entity)
        {
            entity.AddComponent<ActorControllerComponent>();
            ref ActorControllerComponent component = ref entity.GetComponent<ActorControllerComponent>();

            Entity<ActorEntity> actor = entity.As<IEntityData, ActorEntity>();

            component.m_EntitySystem = m_EntitySystem.SystemID;
            component.m_Parent = actor;
            component.m_InstanceProviders = new InstanceArray<ActorProviderBase>(attribute.m_Providers.Length, Allocator.Persistent
#if DEBUG_MODE
                , NativeArrayOptions.ClearMemory
#else
                , NativeArrayOptions.UninitializedMemory
#endif
                );
            component.m_OnEventReceived = attribute.m_OnEventReceived.ToFixedList64();
            
            for (int i = 0; i < attribute.m_Providers.Length; i++)
            {
#if DEBUG_MODE
                if (attribute.m_Providers[i].IsEmpty() || !attribute.m_Providers[i].IsValid())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"Entity({actor.RawName}) has an invalid provider at {i}. This is not allowed.");
                    continue;
                }
#endif
                Instance<ActorProviderBase> clone = EntitySystem.CreateInstance(attribute.m_Providers[i]);
                Initialize(entity, clone.GetObject());
                component.m_InstanceProviders[i] = clone;
            }

            for (int i = 0; i < component.m_InstanceProviders.Length; i++)
            {
                ExecuteOnCreated(component.m_InstanceProviders[i].GetObject(), actor);
            }

            if (attribute.m_SetAliveOnCreated)
            {
                ActorLifetimeChangedEvent ev = new ActorLifetimeChangedEvent(ActorLifetimeChangedEvent.State.Alive);
                component.PostEvent(ev);
            }
        }
        protected override void OnDestroy(ActorControllerAttribute attribute, EntityData<IEntityData> entity)
        {
            //ActorControllerComponent component = entity.GetComponent<ActorControllerComponent>();

            //Entity<ActorEntity> actor = entity.As<IEntityData, ActorEntity>();
            //for (int i = 0; i < component.m_InstanceProviders.Length; i++)
            //{
            //    ExecuteOnDestroy(component.m_InstanceProviders[i].Object, actor);
            //    EntitySystem.DestroyObject(component.m_InstanceProviders[i]);
            //}

            //component.m_InstanceProviders.Dispose();
            //component.m_OnEventReceived.Dispose();
        }
        private void Initialize(EntityData<IEntityData> parent, IActorProvider provider)
        {
            provider.Bind(parent, EventSystem, EntitySystem, EntitySystem.m_CoroutineSystem, m_WorldCanvasSystem);

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

        public void OnProxyCreated(IAttribute attribute, Entity<IEntity> entity, RecycleableMonobehaviour monoObj)
        {
            //ActorControllerAttribute att = (ActorControllerAttribute)attribute;
            ActorControllerComponent component = entity.GetComponent<ActorControllerComponent>();
            for (int i = 0; i < component.m_InstanceProviders.Length; i++)
            {
                ExecuteOnProxyCreated(component.m_InstanceProviders[i].GetObject(), monoObj);
            }
        }
        public void OnProxyRemoved(IAttribute attribute, Entity<IEntity> entity, RecycleableMonobehaviour monoObj)
        {
            //ActorControllerAttribute att = (ActorControllerAttribute)attribute;
            ActorControllerComponent component = entity.GetComponent<ActorControllerComponent>();
            for (int i = 0; i < component.m_InstanceProviders.Length; i++)
            {
                ExecuteOnProxyRemoved(component.m_InstanceProviders[i].GetObject(), monoObj);
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
}
