using Newtonsoft.Json;
using Syadeu.Database;

namespace Syadeu.Presentation
{
    public sealed class ObjectEntity : EntityBase
    {
        [JsonProperty(Order = 0, PropertyName = "Values")] public ValuePairContainer m_Values;
    }
}
