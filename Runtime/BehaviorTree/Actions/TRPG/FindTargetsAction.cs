#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using BehaviorDesigner.Runtime.Tasks;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Components;

namespace Syadeu.Presentation.BehaviorTree
{
#if CORESYSTEM_TURNBASESYSTEM
    using Syadeu.Presentation.TurnTable;

    [TaskCategory("Entity/Actor/TRPG")]
    public sealed class FindTargetsAction : ActionBase
    {
        [UnityEngine.SerializeField] private int m_DesireRange = 1;

        public override TaskStatus OnUpdate()
        {
#if DEBUG_MODE
            if (!Entity.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Invalid Target at {nameof(FindTargetsAction)}.");
                return TaskStatus.Failure;
            }
            else if (!Entity.HasComponent<ActorControllerComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({Entity.RawName}) doesn\'t have {nameof(ActorControllerAttribute)}");
                return TaskStatus.Failure;
            }
#endif
            var ctr = Entity.GetComponent<ActorControllerComponent>();
#if DEBUG_MODE
            if (!ctr.HasProvider<TRPGActorAttackProvider>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({Entity.RawName}) doesn\'t have {nameof(TRPGActorAttackProvider)}");
                return TaskStatus.Failure;
            }
#endif
            var temp = ctr.GetProvider<TRPGActorAttackProvider>().Target.GetTargetsWithin(m_DesireRange);
            //if (temp.Length > 0)
            //{

            //}

            $"{temp.Length} found".ToLog();
            return TaskStatus.Success;
        }
    }
#endif
        }
