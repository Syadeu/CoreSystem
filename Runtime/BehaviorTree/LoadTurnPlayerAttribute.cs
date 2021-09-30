using BehaviorDesigner.Runtime.Tasks;
using Syadeu.Presentation.TurnTable;
using UnityEngine;

namespace Syadeu.Presentation.BehaviorTree
{
    [TaskCategory("Entity/Attributes")]
    public sealed class LoadTurnPlayerAttribute : Action
    {
        [SerializeField] private SharedRecycleableMonobehaviour m_Target;

        private TurnPlayerAttribute m_TurnPlayer;

        public TurnPlayerAttribute TurnPlayer => m_TurnPlayer;

        public override TaskStatus OnUpdate()
            => PresentationBehaviorTreeUtility.LoadAttributeFromMono(m_Target, nameof(LoadTurnPlayerAttribute), out m_TurnPlayer);
    }
}
