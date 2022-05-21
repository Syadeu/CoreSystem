﻿#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
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
    using System.Linq;

    [TaskCategory("Entity/Actor/TRPG")]
    public sealed class IsActorTargetInAttackRange : ConditionalBase
    {
        [SerializeField] private int m_DesireRange = 1;

        public override TaskStatus OnUpdate()
        {
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

            var targetPos = att.GetTargetAt(0).GetComponentReadOnly<GridComponent>().Indices;

            //FixedList4096Bytes<GridIndex> list = new FixedList4096Bytes<GridIndex>();
            GridComponent gridSize = Entity.GetComponentReadOnly<GridComponent>();

            WorldGridSystem gridSystem = PresentationSystem<DefaultPresentationGroup, WorldGridSystem>.System;

            var iter = gridSystem.GetRange(gridSize.Indices[0], m_DesireRange);

            //foreach (var item in gridSystem.GetRange(gridSize.Indices[0], m_DesireRange))
            //{
            //    if (item.Equals(targetPos[i])) return TaskStatus.Success;
            //}
            
            for (int i = 0; i < targetPos.Length; i++)
            {
                if (iter.Contains(targetPos[i]))
                {
                    return TaskStatus.Success;
                }
            }

            return TaskStatus.Failure;
        }
    }
#endif
}
