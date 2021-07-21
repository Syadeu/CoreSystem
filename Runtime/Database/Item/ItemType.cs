using Newtonsoft.Json;
using System;
using UnityEngine;

using Syadeu.Database.Lua;
using Syadeu.Presentation;

namespace Syadeu.Database
{
    [Serializable]
    [System.Obsolete("", true)]
    public sealed class ItemType : AttributeBase
    {
        [Space]
        [JsonProperty(Order = 3, PropertyName = "Values")] public ValuePairContainer m_Values = new ValuePairContainer();

        [NonSerialized] private ItemTypeProxy m_Proxy = null;

        public ItemType()
        {
            Name = "NewItemType";
            Hash = Hash.NewHash();
        }
        public ItemType(string name)
        {
            Name = name;
            Hash = Hash.NewHash();
        }
        public ItemType(string name, Hash hash)
        {
            Name = name;
            Hash = hash;
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
    [System.Obsolete("", true)]
    public sealed class ItemUseableType : AttributeBase
    {
        [Space]
        [JsonProperty(Order = 3, PropertyName = "RemoveOnUse")] public bool m_RemoveOnUse = true;
        [JsonProperty(Order = 4, PropertyName = "OnUse")] public ValuePairContainer m_OnUse = new ValuePairContainer();

        [NonSerialized] private ItemUseableTypeProxy m_Proxy = null;

        public ItemUseableType()
        {
            Name = "NewUseableType";
            Hash = Hash.NewHash();
        }
        public ItemUseableType(string name)
        {
            Name = name;
            Hash = Hash.NewHash();
        }
        public ItemUseableType(string name, Hash hash)
        {
            Name = name;
            Hash = hash;
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
