using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace Syadeu.Presentation.BehaviorTree
{
    [TaskCategory("Collections")]
    public sealed class IsNoneConditional : ConditionalBase
    {
        [SerializeField, SharedRequired]
        private SharedVariable m_Target;

        public override TaskStatus OnUpdate()
        {
            if (m_Target == null || m_Target.GetValue() == null)
            {
                return TaskStatus.Success;
            }
            return TaskStatus.Failure;
        }
    }
}
