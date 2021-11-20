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
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.TurnTable
{
    [DisplayName("Attribute: TRPG Turn Player")]
    public sealed class TurnPlayerAttribute : AttributeBase,
        INotifyComponent<TurnPlayerComponent>
    {
        [Header("Generals")]
        [JsonProperty(Order = 0, PropertyName = "ActivateOnCreate")] internal bool m_ActivateOnCreate = true;
        [JsonProperty(Order = 1, PropertyName = "TurnSpeed")] internal float m_TurnSpeed = 0;
        [JsonProperty(Order = 2, PropertyName = "MaxActionPoint")] internal int m_MaxActionPoint = 6;

        [Space]
        [Header("Actions")]
        [JsonProperty(Order = 3, PropertyName = "OnStartTurn")]
        internal Reference<TriggerAction>[] m_OnStartTurnActions = Array.Empty<Reference<TriggerAction>>();
        [JsonProperty(Order = 4, PropertyName = "OnEndTurn")]
        internal Reference<TriggerAction>[] m_OnEndTurnActions = Array.Empty<Reference<TriggerAction>>();
        [JsonProperty(Order = 5, PropertyName = "OnResetTurn")]
        internal Reference<TriggerAction>[] m_OnResetTurnActions = Array.Empty<Reference<TriggerAction>>();
    }
    [Preserve]
    internal sealed class TurnPlayerProcessor : AttributeProcessor<TurnPlayerAttribute>
    {
        private TRPGTurnTableSystem m_TurnTableSystem;
        private readonly Queue<EntityData<IEntityData>> m_WaitForRegister = new Queue<EntityData<IEntityData>>();

        protected override void OnInitialize()
        {
            RequestSystem<TRPGIngameSystemGroup, TRPGTurnTableSystem>(Bind);
        }
        protected override void OnDispose()
        {
            m_TurnTableSystem = null;
        }

        #region Binds

        private void Bind(TRPGTurnTableSystem other)
        {
            m_TurnTableSystem = other;

            int count = m_WaitForRegister.Count;
            for (int i = 0; i < count; i++)
            {
                m_TurnTableSystem.AddPlayer(m_WaitForRegister.Dequeue());
            }
        }

        #endregion

        protected override void OnCreated(TurnPlayerAttribute attribute, EntityData<IEntityData> entity)
        {
            entity.AddComponent<TurnPlayerComponent>();
            ref var com = ref entity.GetComponent<TurnPlayerComponent>();

            com = new TurnPlayerComponent(attribute, EntitySystem.CreateHashCode());
            
            if (m_TurnTableSystem == null)
            {
                m_WaitForRegister.Enqueue(entity);
            }
            else
            {
                m_TurnTableSystem.AddPlayer(entity);
            }

            ActorStateAttribute stateAttribute = entity.GetAttribute<ActorStateAttribute>();
            if (stateAttribute == null)
            {
                //CoreSystem.Logger.LogError(Channel.Entity,
                //    $"Entity({entity.RawName}) has {nameof(TurnPlayerAttribute)} but doesn\'t have" +
                //    $"{nameof(ActorStateAttribute)}.");
                return;
            }
            stateAttribute.AddEvent(ActorStateChangedEventHandler);
        }
        protected override void OnDestroy(TurnPlayerAttribute attribute, EntityData<IEntityData> entity)
        {
            m_TurnTableSystem.RemovePlayer(entity);
            ActorStateAttribute stateAttribute = entity.GetAttribute<ActorStateAttribute>();

            if (stateAttribute == null) return;

            stateAttribute.RemoveEvent(ActorStateChangedEventHandler);
        }

        private void ActorStateChangedEventHandler(ActorStateAttribute att, 
            ActorStateAttribute.StateInfo from, ActorStateAttribute.StateInfo to)
        {
            if ((to & ActorStateAttribute.StateInfo.Dead) == ActorStateAttribute.StateInfo.Dead)
            {
                m_TurnTableSystem.RemovePlayer(att.Parent);
            }

            ActorFaction faction = ((ActorEntity)att.ParentEntity).Faction;
            if (faction.FactionType != FactionType.Player)
            {
                return;
            }

            EventSystem.PostEvent(OnPlayerFactionStateChangedEvent.GetEvent(att.Parent, from, to));

            //if ((from & ActorStateAttribute.StateInfo.Battle) != ActorStateAttribute.StateInfo.Battle &&
            //    (to & ActorStateAttribute.StateInfo.Battle) == ActorStateAttribute.StateInfo.Battle)
            //{
            //    ActorFactionComponent faction = att.Parent.GetComponent<ActorFactionComponent>();
            //    if (faction.FactionType != FactionType.Player)
            //    {
            //        return;
            //    }

            //    EventSystem.PostEvent(OnPlayerFactionStateChangedEvent.GetEvent(att.Parent, from, to));
            //}

            
        }
    }
}
