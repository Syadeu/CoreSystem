using Newtonsoft.Json;
using Syadeu.Mono;
using Syadeu.Mono.TurnTable;
using Syadeu.Presentation.Entities;
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
            set => m_CurrentActionPoint = value;
        }

        public void StartTurn()
        {
            CoreSystem.Logger.Log(Channel.Entity, $"{Name} turn start");
        }
        public void EndTurn()
        {
            CoreSystem.Logger.Log(Channel.Entity, $"{Name} turn end");
        }
        public void ResetTurnTable()
        {
            m_CurrentActionPoint = m_MaxActionPoint;

            CoreSystem.Logger.Log(Channel.Entity, $"{Name} reset turn");
        }

        public void SetMaxActionPoint(int ap) => m_MaxActionPoint = ap;
        public int UseActionPoint(int ap) => m_CurrentActionPoint -= ap;

        [System.Obsolete("", true)]
        public IReadOnlyList<int2> GetMoveableCells()
        {
            IEntity parent = (IEntity)Parent;

            ref GridManager.GridCell cell = ref parent.GetCurrentCell();
            return TurnTableManager.GetMoveableCells(in cell, ActionPoint);
        }
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
