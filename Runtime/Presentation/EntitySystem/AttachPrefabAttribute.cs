using Newtonsoft.Json;
using Syadeu.Mono;

namespace Syadeu.Presentation
{
    public class AttachPrefabAttribute : AttributeBase
    {
        [JsonProperty(Order = 0, PropertyName = "Prefab")] public PrefabReference m_Prefab;
    }
    public sealed class AttachPrefabProcessor : AttributeProcessor<AttachPrefabAttribute>, IAttributeOnProxyCreated 
    {
        public void OnProxyCreated(AttributeBase attribute, DataGameObject dataObj)
        {
            "in".ToLog();
        }
    }
}
