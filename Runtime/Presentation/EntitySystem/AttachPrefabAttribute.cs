using Newtonsoft.Json;
using Syadeu.Mono;
using UnityEngine.Scripting;

namespace Syadeu.Presentation
{
    public class AttachPrefabAttribute : AttributeBase
    {
        [JsonProperty(Order = 0, PropertyName = "Prefab")] public PrefabReference m_Prefab;
    }
    [Preserve]
    internal sealed class AttachPrefabProcessor : AttributeProcessor<AttachPrefabAttribute>, IAttributeOnProxyCreated 
    {
        public void OnProxyCreated(AttributeBase attribute, IEntity entity)
        {
            "in".ToLog();
        }
    }

    public sealed class CreatureBrainAttribute : AttributeBase
    {

    }
    [Preserve]
    internal sealed class CreatureBrainProcessor : AttributeProcessor<CreatureBrainAttribute>, IAttributeOnProxyCreated
    {
        protected override void OnCreated(CreatureBrainAttribute attribute, IEntity entity)
        {
            if (entity.transform.m_EnableCull) entity.transform.SetCulling(false);


        }

        public void OnProxyCreated(AttributeBase attribute, IEntity entity)
        {
            
        }
    }
}
