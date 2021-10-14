using BehaviorDesigner.Runtime.Tasks;
using Syadeu.Collections;
using Syadeu.Presentation.TurnTable;

namespace Syadeu.Presentation.BehaviorTree
{
#if CORESYSTEM_TURNBASESYSTEM
    [TaskCategory("Entity/Actor")]
    public sealed class HasActorTargetConditional : ConditionalBase
    {
        public override TaskStatus OnUpdate()
        {
            if (!Entity.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Invalid Target at {nameof(NextTurnAction)}.");
                return TaskStatus.Failure;
            }

            if (Entity.GetComponentReadOnly<TRPGActorAttackComponent>().TargetCount > 0)
            {
                $"has target {Entity.GetComponentReadOnly<TRPGActorAttackComponent>().m_Targets[0].GetEntity<IEntity>().Name}".ToLog();
                return TaskStatus.Success;
            }

            return TaskStatus.Failure;
        }
    }
#endif
}
