using BehaviorDesigner.Runtime.Tasks;
using Syadeu.Presentation.Map;
using UnityEngine;

namespace Syadeu.Presentation.BehaviorTree
{
    [TaskCategory("Entity/Attributes")]
    public sealed class LoadGridSizeAttribute : Action
    {
        [SerializeField] private SharedRecycleableMonobehaviour m_Target;

        private GridSizeAttribute m_GridSize;

        public GridSizeAttribute GridSizeAttribute => m_GridSize;

        public override TaskStatus OnUpdate()
            => PresentationBehaviorTreeUtility.LoadAttributeFromMono(m_Target, nameof(LoadGridSizeAttribute), out m_GridSize);
    }
}
