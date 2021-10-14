using BehaviorDesigner.Runtime.Tasks;
using Syadeu.Collections;
using Syadeu.Presentation.Map;
using Syadeu.Presentation.TurnTable;

namespace Syadeu.Presentation.BehaviorTree
{
#if CORESYSTEM_TURNBASESYSTEM
    [TaskCategory("Entity/Actor")]
    public sealed class MoveToTarget : ActionBase
    {
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

            TRPGActorAttackComponent att = Entity.GetComponentReadOnly<TRPGActorAttackComponent>();
            if (att.TargetCount == 0)
            {
                "no target".ToLog();
                return TaskStatus.Failure;
            }
            
            GridPosition targetPos = att.m_Targets[0].GetEntity<IEntity>().GetComponentReadOnly<GridSizeComponent>().positions[0];

            GridSizeComponent gridSize = Entity.GetComponentReadOnly<GridSizeComponent>();

            GridPath64 tempPath = new GridPath64();
            gridSize.GetPath64(targetPos.index, ref tempPath, avoidEntity: false);

            ref TurnPlayerComponent turnPlayer = ref Entity.GetComponent<TurnPlayerComponent>();
            GridPath64 path = new GridPath64();
            for (int i = 0; i < turnPlayer.ActionPoint && i < tempPath.Length - 1; i++)
            {
                path.Add(tempPath[i]);
            }

            $"from {gridSize.positions[0].location} to {targetPos.location}".ToLog();
            $"1. path length {tempPath.Length} :: {path.Length}".ToLog();

            TRPGActorMoveComponent move = Entity.GetComponentReadOnly<TRPGActorMoveComponent>();
            move.MoveTo(in path, new ActorMoveEvent(Entity.As<IEntity, IEntityData>(), 1));

            turnPlayer.ActionPoint -= path.Length;

            return TaskStatus.Success;
        }
    }
#endif
}
