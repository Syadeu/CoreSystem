using MoonSharp.Interpreter;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Syadeu.Mono;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;

#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;
#endif

namespace Syadeu.Database
{
    [Serializable]
    public sealed class Item
    {
        [JsonProperty(Order = 0, PropertyName = "Name")] public string m_Name;
        [JsonProperty(Order = 1, PropertyName = "Hash")] public Hash m_Hash;
#if UNITY_ADDRESSABLES
        [JsonConverter(typeof(AssetReferenceJsonConverter))]
        [JsonProperty(Order = 2, PropertyName = "ImagePath")] public AssetReference m_ImagePath;
#endif

        [Tooltip("Hash")]
        /// <summary>
        /// <see cref="ItemType"/>
        /// </summary>
        [JsonProperty(Order = 3, PropertyName = "ItemTypes")] public ulong[] m_ItemTypes = new ulong[0];
        [Tooltip("Hash")]
        /// <summary>
        /// <see cref="ItemEffectType"/>
        /// </summary>
        [JsonProperty(Order = 4, PropertyName = "ItemEffectTypes")] public ulong[] m_ItemEffectTypes = new ulong[0];
        [JsonProperty(Order = 5, PropertyName = "Values")] public ValuePairContainer m_Values = new ValuePairContainer();

        [NonSerialized] private ItemProxy m_Proxy = null;

        [NonSerialized] private List<ItemInstance> m_Instances = new List<ItemInstance>();

        [NonSerialized] public Action m_OnEquip;
        [NonSerialized] public Action m_OnUnequip;
        [NonSerialized] public Action m_OnUse;
        [NonSerialized] public Action m_OnDrop;

        public Item()
        {
            m_Name = "NewItem";
            m_Hash = Hash.NewHash();
        }
        public Item(string name)
        {
            m_Name = name;
            m_Hash = Hash.NewHash();
        }

        internal ItemProxy GetProxy()
        {
            if (m_Proxy == null)
            {
                m_Proxy = new ItemProxy(this);
            }
            return m_Proxy;
        }

        #region Instance
        public ItemInstance CreateInstance()
        {
            ItemInstance instance = new ItemInstance(this);
            m_Instances.Add(instance);
            return instance;
        }
        public ItemInstance GetInstance(Guid guid)
        {
            for (int i = 0; i < m_Instances.Count; i++)
            {
                if (m_Instances[i].Hash.Equals(guid))
                {
                    return m_Instances[i];
                }
            }

            throw new Exception();
        }
        #endregion
    }
}
