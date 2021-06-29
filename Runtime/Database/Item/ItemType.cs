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
        [JsonProperty(Order = 3)] public ValuePairContainer m_Values = new ValuePairContainer();

        [NonSerialized] private ItemTypeProxy m_Proxy = null;

        public ItemType()
        {
            m_Name = "NewItemType";
            m_Guid = Guid.NewGuid().ToString();
        }
        public ItemType(string name)
        {
            m_Name = name;
            m_Guid = Guid.NewGuid().ToString();
        }
        public ItemType(string name, string guid)
        {
            m_Name = name;
            m_Guid = guid;
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
        [JsonProperty(Order = 3)] public bool m_RemoveOnUse = true;
        [JsonProperty(Order = 4)] public ValuePairContainer m_OnUse = new ValuePairContainer();

        [NonSerialized] private ItemUseableTypeProxy m_Proxy = null;

        public ItemUseableType()
        {
            m_Name = "NewUseableType";
            m_Guid = Guid.NewGuid().ToString();
        }
        public ItemUseableType(string name)
        {
            m_Name = name;
            m_Guid = Guid.NewGuid().ToString();
        }
        public ItemUseableType(string name, string guid)
        {
            m_Name = name;
            m_Guid = guid;
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
