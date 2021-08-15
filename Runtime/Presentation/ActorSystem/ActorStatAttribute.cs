using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Actor
{
    public sealed class ActorStatAttribute : ActorAttributeBase
    {
        [JsonProperty(Order = 0, PropertyName = "Stats")] private ValuePairContainer m_Stats = new ValuePairContainer();

        [JsonIgnore] private ValuePairContainer m_CurrentStats;

        internal void Initialize()
        {
            m_CurrentStats = (ValuePairContainer)m_Stats.Clone();
        }
        protected override void OnDispose()
        {
            m_CurrentStats = null;

            base.OnDispose();
        }

        public void Reset()
        {
            m_CurrentStats = (ValuePairContainer)m_Stats.Clone();
        }
        public T GetOriginalValue<T>(string name) => m_Stats.GetValue<T>(name);
        public T GetOriginalValue<T>(Hash hash) => m_Stats.GetValue<T>(hash);
        public T GetValue<T>(string name) => m_CurrentStats.GetValue<T>(name);
        public T GetValue<T>(Hash hash) => m_CurrentStats.GetValue<T>(hash);
        public void SetValue<T>(string name, T value) => m_CurrentStats.SetValue(name, value);
        public void SetValue<T>(Hash hash, T value) => m_CurrentStats.SetValue(hash, value);

        public static Hash ToValueHash(string name) => Hash.NewHash(name);
    }
    [Preserve]
    internal sealed class ActorStatProcessor : AttributeProcessor<ActorStatAttribute>
    {
        protected override void OnCreated(ActorStatAttribute attribute, EntityData<IEntityData> entity)
        {
            attribute.Initialize();
        }
    }
}
