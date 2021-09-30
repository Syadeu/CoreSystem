using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace Syadeu.Presentation.BehaviorTree
{
    [TaskCategory("Entity/Grid")]
    public sealed class SelectRandomGridCell : Action
    {
        [SerializeField] private GetGridCellInRange m_GetGridCellInRange;

        [Space]
        [SerializeField] private int m_Result;

        public override TaskStatus OnUpdate()
        {
            if (m_GetGridCellInRange.Result == null ||
                m_GetGridCellInRange.Result.Length == 0)
            {
                return TaskStatus.Failure;
            }

            int temp = UnityEngine.Random.Range(0, m_GetGridCellInRange.Result.Length);
            m_Result = m_GetGridCellInRange.Result[temp];
            return TaskStatus.Success;
        }
    }
}
