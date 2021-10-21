using BehaviorDesigner.Runtime.Tasks;
using Syadeu.Collections;
using Syadeu.Presentation.Map;
using Syadeu.Presentation.TurnTable;
using Unity.Collections;
using UnityEngine;

namespace Syadeu.Presentation.BehaviorTree
{
#if CORESYSTEM_TURNBASESYSTEM
    [TaskCategory("Entity/Actor/TRPG")]
    public sealed class IsActorTargetInAttackRange : ConditionalBase
    {
        [SerializeField] private int m_DesireRange = 1;

        public override TaskStatus OnUpdate()
        {
            if (!Entity.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Invalid Target at {nameof(NextTurnAction)}.");
                return TaskStatus.Failure;
            }
            else if (!Entity.HasComponent<TRPGActorAttackComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"{Entity.RawName} doesn\'t have any {nameof(TRPGActorAttackComponent)}.");
                return TaskStatus.Failure;
            }
            else if (!Entity.HasComponent<GridSizeComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"{Entity.RawName} doesn\'t have any {nameof(GridSizeComponent)}.");
                return TaskStatus.Failure;
            }

            TRPGActorAttackComponent att = Entity.GetComponentReadOnly<TRPGActorAttackComponent>();
            if (att.TargetCount == 0)
            {
                "no target".ToLog();
                return TaskStatus.Failure;
            }

            var targetPos = att.m_Targets[0].GetEntity<IEntity>().GetComponentReadOnly<GridSizeComponent>().positions;

            FixedList4096Bytes<int> list = new FixedList4096Bytes<int>();
            GridSizeComponent gridSize = Entity.GetComponentReadOnly<GridSizeComponent>();
            gridSize.GetRange(ref list, in m_DesireRange);

            for (int i = 0; i < targetPos.Length; i++)
            {
                if (list.Contains(targetPos[i].index))
                {
                    return TaskStatus.Success;
                }
            }

            return TaskStatus.Failure;
        }
    }
#endif
}
