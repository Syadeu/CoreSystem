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
    public sealed class ActorControllerAttribute : ActorAttributeBase
    {
        [Header("General")]
        [JsonProperty(Order = 0, PropertyName = "SetAliveOnCreated")]
        internal bool m_SetAliveOnCreated = true;
        [JsonProperty(Order = 1, PropertyName = "OnEventReceived")]
        internal Reference<ParamAction<IActorEvent>>[] m_OnEventReceived = Array.Empty<Reference<ParamAction<IActorEvent>>>();

        [Header("Provider")]
        [JsonProperty(Order = 2, PropertyName = "Providers")] 
        internal Reference<IActorProvider>[] m_Providers = Array.Empty<Reference<IActorProvider>>();
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
        private void Bind(WorldCanvasSystem other)
        {
            m_WorldCanvasSystem = other;
        }
        private void Bind(ActorSystem other)
        {
            m_ActorSystem = other;
        }
        protected override void OnDispose()
        {
            m_WorldCanvasSystem = null;
            m_ActorSystem = null;
        }

        protected override void OnCreated(ActorControllerAttribute attribute, EntityData<IEntityData> entity)
        {
            entity.AddComponent<ActorControllerComponent>();
            ref ActorControllerComponent component = ref entity.GetComponent<ActorControllerComponent>();

            Entity<ActorEntity> actor = entity.ToEntity<ActorEntity>();

            component.m_Parent = actor;
            component.m_InstanceProviders = new FixedInstanceList64<IActorProvider>();
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
                Instance<IActorProvider> clone = EntitySystem.CreateInstance(attribute.m_Providers[i]);
                Initialize(entity, clone.GetObject());
                component.m_InstanceProviders.Add(clone);
            }

            for (int i = 0; i < component.m_InstanceProviders.Length; i++)
            {
                ExecuteOnCreated(component.m_InstanceProviders[i].GetObject());
            }

            if (attribute.m_SetAliveOnCreated)
            {
                ActorLifetimeChangedEvent ev = new ActorLifetimeChangedEvent(ActorLifetimeChangedEvent.State.Alive);

                ActorSystem.PostEvent(actor, ev);
            }
        }
        protected override void OnDestroy(ActorControllerAttribute attribute, EntityData<IEntityData> entity)
        {
            ref ActorControllerComponent component = ref entity.GetComponent<ActorControllerComponent>();
            for (int i = 0; i < component.m_InstanceProviders.Length; i++)
            {
                //ExecuteOnDestroy(component.m_InstanceProviders[i].GetObject());
                EntitySystem.DestroyObject(component.m_InstanceProviders[i]);
            }

            entity.RemoveComponent<ActorControllerComponent>();
        }
        private void Initialize(EntityData<IEntityData> parent, IActorProvider provider)
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
                ExecuteOnProxyCreated(component.m_InstanceProviders[i].GetObject());
            }
        }
        public void OnProxyRemoved(IAttribute attribute, Entity<IEntity> entity, RecycleableMonobehaviour monoObj)
        {
            ref ActorControllerComponent component = ref entity.GetComponent<ActorControllerComponent>();
            for (int i = 0; i < component.m_InstanceProviders.Length; i++)
            {
                ExecuteOnProxyRemoved(component.m_InstanceProviders[i].GetObject());
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
