#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using BehaviorDesigner.Runtime.Tasks;
using Syadeu.Presentation.Map;
using Syadeu.Collections;
using Syadeu.Presentation.Components;

namespace Syadeu.Presentation.BehaviorTree
{
#if CORESYSTEM_TURNBASESYSTEM
    using Syadeu.Presentation.TurnTable;

    public sealed class IsActorCanMoveToTarget : ConditionalBase
    {
        public override TaskStatus OnUpdate()
        {
#if DEBUG_MODE
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
#endif

            GridSizeComponent gridSize = Entity.GetComponent<GridSizeComponent>();
            TRPGActorAttackComponent att = Entity.GetComponent<TRPGActorAttackComponent>();

            if (att.TargetCount == 0)
            {
                return TaskStatus.Failure;
            }

            if (gridSize.HasPath(att.GetTargetAt<IEntityData>(0).GetComponent<GridSizeComponent>().positions[0].index))
            {
                return TaskStatus.Success;
            }

            return TaskStatus.Failure;
        }
    }
#endif
}
