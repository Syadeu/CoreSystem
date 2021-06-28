using Syadeu.Database;
using Syadeu.Mono.Creature;
using UnityEngine;

namespace Syadeu.Mono
{
    public sealed class CreatureStat : CreatureEntity
    {
        [SerializeField] private ValuePairContainer m_ReflectionValues;
        [SerializeField] private ValuePairContainer m_Values;

        public ValuePairContainer Values => m_Values;

        protected override void OnInitialize(CreatureBrain brain, int dataIdx)
        {
            ValuePairContainer originalValues = CreatureSettings.Instance.GetPrivateSet(dataIdx).m_Values;
            for (int i = 0; i < m_ReflectionValues.Count; i++)
            {
                if (originalValues.Contains(m_ReflectionValues[i].Hash))
                {
                    ValuePair clone = (ValuePair)originalValues.GetValuePair(m_ReflectionValues[i].Hash).Clone();
                    clone.Name = m_ReflectionValues[i].GetValue<string>();
                    m_Values.Add(clone);
                }
            }
        }
    }
}
