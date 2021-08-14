using Newtonsoft.Json;
using Syadeu.Mono;
using Syadeu.Mono.TurnTable;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Event;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Attributes
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
                    m_CurrentActionPoint = value;
                    OnActionPointChanged?.Invoke(value);
                }
            }
        }

        public event Action<int> OnActionPointChanged;

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

    public sealed class OnTurnStateChangedEvent : SynchronizedEvent<OnTurnStateChangedEvent>
    {
        public enum TurnState
        {
            Reset   =   0b001,
            Start   =   0b010,
            End     =   0b100,
        }
        public EntityData<IEntityData> Entity { get; private set; }
        public TurnPlayerAttribute Attribute { get; private set; }
        public TurnState State { get; private set; }

        public static OnTurnStateChangedEvent GetEvent(TurnPlayerAttribute target, TurnState state)
        {
            var temp = Dequeue();

            temp.Entity = target.Parent;
            temp.Attribute = target;
            temp.State = state;

            return temp;
        }
        protected override void OnTerminate()
        {
            Entity = EntityData<IEntityData>.Empty;
            Attribute = null;
        }
    }
}
