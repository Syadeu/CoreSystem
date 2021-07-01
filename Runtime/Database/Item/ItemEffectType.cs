using Newtonsoft.Json;
using System;
using UnityEngine;

#if UNITY_ADDRESSABLES
#endif

namespace Syadeu.Database
{
    [Serializable]
    public sealed class ItemEffectType
    {
        [JsonProperty(Order = 0, PropertyName = "Name")] public string m_Name;
        [JsonProperty(Order = 1, PropertyName = "Hash")] public Hash m_Hash;

        [Space]
        [JsonProperty(Order = 2, PropertyName = "Values")] public ValuePairContainer m_Values = new ValuePairContainer();

        [NonSerialized][JsonIgnore] private ItemEffectTypeProxy m_Proxy = null;

        public ItemEffectType()
        {
            m_Name = "NewItemEffectType";
            m_Hash = Hash.NewHash();
        }
        public ItemEffectType(string name)
        {
            m_Name = name;
            m_Hash = Hash.NewHash();
        }

        internal ItemEffectTypeProxy GetProxy()
        {
            if (m_Proxy == null) m_Proxy = new ItemEffectTypeProxy(this);
            return m_Proxy;
        }
    }
}
