using Syadeu.Collections;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Actor
{
    public struct ActorAttackEvent : IActorEvent
    {
        private EntityID m_Target;

        public bool BurstCompile => true;
        public EntityID Target => m_Target;

        public ActorAttackEvent(IEntityDataID target)
        {
            m_Target = target.Idx;
        }

        void IActorEvent.OnExecute(Entity<ActorEntity> from)
        {

        }

        [UnityEngine.Scripting.Preserve]
        static void AOTCodeGeneration()
        {
            ActorSystem.AOTCodeGenerator<ActorAttackEvent>();

            throw new System.InvalidOperationException();
        }
    }
}
