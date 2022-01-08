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
    using Syadeu.Presentation.Grid;
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
            else if (!Entity.HasComponent<GridComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"{Entity.RawName} doesn\'t have any {nameof(GridComponent)}.");
                return TaskStatus.Failure;
            }
#endif
            WorldGridSystem gridSystem = PresentationSystem<DefaultPresentationGroup, WorldGridSystem>.System;
            GridComponent gridSize = Entity.GetComponent<GridComponent>();

            FixedList4096Bytes<GridIndex> range = new FixedList4096Bytes<GridIndex>();
            gridSystem.GetRange(Entity.Idx, m_DesireRange, ref range);

            for (int i = 0; i < gridSize.Indices.Length; i++)
            {
                range.Remove(gridSize.Indices[i]);
            }
            for (int i = range.Length - 1; i >= 0; i--)
            {
                if (gridSystem.HasEntityAt(range[i]))
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
            FixedList4096Bytes<GridIndex> tempPath = new FixedList4096Bytes<GridIndex>();
            gridSystem.GetPath(Entity.Idx, range[rnd], ref tempPath);

            PresentationSystem<TRPGIngameSystemGroup, TRPGGridSystem>.System
                .MoveToCell(Entity, tempPath, new ActorMoveEvent(Entity.ToEntity<IEntityData>(), 1));

            return TaskStatus.Success;
        }
    }
#endif
}
