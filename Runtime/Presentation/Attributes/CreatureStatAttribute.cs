using Newtonsoft.Json;
using Syadeu.Database;

namespace Syadeu.Presentation.Attributes
{
    public sealed class CreatureStatAttribute : AttributeBase
    {
        [JsonProperty(Order = 0, PropertyName = "Stats")] public ValuePairContainer m_Stats;
    }
}
