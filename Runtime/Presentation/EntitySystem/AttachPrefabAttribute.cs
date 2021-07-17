using Newtonsoft.Json;
using Syadeu.Mono;
using Syadeu.ThreadSafe;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Syadeu.Presentation
{
    public class AttachPrefabAttribute : AttributeBase
    {
        [JsonProperty(Order = 0, PropertyName = "Prefab")] public PrefabReference m_Prefab;

        [JsonIgnore] public DataGameObject PrefabInstance { get; internal set; }
    }
    [Preserve]
    internal sealed class AttachPrefabProcessor : AttributeProcessor<AttachPrefabAttribute>
    {
        protected override void OnCreated(AttachPrefabAttribute attribute, IEntity entity)
        {
            Vector3 pos = entity.transform.position;
            attribute.PrefabInstance = CreatePrefab(attribute.m_Prefab, pos, quaternion.identity);
        }
        //public void OnProxyCreated(AttributeBase attribute, IEntity entity)
        //{
        //    "in".ToLog();
        //}
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
