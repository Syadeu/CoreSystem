using Newtonsoft.Json;
using Syadeu.Database.Lua;
using Syadeu.Mono;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_ADDRESSABLES
#endif

namespace Syadeu.Database
{
    [Serializable] [JsonConverter(typeof(Converters.ItemInstanceJsonConverter))]
    [System.Obsolete("", true)]
    public sealed class ItemInstance : IDisposable, IValidation
    {
        private readonly Item m_Data;
        private readonly Hash m_Hash;

        //private readonly ItemTypeEntity[] m_ItemTypes;
        //private readonly ItemEffectType[] m_ItemEffectTypes;
        private readonly ValuePairContainer m_Values;

        [NonSerialized] private ItemInstanceProxy m_LuaProxy = null;

        internal GameObject m_ProxyObject;

        public Item Data => m_Data;
        public Hash Hash => m_Hash;

        //public ItemTypeEntity[] ItemTypes => m_ItemTypes;
        //public ItemEffectType[] EffectTypes => m_ItemEffectTypes;
        public ValuePairContainer Values => m_Values;

        public bool Disposed { get; private set; } = false;

        internal ItemInstance(Item item)
        {
            m_Data = item;
            m_Hash = Hash.NewHash();

            //m_ItemTypes = new ItemTypeEntity[item.m_ItemTypes.Length];
            //for (int i = 0; i < m_ItemTypes.Length; i++)
            //{
            //    m_ItemTypes[i] = ItemDataList.Instance.GetItemType(item.m_ItemTypes[i]);
            //}
            //m_ItemEffectTypes = new ItemEffectType[item.m_ItemEffectTypes.Length];
            //for (int i = 0; i < m_ItemEffectTypes.Length; i++)
            //{
            //    m_ItemEffectTypes[i] = ItemDataList.Instance.GetItemEffectType(item.m_ItemEffectTypes[i]);
            //}
            m_Values = (ValuePairContainer)item.m_Values.Clone();

            Data.m_OnSpawn?.Invoke(this);
        }
        internal ItemInstance(Hash dataHash, Hash hash)
        {
            //Item item = ItemDataList.Instance.GetItem(dataHash);

            //m_Data = item;
            //m_Hash = hash;

            ////m_ItemTypes = new ItemType[item.m_ItemTypes.Length];
            ////for (int i = 0; i < m_ItemTypes.Length; i++)
            ////{
            ////    m_ItemTypes[i] = ItemDataList.Instance.GetItemType(item.m_ItemTypes[i]);
            ////}
            ////m_ItemEffectTypes = new ItemEffectType[item.m_ItemEffectTypes.Length];
            ////for (int i = 0; i < m_ItemEffectTypes.Length; i++)
            ////{
            ////    m_ItemEffectTypes[i] = ItemDataList.Instance.GetItemEffectType(item.m_ItemEffectTypes[i]);
            ////}
            //m_Values = (ValuePairContainer)item.m_Values.Clone();

            //Data.m_OnSpawn?.Invoke(this);
        }

        public bool IsValid()
        {
            if (m_Data == null || m_Hash.Equals(Guid.Empty)) return false;
            return true;
        }
        internal ItemInstanceProxy GetLuaProxy()
        {
            if (m_LuaProxy == null) m_LuaProxy = new ItemInstanceProxy(this);
            return m_LuaProxy;
        }

        //public bool HasType<T>() where T : ItemTypeEntity => m_ItemTypes.Where((other) => other is T).Count() != 0;
        //public bool HasType(Hash hash) => m_ItemTypes.Where((other) => other.Hash.Equals(hash)).Count() != 0;
        //public ItemTypeEntity[] GetTypes<T>() where T : ItemTypeEntity => m_ItemTypes.Where((other) => other is T).ToArray();
        //public ItemTypeEntity GetType(Hash hash) => m_ItemTypes.Where((other) => other.Hash.Equals(hash)).First();
        //public T GetType<T>() where T : ItemTypeEntity => (T)m_ItemTypes.Where((other) => other is T).First();
        //public T GetType<T>(Hash hash) where T : ItemTypeEntity => (T)GetType(hash);

        public void Dispose()
        {
            Disposed = true;
        }

        public override string ToString() => m_Data == null ? "NULL" : m_Data.Name;
    }
}
