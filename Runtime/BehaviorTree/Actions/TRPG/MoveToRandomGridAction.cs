#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using BehaviorDesigner.Runtime.Tasks;
using Syadeu.Collections;
using Syadeu.Presentation.Map;
using Unity.Collections;
using UnityEngine;
using Syadeu.Presentation.Components;

namespace Syadeu.Presentation.BehaviorTree
{
#if CORESYSTEM_TURNBASESYSTEM
    using Syadeu.Presentation.TurnTable;

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

            var grid = PresentationSystem<DefaultPresentationGroup, GridSystem>.System;
            for (int i = 0; i < gridSize.positions.Length; i++)
            {
                range.Remove(gridSize.positions[i].index);
            }
            for (int i = range.Length - 1; i >= 0; i--)
            {
                if (grid.HasEntityAt(range[i]))
                {
                    range.RemoveAt(i);
                }
            }
            if (range.Length == 0)
            {
                "cant move all blocked".ToLog();

                return TaskStatus.Success;
            }

            int rnd = Random.Range(0, range.Length);
            GridPath64 tempPath = new GridPath64();
            gridSize.GetPath64(range[rnd], ref tempPath, avoidEntity: true);

            PresentationSystem<TRPGIngameSystemGroup, TRPGGridSystem>.System
                .MoveToCell(Entity, tempPath, new ActorMoveEvent(Entity.ToEntity<IEntityData>(), 1));

            return TaskStatus.Success;
        }
    }
#endif
}
