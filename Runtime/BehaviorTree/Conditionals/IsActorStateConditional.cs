using BehaviorDesigner.Runtime.Tasks;
using Syadeu.Presentation.Actor;
using UnityEngine;

namespace Syadeu.Presentation.BehaviorTree
{
    [TaskCategory("Entity/Actor/TRPG")]
    public sealed class IsActorStateConditional : ConditionalBase
    {
        [SerializeField] private ActorStateAttribute.StateInfo m_DesireState = ActorStateAttribute.StateInfo.Idle;

        public override TaskStatus OnUpdate()
        {
            if (!Entity.IsValid()) return TaskStatus.Failure;

            var state = Entity.GetAttribute<ActorStateAttribute>();
            if (state == null) return TaskStatus.Failure;

            if ((state.State & m_DesireState) == m_DesireState)
            {
                return TaskStatus.Success;
            }

            return TaskStatus.Failure;
        }
    }
}
