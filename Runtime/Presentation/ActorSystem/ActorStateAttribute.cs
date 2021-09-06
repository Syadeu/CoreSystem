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
        public enum StateInfo
        {
            None = 0,

            Idle    =   0b00001,
            Alert   =   0b00010,
            Chasing =   0b00100,
            Battle  =   0b01000,

            Dead    =   0b10000
        }

        [Header("TriggerActions")]
        [JsonProperty(Order = 0, PropertyName = "OnStateChanged")]
        private Reference<TriggerAction>[] m_OnStateChanged = Array.Empty<Reference<TriggerAction>>();
        [JsonProperty(Order = 0, PropertyName = "OnIdleState")]
        private Reference<TriggerAction>[] m_OnIdleState = Array.Empty<Reference<TriggerAction>>();
        [JsonProperty(Order = 0, PropertyName = "OnAlertState")]
        private Reference<TriggerAction>[] m_OnAlertState = Array.Empty<Reference<TriggerAction>>();
        [JsonProperty(Order = 0, PropertyName = "OnChasingState")]
        private Reference<TriggerAction>[] m_OnChasingState = Array.Empty<Reference<TriggerAction>>();
        [JsonProperty(Order = 0, PropertyName = "OnBattleState")]
        private Reference<TriggerAction>[] m_OnBattleState = Array.Empty<Reference<TriggerAction>>();
        [JsonProperty(Order = 0, PropertyName = "OnDeadState")]
        private Reference<TriggerAction>[] m_OnDeadState = Array.Empty<Reference<TriggerAction>>();

        [JsonIgnore] internal EventSystem m_EventSystem = null;
        [JsonIgnore] private StateInfo m_State = StateInfo.Idle;

        [JsonIgnore] public StateInfo State
        {
            get => m_State;
            set
            {
                if (m_State.Equals(value)) return;

                StateInfo prev = m_State;
                m_State = value;

                if ((value & StateInfo.Idle) == StateInfo.Idle) m_OnIdleState.Execute(Parent);
                if ((value & StateInfo.Alert) == StateInfo.Alert) m_OnAlertState.Execute(Parent);
                if ((value & StateInfo.Chasing) == StateInfo.Chasing) m_OnChasingState.Execute(Parent);
                if ((value & StateInfo.Battle) == StateInfo.Battle) m_OnBattleState.Execute(Parent);
                if ((value & StateInfo.Dead) == StateInfo.Dead) m_OnDeadState.Execute(Parent);
                m_OnStateChanged.Execute(Parent);

                m_EventSystem.PostEvent(OnActorStateChangedEvent.GetEvent(
                    Parent.As<IEntityData, ActorEntity>(), prev, m_State));
            }
        }
    }
    internal sealed class ActorStateProcessor : AttributeProcessor<ActorStateAttribute>
    {
        protected override void OnCreated(ActorStateAttribute attribute, EntityData<IEntityData> entity)
        {
            attribute.m_EventSystem = EventSystem;
        }
        protected override void OnDestroy(ActorStateAttribute attribute, EntityData<IEntityData> entity)
        {
            attribute.m_EventSystem = null;
        }
    }
}
