using Syadeu.Collections;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Actions
{
    public abstract class StateBase<TAction> : ITerminate
        where TAction : ActionBase
    {
        public enum State
        {
            Wait            =   0,
            AboutToExecute  =   1,
            Executing       =   2,

            Success         =   3,
            Failure         =   4
        }

        internal TAction Action { get; set; }
        public EntityData<IEntityData> Entity { get; internal set; }
        public State CurrentState { get; internal set; } = 0;

        protected abstract void OnTerminate();

        void ITerminate.Terminate()
        {
            OnTerminate();

            Entity = EntityData<IEntityData>.Empty;
            CurrentState = 0;
        }
    }
}
