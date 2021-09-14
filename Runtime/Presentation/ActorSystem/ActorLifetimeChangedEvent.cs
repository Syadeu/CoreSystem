using Syadeu.Database;
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

        private readonly ActorEventID m_EventID;
        private State m_LifeTime;

        public ActorEventID EventID => m_EventID;
        public State LifeTime => m_LifeTime;

        public ActorLifetimeChangedEvent(State lifetime)
        {
            m_EventID = ActorEventID.CreateID();
            m_LifeTime = lifetime;
        }

        public void OnExecute(Entity<ActorEntity> from)
        {
            $"Lifetime changed {from.Name} -> {m_LifeTime}".ToLog();
        }
    }
}
