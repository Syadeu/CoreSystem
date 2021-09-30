using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using System;
using System.ComponentModel;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Actor
{
    [DisplayName("Attribute: Actor Stat")]
    public sealed class ActorStatAttribute : ActorAttributeBase
    {
        [JsonProperty(Order = 0, PropertyName = "Stats")] private ValuePairContainer m_Stats = new ValuePairContainer();

        [JsonIgnore] private ValuePairContainer m_CurrentStats;

        public event Action<ActorStatAttribute, Hash, object> OnValueChanged;

        internal void Initialize()
        {
            m_CurrentStats = (ValuePairContainer)m_Stats.Clone();
        }
        protected override void OnDispose()
        {
            m_CurrentStats = null;

            base.OnDispose();
        }

        public T GetOriginalValue<T>(string name) => m_Stats.GetValue<T>(name);
        public T GetOriginalValue<T>(Hash hash) => m_Stats.GetValue<T>(hash);
        public T GetValue<T>(string name) => m_CurrentStats.GetValue<T>(name);
        public T GetValue<T>(Hash hash) => m_CurrentStats.GetValue<T>(hash);
        public void SetValue<T>(string name, T value) => SetValue(ToValueHash(name), value);
        public void SetValue<T>(Hash hash, T value)
        {
            m_CurrentStats.SetValue(hash, value);
            try
            {
                OnValueChanged?.Invoke(this, hash, value);
            }
            catch (Exception ex)
            {
                CoreSystem.Logger.LogError(Channel.Entity, ex, nameof(SetValue));
            }
        }

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
