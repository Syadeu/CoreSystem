using Newtonsoft.Json;
using Syadeu.Database;

namespace Syadeu.Presentation.Actor
{
    public sealed class ActorStatAttribute : ActorAttributeBase
    {
        [JsonProperty(Order = 0, PropertyName = "Stats")] private ValuePairContainer m_Stats = new ValuePairContainer();

        [JsonIgnore] public ValuePairContainer Stats => m_Stats;

        public static Hash ToValueHash(string name) => Hash.NewHash(name);
    }
}
