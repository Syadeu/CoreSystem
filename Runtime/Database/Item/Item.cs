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

using Syadeu.Database.Lua;

namespace Syadeu.Database
{
    [Serializable]
    public sealed class Item
    {
        [JsonProperty(Order = 0, PropertyName = "Name")] public string m_Name;
        [JsonProperty(Order = 1, PropertyName = "Hash")] public Hash m_Hash;
        [JsonConverter(typeof(Converters.AssetReferenceJsonConverter))]
        [JsonProperty(Order = 2, PropertyName = "ImagePath")] public AssetReference m_ImagePath;
        /// <summary>
        /// <see cref="PrefabList.ObjectSettings"/>
        /// </summary>
        [JsonProperty(Order = 3, PropertyName = "PrefabIdx")] public int m_PrefabIdx;

        [Tooltip("Hash")]
        /// <summary>
        /// <see cref="ItemType"/>
        /// </summary>
        [JsonProperty(Order = 4, PropertyName = "ItemTypes")] public ulong[] m_ItemTypes = new ulong[0];
        [Tooltip("Hash")]
        /// <summary>
        /// <see cref="ItemEffectType"/>
        /// </summary>
        [JsonProperty(Order = 5, PropertyName = "ItemEffectTypes")] public ulong[] m_ItemEffectTypes = new ulong[0];
        [JsonProperty(Order = 6, PropertyName = "Values")] public ValuePairContainer m_Values = new ValuePairContainer();

        [NonSerialized] private ItemProxy m_Proxy = null;

        [NonSerialized] private List<ItemInstance> m_Instances = new List<ItemInstance>();

        [NonSerialized] public Action<CreatureBrain> m_OnEquip;
        [NonSerialized] public Action<CreatureBrain> m_OnUnequip;
        [NonSerialized] public Action<CreatureBrain> m_OnUse;
        [NonSerialized] public Action<CreatureBrain> m_OnGet;
        [NonSerialized] public Action<CreatureBrain> m_OnDrop;
        [NonSerialized] public Action<ItemInstance> m_OnSpawn;

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
