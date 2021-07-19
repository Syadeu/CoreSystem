using Newtonsoft.Json;
using Syadeu.Database;

namespace Syadeu.Presentation
{
    public sealed class ObjectEntity : EntityBase
    {
        [JsonProperty(Order = 0, PropertyName = "Values")] public ValuePairContainer m_Values;

        public override object Clone()
        {
            ObjectEntity clone = (ObjectEntity)base.Clone();
            if (m_Values == null) m_Values = new ValuePairContainer();

            clone.m_Values = (ValuePairContainer)m_Values.Clone();
            return clone;
        }
    }
}
