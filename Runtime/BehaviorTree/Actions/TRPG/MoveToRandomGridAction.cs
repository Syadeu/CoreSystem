#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

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
    public sealed class MoveToRandomGridAction : ActionBase
    {
        [SerializeField] private int m_DesireRange = 1;

        public override TaskStatus OnUpdate()
        {
#if DEBUG_MODE
            if (!Entity.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Invalid Target at {nameof(NextTurnAction)}.");
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
            GridSizeComponent gridSize = Entity.GetComponent<GridSizeComponent>();

            FixedList4096Bytes<int> range = new FixedList4096Bytes<int>();
            gridSize.GetRange(ref range, in m_DesireRange);

            for (int i = 0; i < gridSize.positions.Length; i++)
            {
                range.Remove(gridSize.positions[i].index);
            }

            int rnd = Random.Range(0, range.Length);
            GridPath64 tempPath = new GridPath64();
            gridSize.GetPath64(range[rnd], ref tempPath, avoidEntity: true);

            ref TurnPlayerComponent turnPlayer = ref Entity.GetComponent<TurnPlayerComponent>();
            //GridPath64 path = new GridPath64();
            //for (int i = 0; i < turnPlayer.ActionPoint && i < tempPath.Length - 1; i++)
            //{
            //    path.Add(tempPath[i]);
            //}

            //$"1. path length {tempPath.Length} :: {path.Length}".ToLog();

            TRPGActorMoveComponent move = Entity.GetComponentReadOnly<TRPGActorMoveComponent>();
            move.MoveTo(in tempPath, new ActorMoveEvent(Entity.As<IEntity, IEntityData>(), 1));

            turnPlayer.ActionPoint -= tempPath.Length - 1;

            return TaskStatus.Success;
        }
    }
#endif
}
