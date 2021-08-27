using Newtonsoft.Json;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.TurnTable
{
    public abstract class TurnActionBase : TriggerAction<TurnActionBase> { }
    public abstract class TurnStatefulActionBase : StatefulActionBase<TurnStatefulActionBase.StateContainer, TurnStatefulActionBase>
    {
        public class StateContainer : StateBase<TurnStatefulActionBase>
        {
            protected override void OnTerminate()
            {
            }
        }
    }

    public sealed class TestTurnAction : TurnActionBase
    {
        [JsonProperty] public string test;

        protected override void OnExecute(EntityData<IEntityData> entity)
        {
            $"{test}".ToLog();
        }
    }
    public sealed class TestStatefulTurnAction : TurnStatefulActionBase
    {
        protected override StateBase<TurnStatefulActionBase>.State OnExecute(in StateContainer state, in EntityData<IEntityData> entity)
        {
            

            return StateBase<TurnStatefulActionBase>.State.Success;
        }
    }
}
