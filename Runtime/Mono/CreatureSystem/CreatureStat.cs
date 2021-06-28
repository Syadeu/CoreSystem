using Syadeu.Database;
using Syadeu.Mono.Creature;
using UnityEngine;

namespace Syadeu.Mono
{
    public sealed class CreatureStat : CreatureEntity
    {
        [SerializeField] private ValuePairContainer m_Values;

        public ValuePairContainer Values => m_Values;

        protected override void OnInitialize(CreatureBrain brain, int dataIdx)
        {
            m_Values = (ValuePairContainer)CreatureSettings.Instance.GetPrivateSet(dataIdx).m_Values.Clone();
        }
    }
}
