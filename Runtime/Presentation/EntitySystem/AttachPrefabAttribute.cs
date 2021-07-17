using Newtonsoft.Json;
using Syadeu.Database;
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
    internal sealed class CreatureBrainProcessor : AttributeProcessor<CreatureBrainAttribute>, IAttributeOnProxyCreatedSync
    {
        protected override void OnCreated(CreatureBrainAttribute attribute, IEntity entity)
        {
            if (entity.transform.m_EnableCull) entity.transform.SetCulling(false);


        }
        public void OnProxyCreatedSync(AttributeBase attribute, IEntity entity)
        {
            CreatureBrain brain = (CreatureBrain)entity.gameObject.GetProxyObject();

            brain.Initialize();
        }
    }

    public sealed class CreatureStatAttribute : AttributeBase
    {
        [JsonProperty(Order = 0, PropertyName = "Stats")] public ValuePairContainer m_Stats;
    }
    [Preserve]
    internal sealed class CreatureStatProcessor : AttributeProcessor<CreatureStatAttribute>, IAttributeOnProxyCreatedSync
    {
        public void OnProxyCreatedSync(AttributeBase attribute, IEntity entity)
        {
            CreatureBrain brain = (CreatureBrain)entity.gameObject.GetProxyObject();

            CreatureStat stat = brain.Stat;
            if (stat == null)
            {
                stat = brain.gameObject.AddComponent<CreatureStat>();
                brain.InitializeCreatureEntity(stat);
            }

            stat.Values = ((CreatureStatAttribute)attribute).m_Stats;
        }
    }
}
