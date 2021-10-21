#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using BehaviorDesigner.Runtime.Tasks;
using Syadeu.Collections;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.TurnTable;
using UnityEngine;

namespace Syadeu.Presentation.BehaviorTree
{
#if CORESYSTEM_TURNBASESYSTEM
    [TaskCategory("Entity/Actor/TRPG")]
    public sealed class IsActorHasAPToConditional : ConditionalBase
    {
        public enum Condition
        {
            Move,
            Attack
        }

        [SerializeField] private Condition m_DesireCondition;

        public override TaskStatus OnUpdate()
        {
#if DEBUG_MODE
            if (!Entity.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Invalid Target at {nameof(NextTurnAction)}.");
                return TaskStatus.Failure;
            }
            else if (!Entity.HasComponent<TurnPlayerComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"{Entity.RawName} doesn\'t have any {nameof(TurnPlayerComponent)}.");
                return TaskStatus.Failure;
            }
#endif

            TurnPlayerComponent turnPlayer = Entity.GetComponentReadOnly<TurnPlayerComponent>();
            switch (m_DesireCondition)
            {
                case Condition.Move:
                    return IsActorCanMove(Entity, in turnPlayer) ? TaskStatus.Success : TaskStatus.Failure;
                case Condition.Attack:
                    return IsActorCanAttack(Entity, in turnPlayer) ? TaskStatus.Success : TaskStatus.Failure;
                default:
                    return TaskStatus.Failure;
            }
        }

        private static bool IsActorCanMove(Entity<IEntity> entity, in TurnPlayerComponent turnPlayer)
        {
            return turnPlayer.ActionPoint > 0;
        }
        private static bool IsActorCanAttack(Entity<IEntity> entity, in TurnPlayerComponent turnPlayer)
        {
            return turnPlayer.ActionPoint > 0;
        }
    }
#endif
}
