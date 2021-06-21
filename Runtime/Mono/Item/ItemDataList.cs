using System;
using UnityEngine;

namespace Syadeu.Database
{
    [PreferBinarySerialization][CustomStaticSetting("Syadeu/Item")]
    public sealed class ItemDataList : StaticSettingEntity<ItemDataList>
    {
        public override bool RuntimeModifiable => true;

        public Item[] m_Items;
        public ItemType[] m_ItemTypes;
        public ItemEffectType[] m_ItemEffectTypes;

        public override void OnInitialize()
        {
            
        }
#if UNITY_EDITOR
        private void OnValidate()
        {
            for (int i = 0; i < m_Items?.Length; i++)
            {
                if (string.IsNullOrEmpty(m_Items[i].m_Guid))
                {
                    m_Items[i].m_Guid = Guid.NewGuid().ToString();
                }
            }
            for (int i = 0; i < m_ItemTypes?.Length; i++)
            {
                if (string.IsNullOrEmpty(m_ItemTypes[i].m_Guid))
                {
                    m_ItemTypes[i].m_Guid = Guid.NewGuid().ToString();
                }
            }
            for (int i = 0; i < m_ItemEffectTypes?.Length; i++)
            {
                if (string.IsNullOrEmpty(m_ItemEffectTypes[i].m_Guid))
                {
                    m_ItemEffectTypes[i].m_Guid = Guid.NewGuid().ToString();
                }
            }
        }
#endif

        public Item GetItem(string guid)
        {
            for (int i = 0; i < m_Items.Length; i++)
            {
                if (m_Items[i].m_Guid.Equals(guid))
                {
                    return m_Items[i];
                }
            }
            return null;
        }
        public ItemType GetItemType(string guid)
        {
            for (int i = 0; i < m_ItemTypes.Length; i++)
            {
                if (m_ItemTypes[i].m_Guid.Equals(guid))
                {
                    return m_ItemTypes[i];
                }
            }
            return null;
        }
        public ItemEffectType GetItemEffectType(string guid)
        {
            for (int i = 0; i < m_ItemEffectTypes.Length; i++)
            {
                if (m_ItemEffectTypes[i].m_Guid.Equals(guid))
                {
                    return m_ItemEffectTypes[i];
                }
            }
            return null;
        }
    }
}
