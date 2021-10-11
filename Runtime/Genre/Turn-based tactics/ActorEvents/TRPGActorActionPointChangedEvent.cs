using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.TurnTable
{
    public struct TRPGActorActionPointChangedEvent : IActorEvent
    {
        private int m_To;

        public bool BurstCompile => false;
        public int To => m_To;

        public TRPGActorActionPointChangedEvent(int to)
        {
            m_To = to;
        }

        public void OnExecute(Entity<ActorEntity> from)
        {
        }
    }
}