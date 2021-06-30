using Syadeu.Database;
using Syadeu.Mono.Creature;
using UnityEngine;

namespace Syadeu.Mono
{
    public sealed class CreatureStat : CreatureEntity
    {
        [SerializeField] private ValuePairContainer m_ReflectionValues = new ValuePairContainer();
        [SerializeField] private ValuePairContainer m_Values = new ValuePairContainer();

        public ValuePairContainer Values => m_Values;

        public ValuePair this[uint hash] => m_Values.GetValuePair(hash);
        public ValuePair this[string name] => m_Values.GetValuePair(name);

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
                else
                {
                    $"{m_ReflectionValues[i].Name} 의 값을 찾을 수 없음".ToLogError();
                }
            }
        }

        public static uint ToValueHash(string name) => FNV1a32.Calculate(name);
    }
}
