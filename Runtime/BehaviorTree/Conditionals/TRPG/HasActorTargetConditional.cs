#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using BehaviorDesigner.Runtime.Tasks;
using Syadeu.Collections;
using Syadeu.Presentation.TurnTable;

namespace Syadeu.Presentation.BehaviorTree
{
#if CORESYSTEM_TURNBASESYSTEM
    [TaskCategory("Entity/Actor/TRPG")]
    public sealed class HasActorTargetConditional : ConditionalBase
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
#endif
            if (Entity.GetComponentReadOnly<TRPGActorAttackComponent>().TargetCount > 0)
            {
                $"has target {Entity.GetComponentReadOnly<TRPGActorAttackComponent>().GetTargetAt(0).GetEntity<IEntity>().Name}".ToLog();
                return TaskStatus.Success;
            }

            return TaskStatus.Failure;
        }
    }
#endif
}
