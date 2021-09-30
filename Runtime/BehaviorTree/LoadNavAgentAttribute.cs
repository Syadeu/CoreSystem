using BehaviorDesigner.Runtime.Tasks;
using Syadeu.Presentation.Attributes;
using UnityEngine;

namespace Syadeu.Presentation.BehaviorTree
{
    [TaskCategory("Entity/Attributes")]
    public sealed class LoadNavAgentAttribute : Action
    {
        [SerializeField] private SharedRecycleableMonobehaviour m_Target;

        private NavAgentAttribute m_NavAgent;

        public NavAgentAttribute NavAgentAttribute => m_NavAgent;

        public override TaskStatus OnUpdate()
            => PresentationBehaviorTreeUtility.LoadAttributeFromMono(m_Target, nameof(LoadNavAgentAttribute), out m_NavAgent);
    }
}
