using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Actor
{
    public struct ActorLifetimeChangedEvent : IActorEvent
    {
        public enum State
        {
            Alive,
            Dead
        }

        private State m_LifeTime;

        public State LifeTime => m_LifeTime;

        public ActorLifetimeChangedEvent(State lifetime)
        {
            m_LifeTime = lifetime;
        }

        public void OnExecute(Entity<ActorEntity> from)
        {
        }
    }
}
