using Newtonsoft.Json;

using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;

using UnityEngine.Scripting;

namespace Syadeu.Presentation.TurnTable
{
    public sealed class TurnPlayerAttribute : AttributeBase, ITurnPlayer
    {
        [JsonProperty(Order = 0, PropertyName = "ActivateOnCreate")] private bool m_ActivateOnCreate = true;
        [JsonProperty(Order = 1, PropertyName = "TurnSpeed")] private float m_TurnSpeed = 0;
        [JsonProperty(Order = 2, PropertyName = "MaxActionPoint")] private int m_MaxActionPoint = 6;

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
        }
        public void EndTurn()
        {
            CoreSystem.Logger.Log(Channel.Entity, $"{Name} turn end");
            PresentationSystem<EventSystem>.System.PostEvent(OnTurnStateChangedEvent.GetEvent(this, OnTurnStateChangedEvent.TurnState.End));
        }
        public void ResetTurnTable()
        {
            ActionPoint = m_MaxActionPoint;

            CoreSystem.Logger.Log(Channel.Entity, $"{Name} reset turn");
            PresentationSystem<EventSystem>.System.PostEvent(OnTurnStateChangedEvent.GetEvent(this, OnTurnStateChangedEvent.TurnState.Reset));
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
