#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using BehaviorDesigner.Runtime.Tasks;
using Syadeu.Collections;
using Syadeu.Presentation.Map;
using Syadeu.Presentation.Components;

namespace Syadeu.Presentation.BehaviorTree
{
#if CORESYSTEM_TURNBASESYSTEM
    using Syadeu.Presentation.TurnTable;

    [TaskCategory("Entity/Actor/TRPG")]
    [TaskDescription(
        "찾은 타겟의 근처로 최대한 이동합니다." +
        "FindTargetsAction 이 이전에 수행되어야됩니다.")]
    public sealed class MoveToTarget : ActionBase
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
            else if (!Entity.HasComponent<TRPGActorMoveComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"{Entity.RawName} doesn\'t have any {nameof(TRPGActorMoveComponent)}.");
                return TaskStatus.Failure;
            }
            else if (!Entity.HasComponent<TurnPlayerComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"{Entity.RawName} doesn\'t have any {nameof(TurnPlayerComponent)}.");
                return TaskStatus.Failure;
            }
            else if (!Entity.HasComponent<GridSizeComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"{Entity.RawName} doesn\'t have any {nameof(GridSizeComponent)}.");
                return TaskStatus.Failure;
            }
#endif
            TRPGActorAttackComponent att = Entity.GetComponentReadOnly<TRPGActorAttackComponent>();
            if (att.TargetCount == 0)
            {
                "no target".ToLog();
                return TaskStatus.Failure;
            }
            
            GridPosition targetPos = att.GetTargetAt(0).GetEntity<IEntity>().GetComponentReadOnly<GridSizeComponent>().positions[0];

            GridSizeComponent gridSize = Entity.GetComponentReadOnly<GridSizeComponent>();

            GridPath64 tempPath = new GridPath64();
            gridSize.GetPath64(targetPos.index, ref tempPath, avoidEntity: true);

            ref TurnPlayerComponent turnPlayer = ref Entity.GetComponent<TurnPlayerComponent>();
            GridPath64 path;
            if (tempPath[tempPath.Length - 1].index == targetPos.index)
            {
                path = new GridPath64();
                for (int i = 0; i < turnPlayer.ActionPoint && i < tempPath.Length - 1; i++)
                {
                    path.Add(tempPath[i]);
                }
            }
            else
            {
                path = tempPath;
            }

            $"from {gridSize.positions[0].location} to {targetPos.location}".ToLog();
            $"1. path length {tempPath.Length} :: {path.Length}".ToLog();

            TRPGActorMoveComponent move = Entity.GetComponentReadOnly<TRPGActorMoveComponent>();
            move.MoveTo(in path, new ActorMoveEvent(Entity.As<IEntity, IEntityData>(), 1));

            turnPlayer.ActionPoint -= path.Length - 1;

            return TaskStatus.Success;
        }
    }
#endif
}
