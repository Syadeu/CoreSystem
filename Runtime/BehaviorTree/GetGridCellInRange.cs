//using BehaviorDesigner.Runtime;
//using BehaviorDesigner.Runtime.Tasks;
//using Syadeu.Collections;
//using Syadeu.Presentation.Entities;
//using Syadeu.Presentation.Map;
//using UnityEngine;

//namespace Syadeu.Presentation.BehaviorTree
//{
//    [TaskCategory("Entity/Grid")]
//    public sealed class GetGridCellInRange : Action
//    {
//        [SerializeField] private bool m_UseEntity;
//        [SerializeField] private SharedEntity m_TargetEntity;
//        [SerializeField] private SharedRecycleableMonobehaviour m_Target;

//        [Space]
//        [SerializeField] private int m_Range;

//        private int[] m_Result;

//        public int[] Result => m_Result;

//        public override TaskStatus OnUpdate()
//        {
//            if (m_UseEntity) return UseEntity();

//            return UseMono();
//        }

//        private TaskStatus UseEntity()
//        {
//            if (!m_TargetEntity.IsValid())
//            {
//                CoreSystem.Logger.LogError(Channel.Entity,
//                    $"Invalid Target at {nameof(GetGridCellInRange)}.");
//                return TaskStatus.Failure;
//            }

//            if (!m_TargetEntity.Value.HasComponent<GridSizeComponent>())
//            {
//                CoreSystem.Logger.LogError(Channel.Entity,
//                    $"Invalid Target at {nameof(GetGridCellInRange)}, no {nameof(GridSizeComponent)}.");
//                return TaskStatus.Failure;
//            }

//            m_Result = m_TargetEntity.Value.GetComponent<GridSizeComponent>().GetRange(m_Range);
//            return TaskStatus.Success;
//        }
//        private TaskStatus UseMono()
//        {
//            if (!m_Target.IsValid())
//            {
//                CoreSystem.Logger.LogError(Channel.Entity,
//                    $"Invalid Target at {nameof(GetGridCellInRange)}.");
//                return TaskStatus.Failure;
//            }

//            Entity<IEntity> entity = m_Target.GetEntity();
//            if (!entity.HasComponent<GridSizeComponent>())
//            {
//                CoreSystem.Logger.LogError(Channel.Entity,
//                    $"Invalid Target at {nameof(GetGridCellInRange)}, no {nameof(GridSizeAttribute)}.");
//                return TaskStatus.Failure;
//            }

//            m_Result = entity.GetComponent<GridSizeComponent>().GetRange(m_Range);
//            return TaskStatus.Success;
//        }
//    }
//}
