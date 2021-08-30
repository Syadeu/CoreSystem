using Newtonsoft.Json;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
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
        [JsonProperty(Order = 3, PropertyName = "OnStartTurn")] private Reference<TurnActionBase>[] m_OnStartTurnActions;
        [JsonProperty(Order = 4, PropertyName = "OnEndTurn")] private Reference<TurnActionBase>[] m_OnEndTurnActions;
        [JsonProperty(Order = 5, PropertyName = "OnResetTurn")] private Reference<TurnActionBase>[] m_OnResetTurnActions;

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

        public void StartTurn()
        {
            CoreSystem.Logger.Log(Channel.Entity, $"{Name} turn start");
            PresentationSystem<EventSystem>.System.PostEvent(OnTurnStateChangedEvent.GetEvent(this, OnTurnStateChangedEvent.TurnState.Start));

            for (int i = 0; i < m_OnStartTurnActions.Length; i++)
            {
                m_OnStartTurnActions[i].Execute(Parent);
            }
        }
        public void EndTurn()
        {
            CoreSystem.Logger.Log(Channel.Entity, $"{Name} turn end");
            PresentationSystem<EventSystem>.System.PostEvent(OnTurnStateChangedEvent.GetEvent(this, OnTurnStateChangedEvent.TurnState.End));

            for (int i = 0; i < m_OnEndTurnActions.Length; i++)
            {
                m_OnEndTurnActions[i].Execute(Parent);
            }
        }
        public void ResetTurnTable()
        {
            ActionPoint = m_MaxActionPoint;

            CoreSystem.Logger.Log(Channel.Entity, $"{Name} reset turn");
            PresentationSystem<EventSystem>.System.PostEvent(OnTurnStateChangedEvent.GetEvent(this, OnTurnStateChangedEvent.TurnState.Reset));

            for (int i = 0; i < m_OnResetTurnActions.Length; i++)
            {
                m_OnResetTurnActions[i].Execute(Parent);
            }
        }

        public void SetMaxActionPoint(int ap) => m_MaxActionPoint = ap;
        public int UseActionPoint(int ap) => m_CurrentActionPoint -= ap;
    }
    [Preserve]
    internal sealed class TurnPlayerProcessor : AttributeProcessor<TurnPlayerAttribute>
    {
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
