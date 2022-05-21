// Copyright 2021 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Collections.Buffer.LowLevel;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Proxy;
using Syadeu.Presentation.Render;
using System;
using System.ComponentModel;
using UnityEngine;

namespace Syadeu.Presentation.Actor
{
    [DisplayName("Attribute: Actor Controller")]
    public sealed class ActorControllerAttribute : ActorAttributeBase,
        INotifyComponent<ActorControllerComponent>
    {
        [Header("General")]
        [SerializeField, JsonProperty(Order = 0, PropertyName = "SetAliveOnCreated")]
        internal bool m_SetAliveOnCreated = true;
        [SerializeField, JsonProperty(Order = 1, PropertyName = "OnEventReceived")]
        internal ArrayWrapper<Reference<ParamAction<IActorEvent>>> m_OnEventReceived = Array.Empty<Reference<ParamAction<IActorEvent>>>();

        [Header("Provider")]
        [SerializeField, JsonProperty(Order = 2, PropertyName = "Providers")] 
        internal ArrayWrapper<Reference<IActorProvider>> m_Providers = Array.Empty<Reference<IActorProvider>>();

        //protected override void OnReserve()
        //{
        //    ref ActorControllerComponent component = ref Idx.GetComponent<ActorControllerComponent>();
        //    for (int i = 0; i < component.m_InstanceProviders.Length; i++)
        //    {
        //        component.m_InstanceProviders[i].Destroy();
        //    }
        //}
    }
    internal sealed class ActorControllerProcessor : AttributeProcessor<ActorControllerAttribute>,
        IAttributeOnProxy
    {
        WorldCanvasSystem m_WorldCanvasSystem;
        ActorSystem m_ActorSystem;

        protected override void OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, WorldCanvasSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, ActorSystem>(Bind);
        }

        #region Binds

        private void Bind(WorldCanvasSystem other)
        {
            m_WorldCanvasSystem = other;
        }
        private void Bind(ActorSystem other)
        {
            m_ActorSystem = other;
        }

        #endregion

        protected override void OnDispose()
        {
            m_WorldCanvasSystem = null;
            m_ActorSystem = null;
        }

        protected unsafe override void OnCreated(ActorControllerAttribute attribute, Entity<IEntityData> entity)
        {
            ref ActorControllerComponent component = ref entity.GetComponent<ActorControllerComponent>();

            Entity<ActorEntity> actor = entity.ToEntity<ActorEntity>();

            component.m_Parent = actor;
            component.m_InstanceProviders = new FixedInstanceList64<IActorProvider>();
            component.m_OnEventReceived = attribute.m_OnEventReceived.ToFixedList64();

            Entity<IActorProvider>* tempBuffer = stackalloc Entity<IActorProvider>[attribute.m_Providers.Length];
            UnsafeFixedListWrapper<Entity<IActorProvider>> list = new UnsafeFixedListWrapper<Entity<IActorProvider>>(tempBuffer, attribute.m_Providers.Length);

            for (int i = 0; i < attribute.m_Providers.Length; i++)
            {
#if DEBUG_MODE
                if (attribute.m_Providers[i].IsEmpty() || !attribute.m_Providers[i].IsValid())
                {
                    CoreSystem.Logger.LogError(LogChannel.Entity,
                        $"Entity({actor.RawName}) has an invalid provider at {i}. This is not allowed.");
                    continue;
                }
#endif
                Entity<IActorProvider> clone = EntitySystem.CreateEntity(attribute.m_Providers[i]);
                Initialize(entity, clone.Target);

                component.m_InstanceProviders.Add(clone.Idx);
                list.AddNoResize(clone);
            }

            for (int i = 0; i < list.Length; i++)
            {
                ExecuteOnCreated(list[i].Target);
            }
            //for (int i = 0; i < component.m_InstanceProviders.Length; i++)
            //{
            //    ExecuteOnCreated(component.m_InstanceProviders[i].GetEntity<IActorProvider>().Target);
            //}

            if (attribute.m_SetAliveOnCreated)
            {
                ActorLifetimeChangedEvent ev = new ActorLifetimeChangedEvent(ActorLifetimeChangedEvent.State.Alive);

                ActorSystem.PostEvent(actor, ev);
            }
        }
        protected override void OnDestroy(ActorControllerAttribute attribute, Entity<IEntityData> entity)
        {
            ref ActorControllerComponent component = ref entity.GetComponent<ActorControllerComponent>();

            for (int i = 0; i < component.m_InstanceProviders.Length; i++)
            {
                component.m_InstanceProviders[i].GetObject<IActorProvider>().OnReserve();
            }
            for (int i = 0; i < component.m_InstanceProviders.Length; i++)
            {
                //ExecuteOnDestroy(component.m_InstanceProviders[i].GetObject());
                EntitySystem.DestroyEntity(component.m_InstanceProviders[i]);
            }
        }
        private void Initialize(Entity<IEntityData> parent, IActorProvider provider)
        {
            provider.Bind(parent);
        }
        private static void ExecuteOnCreated(IActorProvider provider)
        {
            provider.OnCreated();
        }

        public void OnProxyCreated(IAttribute attribute, Entity<IEntity> entity, RecycleableMonobehaviour monoObj)
        {
            ref ActorControllerComponent component = ref entity.GetComponent<ActorControllerComponent>();
            for (int i = 0; i < component.m_InstanceProviders.Length; i++)
            {
                ExecuteOnProxyCreated(component.m_InstanceProviders[i].GetEntity<IActorProvider>().Target);
            }
        }
        public void OnProxyRemoved(IAttribute attribute, Entity<IEntity> entity, RecycleableMonobehaviour monoObj)
        {
            ref ActorControllerComponent component = ref entity.GetComponent<ActorControllerComponent>();
            for (int i = 0; i < component.m_InstanceProviders.Length; i++)
            {
                ExecuteOnProxyRemoved(component.m_InstanceProviders[i].GetEntity<IActorProvider>().Target);
            }
        }
        private static void ExecuteOnProxyCreated(IActorProvider provider)
        {
            provider.OnProxyCreated();
        }
        private static void ExecuteOnProxyRemoved(IActorProvider provider)
        {
            provider.OnProxyRemoved();
        }
    }
}
