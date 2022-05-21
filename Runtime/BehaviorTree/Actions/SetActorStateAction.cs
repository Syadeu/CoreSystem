#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using BehaviorDesigner.Runtime.Tasks;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Entities;
using UnityEngine;

namespace Syadeu.Presentation.BehaviorTree
{
    [TaskCategory("Entity/Actor")]
    public sealed class SetActorStateAction : ActionBase
    {
        [SerializeField] private ActorStateAttribute.StateInfo m_DesireState = ActorStateAttribute.StateInfo.Idle;

        public override TaskStatus OnUpdate()
        {
#if DEBUG_MODE
            if (!Entity.IsValid())
            {
                CoreSystem.Logger.LogError(LogChannel.Entity,
                    $"Invalid Target at {nameof(NextTurnAction)}.");
                return TaskStatus.Failure;
            }
            else if (!Entity.HasAttribute<ActorStateAttribute>())
            {
                CoreSystem.Logger.LogError(LogChannel.Entity,
                    $"{Entity.RawName} doesn\'t have any {nameof(ActorStateAttribute)}.");
                return TaskStatus.Failure;
            }
#endif

            ActorStateAttribute state = Entity.GetAttribute<ActorStateAttribute>();
            state.State = m_DesireState;

            return TaskStatus.Success;
        }
    }
}
