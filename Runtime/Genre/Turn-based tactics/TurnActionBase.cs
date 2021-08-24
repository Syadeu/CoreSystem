using Newtonsoft.Json;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.TurnTable
{
    public abstract class TurnActionBase : ActionBase<TurnActionBase> { }

    public sealed class TestTurnAction : TurnActionBase
    {
        [JsonProperty] public string test;
    }
}
