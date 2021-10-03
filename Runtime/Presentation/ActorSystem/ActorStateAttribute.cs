using Newtonsoft.Json;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using System;
using UnityEngine;

namespace Syadeu.Presentation.Actor
{
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

        [JsonIgnore] public StateInfo State
        {
            get => m_State;
            set
            {
                if (m_State.Equals(value)) return;

                StateInfo prev = m_State;
                m_State = value;

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
    }
}
