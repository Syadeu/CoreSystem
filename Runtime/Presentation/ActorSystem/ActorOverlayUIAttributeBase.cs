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

using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using System;
using UnityEngine;

namespace Syadeu.Presentation.Actor
{
    [AttributeAcceptOnly(typeof(UIObjectEntity))]
    public abstract class ActorOverlayUIAttributeBase : AttributeBase
    {
        [Header("TriggerAction")]
        [Tooltip("Target 은 Parent Actor 입니다.")]
        [JsonProperty(Order = 1, PropertyName = "OnParentEventReceived")]
        private LogicTriggerAction[] m_OnParentEventReceived = Array.Empty<LogicTriggerAction>();

        [JsonIgnore] FixedLogicTriggerAction8 m_OnParentEventReceived8;

        /// <summary>
        /// 이 Overlay UI 의 부모 <see cref="ActorEntity"/> 입니다.
        /// </summary>
        [JsonIgnore] public Entity<ActorEntity> ParentActor { get; private set; }

        internal void UICreated(Entity<ActorEntity> parent)
        {
            ParentActor = parent;
            m_OnParentEventReceived8 = new FixedLogicTriggerAction8(m_OnParentEventReceived);

            OnUICreated(parent);
        }
        internal void EventReceived<TEvent>(TEvent ev)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
        {
            OnEventReceived(ev);

            var actor = ParentActor.As<ActorEntity, IEntityData>();
            for (int i = 0; i < m_OnParentEventReceived8.Length; i++)
            {
                m_OnParentEventReceived8[i].Execute(Parent, actor);
            }
        }

        /// <summary>
        /// 부모에 의해 이 UI 가 생성되었을 때 수행됩니다.
        /// </summary>
        /// <param name="parent"></param>
        protected virtual void OnUICreated(Entity<ActorEntity> parent) { }
        /// <summary>
        /// 부모가 받은 이벤트입니다.
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <param name="ev"></param>
        protected virtual void OnEventReceived<TEvent>(TEvent ev) where TEvent : IActorEvent
        { }

        internal static void AOTCodeGenerator<TEvent>()
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
        {
            ActorOverlayUIAttributeBase att = null;

            att.EventReceived<TEvent>(default);
            att.OnEventReceived<TEvent>(default);
        }
    }
}
