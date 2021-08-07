using Newtonsoft.Json;

namespace Syadeu.Presentation.Entities
{
    public sealed class UIObjectEntity : EntityBase
    {
        [JsonProperty(Order = 0, PropertyName = "OnWorldCanvas")] public bool m_OnWorldCanvas;
    }
}
