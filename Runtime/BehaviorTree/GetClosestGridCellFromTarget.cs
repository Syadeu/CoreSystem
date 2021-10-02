using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Syadeu.Presentation.Map;
using Syadeu.Presentation.TurnTable;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.BehaviorTree
{
    [TaskCategory("Entity/Grid")]
    public sealed class GetClosestGridCellFromTarget : Action
    {
        [SerializeField] private LoadTurnPlayerAttribute m_ThisTurnPlayer;
        [SerializeField] private LoadGridSizeAttribute m_ThisGridSize;

        [Space]
        [SerializeField] private SharedEntity m_Target;
        [SerializeField] private int[] m_ObstacleLayers = System.Array.Empty<int>();

        [SerializeField] private int m_Result;

        public int Result => m_Result;

        public override TaskStatus OnUpdate()
        {
            if (!m_Target.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Invalid Target at {nameof(GetGridCellInRange)}.");
                return TaskStatus.Failure;
            }

            if (!m_Target.HasComponent<GridSizeComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Invalid Target at {nameof(GetGridCellInRange)}, no {nameof(GridSizeComponent)}.");
                return TaskStatus.Failure;
            }

            $"{m_Target.Value.Name}".ToLog();

            //m_ThisGridSize.GridSizeAttribute.UpdateGridCell();
            //targetGrid.UpdateGridCell();
            GridSizeComponent
                targetComponent = m_Target.Value.GetComponent<GridSizeComponent>(),
                thisComponent = m_ThisGridSize.GridSizeAttribute.Parent.GetComponent<GridSizeComponent>();

            TurnPlayerComponent turnPlayer = m_ThisTurnPlayer.TurnPlayer.Parent.GetComponent<TurnPlayerComponent>();

            int currentAp = turnPlayer.ActionPoint;
            int[]
                //targetCell = targetGrid.CurrentGridIndices,
                //thisCell = m_ThisGridSize.GridSizeAttribute.CurrentGridIndices,
                range = thisComponent.GetRange(currentAp, m_ObstacleLayers);

            Vector3 targetPos = m_Target.Value.transform.position;

            float sqr = float.MaxValue;
            int idx = -1;
            GridPath32 path = new GridPath32();
            for (int i = 0; i < range.Length; i++)
            {
                if (targetComponent.IsMyIndex(range[i]) || thisComponent.IsMyIndex(range[i]))
                {
                    continue;
                }

                if (!thisComponent.GetPath32(range[i], ref path, currentAp))
                {
                    continue;
                }
                int requireAP = path.Length;

                Vector3 pos = thisComponent.IndexToPosition(range[i]);
                float tempSqr = (targetPos - pos).sqrMagnitude;
                if (tempSqr < sqr)
                {
                    sqr = tempSqr;
                    idx = range[i];
                }
            }
            if (idx < 0) return TaskStatus.Failure;

            m_Result = idx;
            return TaskStatus.Success;
        }
    }
}
