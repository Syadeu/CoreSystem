using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Entities;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.TurnTable
{
    public struct TRPGActorActionPointChangedUIEvent : IActorOverlayUIEvent
    {
        private ActorEventID m_EventID;
        private int
            m_From, m_To;

        public ActorEventID EventID => m_EventID;
        public int From => m_From;
        public int To => m_To;

        public TRPGActorActionPointChangedUIEvent(int from, int to)
        {
            m_EventID = ActorEventID.CreateID();
            m_From = from;
            m_To = to;
        }

        public void OnExecute(Entity<UIObjectEntity> targetUI)
        {
        }
        public void OnExecute(Entity<ActorEntity> from)
        {
        }

        [Preserve]
        static void AOTCodeGeneration()
        {
            ActorSystem.AOTCodeGenerator<TRPGActorActionPointChangedUIEvent>();

            throw new System.InvalidOperationException();
        }
    }
}
