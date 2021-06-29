using Newtonsoft.Json;
using System;
using System.Collections.Generic;

using UnityEngine;
#if UNITY_ADDRESSABLES
#endif

namespace Syadeu.Database
{
    [Serializable] [JsonConverter(typeof(ItemInstanceJsonConverter))]
    public sealed class ItemInstance
    {
        private readonly Item m_Data;
        private readonly Guid m_Guid;

        private readonly ItemTypeEntity[] m_ItemTypes;
        private readonly ItemEffectType[] m_ItemEffectTypes;
        private readonly ValuePairContainer m_Values;

        public Item Data => m_Data;
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
        internal ItemInstance(string dataGuid, string guid)
        {
            Item item = ItemDataList.Instance.GetItem(dataGuid);

            m_Data = item;
            m_Guid = Guid.Parse(guid);

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

    [Serializable]
    public sealed class ItemContainer
    {
        [JsonIgnore] private IItemListener m_Listener;
        [SerializeField][JsonProperty(Order = 0)] private readonly List<ItemInstance> m_Items = new List<ItemInstance>();
        
        public List<ItemInstance> Items => m_Items;

        public ItemContainer(IItemListener listener)
        {
            SetListener(listener);
        }

        public ItemContainer SetListener(IItemListener listener)
        {
            m_Listener = listener;
            return this;
        }
    }

    public interface IItemListener
    {

    }
}
