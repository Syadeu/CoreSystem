using Newtonsoft.Json;
using Syadeu.Mono;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace Syadeu.Database
{
    [StaticManagerIntializeOnLoad]
    public sealed class ItemDataManager : StaticDataManager<ItemDataManager>
    {
        private Dictionary<int, Item> m_Items = new Dictionary<int, Item>();

        public override void OnInitialize()
        {
            //$"{DataManager.DataPath}/Item".ToLog();
            //Resources.LoadAll<>
        }

        private static void SaveItem(string path, Item item)    
        {
            string data = JsonConvert.SerializeObject(item);
            File.WriteAllText(path + $"Item/{item.m_Name}.json", data);
        }
    }
    [Serializable]
    public sealed class ItemValue
    {
        public string m_Name;
        public string m_Value;
    }

    [Serializable]
    public sealed class Item
    {
        public string m_Name;
        public string m_Guid;

        [Tooltip("GUID")]
        /// <summary>
        /// <see cref="ItemType"/>
        /// </summary>
        public string[] m_ItemTypes;
        [Tooltip("GUID")]
        /// <summary>
        /// <see cref="ItemEffectType"/>
        /// </summary>
        public string[] m_ItemEffectTypes;

        public ItemValue[] m_ItemValues;

        private ItemProxy m_Proxy = null;
        public ItemProxy Proxy
        {
            get
            {
                if (m_Proxy == null)
                {
                    m_Proxy = new ItemProxy(this);
                }
                return m_Proxy;
            }
        }

        public Action m_OnEquip;
        public Action m_OnUse;

        public Item()
        {
            m_Name = "NewItem";
            m_Guid = Guid.NewGuid().ToString();
        }

        public string GetValue(string name)
        {
            for (int i = 0; i < m_ItemValues.Length; i++)
            {
                if (m_ItemValues[i].m_Name.Equals(name))
                {
                    return m_ItemValues[i].m_Value;
                }
            }
            return null;
        }
    }
    public sealed class ItemProxy : LuaProxyEntity<Item>
    {
        public ItemProxy(Item item) : base(item) { }

        public string Name => Target.m_Name;

        private ItemTypeProxy[] m_ItemTypes = null;
        public ItemTypeProxy[] ItemTypes
        {
            get
            {
                if (m_ItemTypes == null)
                {
                    m_ItemTypes = new ItemTypeProxy[Target.m_ItemTypes.Length];
                    for (int i = 0; i < m_ItemTypes.Length; i++)
                    {
                        m_ItemTypes[i] = ItemDataList.Instance.GetItemType(Target.m_ItemTypes[i]).Proxy;
                    }
                }
                
                return m_ItemTypes;
            }
        }

        public Action OnEquip { get => Target.m_OnEquip; set => Target.m_OnEquip = value; }
        public Action OnUse { get => Target.m_OnUse; set => Target.m_OnUse = value; }
    }
    internal sealed class CreatureBrainProxy : LuaProxyEntity<CreatureBrain>
    {
        public CreatureBrainProxy(CreatureBrain brain) : base(brain) { }

        public string Name => Target.name;

        public bool IsOnGrid => Target.IsOnGrid;
        public bool IsOnNavMesh => Target.IsOnNavMesh;
        public bool IsMoving => Target.IsMoving;
    }

    [Serializable]
    public sealed class ItemType
    {
        public string m_Name;
        public string m_Guid;

        private ItemTypeProxy m_Proxy = null;
        public ItemTypeProxy Proxy
        {
            get
            {
                if (m_Proxy == null)
                {
                    m_Proxy = new ItemTypeProxy(this);
                }
                return m_Proxy;
            }
        }

        public ItemType()
        {
            m_Name = "NewItemType";
            m_Guid = Guid.NewGuid().ToString();
        }
    }
    public sealed class ItemTypeProxy : LuaProxyEntity<ItemType>
    {
        public ItemTypeProxy(ItemType itemType) : base(itemType) { }
    }

    [Serializable]
    public sealed class ItemEffectType
    {
        public string m_Name;
        public string m_Guid;

        public Action<CreatureBrain> m_Effect;

        public ItemEffectType()
        {
            m_Name = "NewItemEffectType";
            m_Guid = Guid.NewGuid().ToString();
        }
    }
}
