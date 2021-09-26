using Newtonsoft.Json;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.TurnTable
{
    [DisplayName("Attribute: Turn Player")]
    public sealed class TurnPlayerAttribute : AttributeBase, ITurnPlayer
    {
        [Header("Generals")]
        [JsonProperty(Order = 0, PropertyName = "ActivateOnCreate")] private bool m_ActivateOnCreate = true;
        [JsonProperty(Order = 1, PropertyName = "TurnSpeed")] private float m_TurnSpeed = 0;
        [JsonProperty(Order = 2, PropertyName = "MaxActionPoint")] private int m_MaxActionPoint = 6;

        [Space]
        [Header("Actions")]
        [JsonProperty(Order = 3, PropertyName = "OnStartTurn")]
        private Reference<TriggerAction>[] m_OnStartTurnActions = Array.Empty<Reference<TriggerAction>>();
        [JsonProperty(Order = 4, PropertyName = "OnEndTurn")] 
        private Reference<TriggerAction>[] m_OnEndTurnActions = Array.Empty<Reference<TriggerAction>>();
        [JsonProperty(Order = 5, PropertyName = "OnResetTurn")] 
        private Reference<TriggerAction>[] m_OnResetTurnActions = Array.Empty<Reference<TriggerAction>>();

        [JsonIgnore] private int m_CurrentActionPoint = 6;

        [JsonIgnore] public bool ActivateOnCreate => m_ActivateOnCreate;
        [JsonIgnore] public string DisplayName => Name;
        [JsonIgnore] public float TurnSpeed => m_TurnSpeed;
        [JsonIgnore] public bool ActivateTurn { get; set; }
        [JsonIgnore] public int MaxActionPoint => m_MaxActionPoint;
        [JsonIgnore] public int ActionPoint
        {
            get => m_CurrentActionPoint;
            set
            {
                if (!m_CurrentActionPoint.Equals(value))
                {
                    int prev = m_CurrentActionPoint;
                    m_CurrentActionPoint = value;
                    PresentationSystem<EventSystem>.System.PostEvent(OnActionPointChangedEvent.GetEvent(Parent, prev, value));
                }
            }
        }

        [JsonIgnore] public bool IsMyTurn { get; private set; }

        public void StartTurn()
        {
            IsMyTurn = true;
            CoreSystem.Logger.Log(Channel.Entity, $"{Name} turn start");
            PresentationSystem<EventSystem>.System.PostEvent(OnTurnStateChangedEvent.GetEvent(this, OnTurnStateChangedEvent.TurnState.Start));

            m_OnStartTurnActions.Schedule(Parent);
        }
        public void EndTurn()
        {
            IsMyTurn = false;
            CoreSystem.Logger.Log(Channel.Entity, $"{Name} turn end");
            PresentationSystem<EventSystem>.System.PostEvent(OnTurnStateChangedEvent.GetEvent(this, OnTurnStateChangedEvent.TurnState.End));

            m_OnEndTurnActions.Schedule(Parent);
        }
        public void ResetTurnTable()
        {
            ActionPoint = m_MaxActionPoint;

            CoreSystem.Logger.Log(Channel.Entity, $"{Name} reset turn");
            PresentationSystem<EventSystem>.System.PostEvent(OnTurnStateChangedEvent.GetEvent(this, OnTurnStateChangedEvent.TurnState.Reset));

            m_OnResetTurnActions.Schedule(Parent);
        }

        public void SetMaxActionPoint(int ap) => m_MaxActionPoint = ap;
        public int UseActionPoint(int ap) => m_CurrentActionPoint -= ap;
    }
    [Preserve]
    internal sealed class TurnPlayerProcessor : AttributeProcessor<TurnPlayerAttribute>
    {
        protected override void OnInitialize()
        {
            EventSystem.AddEvent<OnActionPointChangedEvent>(OnActionPointChangedEventHandler);
        }
        protected override void OnDispose()
        {
            EventSystem.RemoveEvent<OnActionPointChangedEvent>(OnActionPointChangedEventHandler);
        }
        private void OnActionPointChangedEventHandler(OnActionPointChangedEvent ev)
        {
            ActorControllerAttribute ctr = ev.Entity.GetAttribute<ActorControllerAttribute>();
            if (ctr == null) return;

            TRPGActorActionPointChangedUIEvent actorEv = new TRPGActorActionPointChangedUIEvent(ev.From, ev.To);
            ctr.PostEvent(actorEv);
        }

        protected override void OnCreated(TurnPlayerAttribute attribute, EntityData<IEntityData> entity)
        {
            attribute.ActivateTurn = attribute.ActivateOnCreate;
            TurnTableManager.AddPlayer(attribute);
        }
        protected override void OnDestroy(TurnPlayerAttribute attribute, EntityData<IEntityData> entity)
        {
            TurnTableManager.RemovePlayer(attribute);
        }
    }
}
