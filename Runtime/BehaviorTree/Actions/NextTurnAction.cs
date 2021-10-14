using BehaviorDesigner.Runtime.Tasks;
using Syadeu.Collections;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Map;
using Syadeu.Presentation.TurnTable;

namespace Syadeu.Presentation.BehaviorTree
{
#if CORESYSTEM_TURNBASESYSTEM
    [TaskCategory("Entity/Actor")]
    public sealed class NextTurnAction : ActionBase
    {
        public override TaskStatus OnUpdate()
        {
            if (!Entity.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Invalid Target at {nameof(NextTurnAction)}.");
                return TaskStatus.Failure;
            }

            if (!Entity.HasComponent<TurnPlayerComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"{Entity.RawName} doesn\'t have any {nameof(TurnPlayerComponent)}.");
                return TaskStatus.Failure;
            }

            TurnPlayerComponent turnPlayer = Entity.GetComponent<TurnPlayerComponent>();

            if (!turnPlayer.IsMyTurn)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"It is not a entity({Entity.RawName}) turn.");
                return TaskStatus.Failure;
            }

            PresentationSystem<DefaultPresentationGroup, EventSystem>
                .System.ScheduleEvent(TRPGEndTurnEvent.GetEvent());

            return TaskStatus.Success;
        }
    }

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
                return TaskStatus.Failure;
            }
            
            GridPosition targetPos = att.m_Targets[0].GetEntity<IEntity>().GetComponentReadOnly<GridSizeComponent>().positions[0];

            GridSizeComponent gridSize = Entity.GetComponentReadOnly<GridSizeComponent>();

            GridPath64 tempPath = new GridPath64();
            gridSize.GetPath64(targetPos.index, ref tempPath);

            ref TurnPlayerComponent turnPlayer = ref Entity.GetComponent<TurnPlayerComponent>();
            GridPath64 path = new GridPath64();
            for (int i = 0; i < turnPlayer.ActionPoint && i < tempPath.Length; i++)
            {
                path.Add(tempPath[i]);
            }

            TRPGActorMoveComponent move = Entity.GetComponentReadOnly<TRPGActorMoveComponent>();
            move.MoveTo(in path, new ActorMoveEvent(Entity.As<IEntity, IEntityData>(), 1));

            turnPlayer.ActionPoint -= path.Length - 1;

            return TaskStatus.Success;
        }
    }
#endif
}
