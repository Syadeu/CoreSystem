using System;

#if UNITY_ADDRESSABLES
#endif

namespace Syadeu.Database
{
    public sealed class ItemInstance
    {
        private readonly Item m_Data;
        private readonly Guid m_Guid;

        private readonly ItemType[] m_ItemTypes;
        private readonly ItemEffectType[] m_ItemEffectTypes;
        private readonly ValuePairContainer m_Values;

        public Guid Guid => m_Guid;

        internal ItemInstance(Item item)
        {
            m_Data = item;
            m_Guid = Guid.NewGuid();

            m_ItemTypes = new ItemType[item.m_ItemTypes.Length];
            for (int i = 0; i < m_ItemTypes.Length; i++)
            {
                m_ItemTypes[i] = ItemDataList.Instance.GetItemType(item.m_ItemTypes[i]);
            }
            m_ItemEffectTypes = new ItemEffectType[item.m_ItemEffectTypes.Length];
            for (int i = 0; i < m_ItemEffectTypes.Length; i++)
            {
                m_ItemEffectTypes[i] = ItemDataList.Instance.GetItemEffectType(item.m_ItemEffectTypes[i]);
            }
            m_Values = (ValuePairContainer)item.m_Values.Clone();
        }
    }
}
