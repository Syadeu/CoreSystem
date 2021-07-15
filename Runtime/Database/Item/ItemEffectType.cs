using Newtonsoft.Json;
using System;
using UnityEngine;

using Syadeu.Database.Lua;

namespace Syadeu.Database
{
    [Serializable]
    public sealed class ItemEffectType : IAttribute
    {
        public string Name { get; set; }
        public Hash Hash { get; set; }

        [Space]
        [JsonProperty(Order = 2, PropertyName = "Values")] public ValuePairContainer m_Values = new ValuePairContainer();

        [NonSerialized][JsonIgnore] private ItemEffectTypeProxy m_Proxy = null;

        public ItemEffectType()
        {
            Name = "NewItemEffectType";
            Hash = Hash.NewHash();
        }
        public ItemEffectType(string name)
        {
            Name = name;
            Hash = Hash.NewHash();
        }

        internal ItemEffectTypeProxy GetProxy()
        {
            if (m_Proxy == null) m_Proxy = new ItemEffectTypeProxy(this);
            return m_Proxy;
        }
    }
}
