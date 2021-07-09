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

        public ValuePair this[Hash hash]
        {
            get
            {
                if (!m_Values.Contains(hash))
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
                        $"{Brain.DisplayName}은 Stat({hash}) 값이 없습니다.");
                }
                return m_Values.GetValuePair(hash);
            }
        }
        public ValuePair this[string name]
        {
            get
            {
#if UNITY_EDITOR
                if (!m_Values.Contains(name))
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
                        $"{Brain.DisplayName}은 Stat({name}) 값이 없습니다.");
                }
#endif
                return m_Values.GetValuePair(name);
            }
        }

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

        public static Hash ToValueHash(string name) => Hash.NewHash(name);
    }
}
