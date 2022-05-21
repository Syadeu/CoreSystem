#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using BehaviorDesigner.Runtime.Tasks;
using Syadeu.Collections;
using Syadeu.Presentation.Map;
using Syadeu.Presentation.Components;

namespace Syadeu.Presentation.BehaviorTree
{
    using Syadeu.Presentation.Actor;
    using Syadeu.Presentation.Grid;
#if CORESYSTEM_TURNBASESYSTEM
    using Syadeu.Presentation.TurnTable;
    using Unity.Collections;

    [TaskCategory("Entity/Actor/TRPG")]
    [TaskDescription(
        "찾은 타겟의 근처로 최대한 이동합니다." +
        "FindTargetsAction 이 이전에 수행되어야됩니다.")]
    public sealed class MoveToTarget : ActionBase
    {
        private ActorEventHandler m_ActorEventHandler;
        private bool m_IsExecuted;

        public override void OnStart()
        {
            base.OnStart();

            m_ActorEventHandler = ActorEventHandler.Empty;
            m_IsExecuted = false;
        }
        public override TaskStatus OnUpdate()
        {
            if (m_IsExecuted)
            {
                if (!m_ActorEventHandler.IsExecuted) return TaskStatus.Running;
                else return TaskStatus.Success;
            }

#if DEBUG_MODE
            if (!Entity.IsValid())
            {
                CoreSystem.Logger.LogError(LogChannel.Entity,
                    $"Invalid Target at {nameof(NextTurnAction)}.");
                return TaskStatus.Failure;
            }
            else if (!Entity.HasComponent<TRPGActorAttackComponent>())
            {
                CoreSystem.Logger.LogError(LogChannel.Entity,
                    $"{Entity.RawName} doesn\'t have any {nameof(TRPGActorAttackComponent)}.");
                return TaskStatus.Failure;
            }
            else if (!Entity.HasComponent<TRPGActorMoveComponent>())
            {
                CoreSystem.Logger.LogError(LogChannel.Entity,
                    $"{Entity.RawName} doesn\'t have any {nameof(TRPGActorMoveComponent)}.");
                return TaskStatus.Failure;
            }
            else if (!Entity.HasComponent<TurnPlayerComponent>())
            {
                CoreSystem.Logger.LogError(LogChannel.Entity,
                    $"{Entity.RawName} doesn\'t have any {nameof(TurnPlayerComponent)}.");
                return TaskStatus.Failure;
            }
            else if (!Entity.HasComponent<GridComponent>())
            {
                CoreSystem.Logger.LogError(LogChannel.Entity,
                    $"{Entity.RawName} doesn\'t have any {nameof(GridComponent)}.");
                return TaskStatus.Failure;
            }
#endif
                TRPGActorAttackComponent att = Entity.GetComponentReadOnly<TRPGActorAttackComponent>();
            if (att.TargetCount == 0)
            {
                "no target".ToLog();
                return TaskStatus.Failure;
            }
            
            GridIndex targetPos = att.GetTargetAt(0).GetComponentReadOnly<GridComponent>().Indices[0];

            //WorldGridSystem gridSystem = PresentationSystem<DefaultPresentationGroup, WorldGridSystem>.System;
            GridComponent gridSize = Entity.GetComponentReadOnly<GridComponent>();

            //FixedList4096Bytes<GridIndex> tempPath = new FixedList4096Bytes<GridIndex>();
            //gridSystem.GetPath(Entity.Idx, targetPos, ref tempPath);

            ref TurnPlayerComponent turnPlayer = ref Entity.GetComponent<TurnPlayerComponent>();
            //FixedList4096Bytes<GridIndex> path;
            //if (tempPath[tempPath.Length - 1].Index == targetPos.Index)
            //{
            //    path = new FixedList4096Bytes<GridIndex>();
            //    for (int i = 0; i < turnPlayer.ActionPoint && i < tempPath.Length - 1; i++)
            //    {
            //        path.Add(tempPath[i]);
            //    }
            //}
            //else
            //{
            //    path = tempPath;
            //}

            $"from {gridSize.Indices[0].Location} to {targetPos.Location}".ToLog();
            //$"1. path length {tempPath.Length} :: {path.Length}".ToLog();

            m_ActorEventHandler = PresentationSystem<TRPGIngameSystemGroup, TRPGGridSystem>.System
                .MoveToCell(Entity.Idx, targetPos);

            //TRPGActorMoveComponent move = Entity.GetComponentReadOnly<TRPGActorMoveComponent>();
            //move.MoveTo(in path, new ActorMoveEvent(Entity.As<IEntity, IEntityData>(), 1));

            //turnPlayer.ActionPoint -= path.Length - 1;

            m_IsExecuted = true;

            return TaskStatus.Running;
        }
    }
#endif
}
