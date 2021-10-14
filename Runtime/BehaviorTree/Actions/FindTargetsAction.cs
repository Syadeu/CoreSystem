using BehaviorDesigner.Runtime.Tasks;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.TurnTable;

namespace Syadeu.Presentation.BehaviorTree
{
#if CORESYSTEM_TURNBASESYSTEM
    [TaskCategory("Entity/Actor")]
    public sealed class FindTargetsAction : ActionBase
    {
        public override TaskStatus OnUpdate()
        {
            if (!Entity.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Invalid Target at {nameof(FindTargetsAction)}.");
                return TaskStatus.Failure;
            }
            else if (!Entity.HasComponent<ActorControllerComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({Entity.RawName}) doeesn\'t have {nameof(ActorControllerAttribute)}");
                return TaskStatus.Failure;
            }

            var ctr = Entity.GetComponent<ActorControllerComponent>();

            if (!ctr.HasProvider<TRPGActorAttackProvider>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({Entity.RawName}) doeesn\'t have {nameof(TRPGActorAttackProvider)}");
                return TaskStatus.Failure;
            }

            ctr.GetProvider<TRPGActorAttackProvider>().Object.GetTargetsInRange();

            return TaskStatus.Success;
        }
    }
#endif
}
