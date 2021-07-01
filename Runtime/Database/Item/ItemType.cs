using Newtonsoft.Json;
using System;
using UnityEngine;

#if UNITY_ADDRESSABLES
#endif

namespace Syadeu.Database
{
    [Serializable]
    public sealed class ItemType : ItemTypeEntity
    {
        [Space]
        [JsonProperty(Order = 3, PropertyName = "Values")] public ValuePairContainer m_Values = new ValuePairContainer();

        [NonSerialized] private ItemTypeProxy m_Proxy = null;

        public ItemType()
        {
            m_Name = "NewItemType";
            m_Hash = Hash.NewHash();
        }
        public ItemType(string name)
        {
            m_Name = name;
            m_Hash = Hash.NewHash();
        }
        public ItemType(string name, Hash hash)
        {
            m_Name = name;
            m_Hash = hash;
        }
        internal ItemTypeProxy GetProxy()
        {
            if (m_Proxy == null)
            {
                m_Proxy = new ItemTypeProxy(this);
            }
            return m_Proxy;
        }
    }
    public sealed class ItemUseableType : ItemTypeEntity
    {
        [Space]
        [JsonProperty(Order = 3, PropertyName = "RemoveOnUse")] public bool m_RemoveOnUse = true;
        [JsonProperty(Order = 4, PropertyName = "OnUse")] public ValuePairContainer m_OnUse = new ValuePairContainer();

        [NonSerialized] private ItemUseableTypeProxy m_Proxy = null;

        public ItemUseableType()
        {
            m_Name = "NewUseableType";
            m_Hash = Hash.NewHash();
        }
        public ItemUseableType(string name)
        {
            m_Name = name;
            m_Hash = Hash.NewHash();
        }
        public ItemUseableType(string name, Hash hash)
        {
            m_Name = name;
            m_Hash = hash;
        }
        internal ItemUseableTypeProxy GetProxy()
        {
            if (m_Proxy == null)
            {
                m_Proxy = new ItemUseableTypeProxy(this);
            }
            return m_Proxy;
        }
    }
}
