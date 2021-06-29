using Newtonsoft.Json;
using Syadeu.Mono;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_ADDRESSABLES
#endif

namespace Syadeu.Database
{
    [Serializable] [JsonConverter(typeof(ItemInstanceJsonConverter))]
    public sealed class ItemInstance : IDisposable
    {
        private readonly Item m_Data;
        private readonly Guid m_Guid;

        private readonly ItemTypeEntity[] m_ItemTypes;
        private readonly ItemEffectType[] m_ItemEffectTypes;
        private readonly ValuePairContainer m_Values;

        public Item Data => m_Data;
        public Guid Guid => m_Guid;

        public ItemTypeEntity[] ItemTypes => m_ItemTypes;
        public ItemEffectType[] EffectTypes => m_ItemEffectTypes;
        public ValuePairContainer Values => m_Values;

        public bool Disposed { get; private set; } = false;

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

        public bool HasType<T>() where T : ItemTypeEntity => m_ItemTypes.Where((other) => other is T).Count() != 0;
        public bool HasType(string guid) => m_ItemTypes.Where((other) => other.m_Guid.Equals(guid)).Count() != 0;
        public ItemTypeEntity[] GetTypes<T>() where T : ItemTypeEntity => m_ItemTypes.Where((other) => other is T).ToArray();
        public ItemTypeEntity GetType(string guid) => m_ItemTypes.Where((other) => other.m_Guid.Equals(guid)).First();
        public T GetType<T>() where T : ItemTypeEntity => (T)m_ItemTypes.Where((other) => other is T).First();
        public T GetType<T>(string guid) where T : ItemTypeEntity => (T)GetType(guid);

        public void Dispose()
        {
            Disposed = true;
        }
    }

    [Serializable]
    public sealed class ItemContainer
    {
        [JsonIgnore] private CreatureBrain m_Listener;
        [SerializeField][JsonProperty(Order = 0)] private readonly List<ItemInstance> m_Items = new List<ItemInstance>();
        
        public List<ItemInstance> Items => m_Items;

        public ItemContainer(CreatureBrain listener)
        {
            SetListener(listener);
        }

        public ItemContainer SetListener(CreatureBrain listener)
        {
            m_Listener = listener;
            return this;
        }

        public void Insert(ItemInstance item)
        {
            //if (item.HasType<ItemUseableType>())
            //{
            //    ItemUseableType useableType = item.GetType<ItemUseableType>();
            //    for (int i = 0; i < useableType.m_OnUse.Count; i++)
            //    {
            //        useableType.m_OnUse[i].GetValue<Action>().Invoke();
            //    }
            //}

            //for (int i = 0; i < item.Values.Count; i++)
            //{
            //    item.Values[i].
            //}
        }
        public void Use(int i)
        {
            ItemInstance item = m_Items[i];

            if (item.HasType<ItemUseableType>())
            {
                ItemUseableType useableType = item.GetType<ItemUseableType>();
                for (int j = 0; j < useableType.m_OnUse.Count; j++)
                {
                    if (useableType.m_OnUse[j] is SerializableClosureValuePair closure)
                    {
                        closure.Invoke(m_Listener);
                    }
                    else if (useableType.m_OnUse[j] is ValueFuncPair<Action<CreatureBrainProxy>> action)
                    {
                        action.Invoke(m_Listener);
                    }
                }

                if (useableType.m_RemoveOnUse)
                { 
                    m_Items[i].Dispose();
                    m_Items.RemoveAt(i);
                }
            }
            else
            {
                $"Item: {item.Data.m_Name} is not a type of useable".ToLog();
            }
        }
    }
}
