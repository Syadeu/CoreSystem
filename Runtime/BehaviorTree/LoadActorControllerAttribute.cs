using BehaviorDesigner.Runtime.Tasks;
using Syadeu.Presentation.Actor;
using UnityEngine;

namespace Syadeu.Presentation.BehaviorTree
{
    [TaskCategory("Entity/Attributes")]
    public sealed class LoadActorControllerAttribute : Action
    {
        [SerializeField] private SharedRecycleableMonobehaviour m_Target;

        private ActorControllerAttribute m_ActorController;

        public ActorControllerAttribute ActorController => m_ActorController;

        public override TaskStatus OnUpdate()
            => PresentationBehaviorTreeUtility.LoadAttributeFromMono(m_Target, nameof(LoadActorControllerAttribute), out m_ActorController);
    }
}
