using Newtonsoft.Json;
using Syadeu.Database;
using System;

namespace Syadeu.Presentation
{
    [Serializable]
    public sealed class CreatureEntity : EntityBase
    {
        [JsonProperty(Order = 0, PropertyName = "HP")] public float m_HP;
        [JsonProperty(Order = 1, PropertyName = "Values")] public ValuePairContainer m_Values = new ValuePairContainer();

        public override object Clone()
        {
            CreatureEntity entity = (CreatureEntity)base.Clone();
            entity.m_Values = (ValuePairContainer)m_Values.Clone();

            return entity;
        }
    }
}
