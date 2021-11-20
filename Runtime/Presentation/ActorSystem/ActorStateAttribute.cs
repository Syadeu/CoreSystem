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
using Syadeu.Presentation.Events;
using System;
using System.ComponentModel;
using UnityEngine;

namespace Syadeu.Presentation.Actor
{
    [DisplayName("Attribute: Actor State")]
    public sealed class ActorStateAttribute : ActorAttributeBase
    {
        [Flags]
        public enum StateInfo
        {
            None = 0,

            Spawn   =   0b00001,

            Idle    =   0b00010,
            Alert   =   0b00100,
            Battle  =   0b01000,

            Dead    =   0b10000
        }

        [Header("TriggerActions")]
        [JsonProperty(Order = 0, PropertyName = "OnStateChanged")]
        private Reference<TriggerAction>[] m_OnStateChanged = Array.Empty<Reference<TriggerAction>>();
        [JsonProperty(Order = 0, PropertyName = "OnSpawn")]
        private Reference<TriggerAction>[] m_OnSpawn = Array.Empty<Reference<TriggerAction>>();
        [JsonProperty(Order = 0, PropertyName = "OnIdle")]
        private Reference<TriggerAction>[] m_OnIdle = Array.Empty<Reference<TriggerAction>>();
        [JsonProperty(Order = 0, PropertyName = "OnAlert")]
        private Reference<TriggerAction>[] m_OnAlert = Array.Empty<Reference<TriggerAction>>();
        [JsonProperty(Order = 0, PropertyName = "OnBattle")]
        private Reference<TriggerAction>[] m_OnBattle = Array.Empty<Reference<TriggerAction>>();
        [JsonProperty(Order = 0, PropertyName = "OnDead")]
        private Reference<TriggerAction>[] m_OnDead = Array.Empty<Reference<TriggerAction>>();

        [JsonIgnore] private StateInfo m_State = StateInfo.Idle;
        [JsonIgnore] private Action<ActorStateAttribute, StateInfo, StateInfo> m_OnStateChangedEvent;

        [JsonIgnore] public StateInfo State
        {
            get => m_State;
            set
            {
                if (m_State.Equals(value)) return;

                StateInfo prev = m_State;
                m_State = value;

                m_OnStateChangedEvent?.Invoke(this, prev, m_State);

                if ((value & StateInfo.Spawn) == StateInfo.Spawn) m_OnSpawn.Execute(Parent);
                if ((value & StateInfo.Idle) == StateInfo.Idle) m_OnIdle.Execute(Parent);
                if ((value & StateInfo.Alert) == StateInfo.Alert) m_OnAlert.Execute(Parent);
                if ((value & StateInfo.Battle) == StateInfo.Battle) m_OnBattle.Execute(Parent);
                if ((value & StateInfo.Dead) == StateInfo.Dead) m_OnDead.Execute(Parent);
                m_OnStateChanged.Execute(Parent);

                PresentationSystem<DefaultPresentationGroup, EventSystem>.System
                    .PostEvent(OnActorStateChangedEvent.GetEvent(
                        Parent.As<IEntityData, ActorEntity>(), prev, m_State));
            }
        }

        public void AddEvent(Action<ActorStateAttribute, StateInfo, StateInfo> ev)
        {
            m_OnStateChangedEvent += ev;
        }
        public void RemoveEvent(Action<ActorStateAttribute, StateInfo, StateInfo> ev)
        {
            m_OnStateChangedEvent -= ev;
        }
    }
}
