using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.TurnTable
{
    public struct ActorActionPointChangedUIEvent : IActorOverlayUIEvent
    {
        private ActorEventID m_EventID;
        private int
            m_From, m_To;

        public ActorEventID EventID => m_EventID;
        public int From => m_From;
        public int To => m_To;

        public ActorActionPointChangedUIEvent(int from, int to)
        {
            m_EventID = ActorEventID.CreateID();
            m_From = from;
            m_To = to;
        }

        public void OnExecute(Entity<ActorEntity> from)
        {
        }
    }
}
