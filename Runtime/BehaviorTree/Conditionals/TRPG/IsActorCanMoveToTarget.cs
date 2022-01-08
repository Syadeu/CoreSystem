#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using BehaviorDesigner.Runtime.Tasks;
using Syadeu.Presentation.Map;
using Syadeu.Collections;
using Syadeu.Presentation.Components;

namespace Syadeu.Presentation.BehaviorTree
{
    using Syadeu.Presentation.Grid;
#if CORESYSTEM_TURNBASESYSTEM
    using Syadeu.Presentation.TurnTable;

    [TaskCategory("Entity/Actor/TRPG")]
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
            else if (!Entity.HasComponent<GridComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"{Entity.RawName} doesn\'t have any {nameof(GridComponent)}.");
                return TaskStatus.Failure;
            }
#endif

            GridComponent gridSize = Entity.GetComponent<GridComponent>();
            TRPGActorAttackComponent att = Entity.GetComponent<TRPGActorAttackComponent>();

            if (att.TargetCount == 0)
            {
                return TaskStatus.Failure;
            }

            WorldGridSystem gridSystem = PresentationSystem<DefaultPresentationGroup, WorldGridSystem>.System;

            if (gridSystem.HasPath(gridSize.Indices[0], att.GetTargetAt<IEntityData>(0).GetComponent<GridComponent>().Indices[0], out _))
            {
                return TaskStatus.Success;
            }

            return TaskStatus.Failure;
        }
    }
#endif
}
