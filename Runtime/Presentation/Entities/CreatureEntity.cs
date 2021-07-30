using Newtonsoft.Json;
using Syadeu.Database;
using System;

namespace Syadeu.Presentation.Entities
{
    [Obsolete]
    public sealed class CreatureEntity : EntityBase
    {
        [JsonProperty(Order = 0, PropertyName = "HP")] public float m_HP;
        [JsonProperty(Order = 1, PropertyName = "Values")] public ValuePairContainer m_Values = new ValuePairContainer();

        public override ObjectBase Copy()
        {
            CreatureEntity entity = (CreatureEntity)base.Copy();
            entity.m_Values = (ValuePairContainer)m_Values.Clone();

            return entity;
        }
    }
}
