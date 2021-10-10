using Newtonsoft.Json;
using System.ComponentModel;

namespace Syadeu.Presentation.Data
{
    [DisplayName("Data: Trigger Bound Layer")]
    public sealed class TriggerBoundLayer : DataObjectBase
    {
        [JsonProperty(Order = 0, PropertyName = "LayerGroup")]
        public string m_LayerGroup = "Default";
    }
}
