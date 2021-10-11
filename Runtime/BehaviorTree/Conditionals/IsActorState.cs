using BehaviorDesigner.Runtime.Tasks;
using Syadeu.Presentation.Actor;
using System;
using UnityEngine;

namespace Syadeu.Presentation.BehaviorTree
{
    [TaskCategory("Entity/Actor"), Obsolete("Use IsActorStateConditional")]
    public sealed class IsActorState : Conditional
    {
        [SerializeField] private SharedRecycleableMonobehaviour m_This;

        [Space]
        [SerializeField] private ActorStateAttribute.StateInfo m_DesireState = ActorStateAttribute.StateInfo.Idle;

        private ActorStateAttribute m_ActorState;

        public override void OnAwake()
        {
            if (!m_This.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Shared Variable is not set");
                return;
            }

            m_ActorState = m_This.GetAttribute<ActorStateAttribute>();
            if (m_ActorState == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This entity({m_This.GetEntity().Name}) doesn\'t have any {nameof(ActorStateAttribute)}.");
                return;
            }
        }
        public override TaskStatus OnUpdate()
        {
            if (!m_This.IsValid()) return TaskStatus.Failure;

            if ((m_ActorState.State & m_DesireState) == m_DesireState)
            {
                return TaskStatus.Success;
            }
            return TaskStatus.Failure;
        }
    }
}
