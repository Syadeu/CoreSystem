using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Syadeu.Mono;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Syadeu.Database
{
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

        [SerializeReference] public ItemValue[] m_Values;

        [NonSerialized] private ItemProxy m_Proxy = null;

        [NonSerialized] private List<ItemInstance> m_Instances = new List<ItemInstance>();

        [NonSerialized] public Action m_OnEquip;
        [NonSerialized] public Action m_OnUnequip;
        [NonSerialized] public Action m_OnUse;
        [NonSerialized] public Action m_OnDrop;

        public Item()
        {
            m_Name = "NewItem";
            m_Guid = Guid.NewGuid().ToString();
        }

        public ItemProxy GetProxy()
        {
            if (m_Proxy == null)
            {
                m_Proxy = new ItemProxy(this);
            }
            return m_Proxy;
        }

        private int GetValueIdx(string name)
        {
            for (int i = 0; i < m_Values.Length; i++)
            {
                if (m_Values[i].m_Name.Equals(name))
                {
                    return i;
                }
            }
            throw new Exception();
        }
        public object GetValue(string name) => m_Values[GetValueIdx(name)].GetValue();
        public void SetValue(string name, object value)
        {
            int other = GetValueIdx(name);

            if (value == null)
            {
                m_Values[other] = new ItemValueNull()
                {
                    m_Name = name
                };
            }
            else if (value is bool boolVal)
            {
                SerializableItemBoolValue temp = new SerializableItemBoolValue
                {
                    m_Name = name,
                    m_Value = boolVal
                };
                m_Values[other] = temp;
            }
            else if (value is float floatVal)
            {
                SerializableItemFloatValue temp = new SerializableItemFloatValue
                {
                    m_Name = name,
                    m_Value = floatVal
                };
                m_Values[other] = temp;
            }
            else if (value is int intVal)
            {
                SerializableItemIntValue temp = new SerializableItemIntValue
                {
                    m_Name = name,
                    m_Value = intVal
                };
                m_Values[other] = temp;
            }
            else
            {
                SerializableItemStringValue temp = new SerializableItemStringValue
                {
                    m_Name = name,
                    m_Value = value.ToString()
                };
                m_Values[other] = temp;
            }
        }

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
                if (m_Instances[i].Guid.Equals(guid))
                {
                    return m_Instances[i];
                }
            }

            throw new Exception();
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
                        m_ItemTypes[i] = ItemDataList.Instance.GetItemType(Target.m_ItemTypes[i]).GetProxy();
                    }
                }
                
                return m_ItemTypes;
            }
        }

        public Action OnEquip { get => Target.m_OnEquip; set => Target.m_OnEquip = value; }
        public Action OnUse { get => Target.m_OnUse; set => Target.m_OnUse = value; }

        public object GetValue(string name) => Target.GetValue(name);
        public void SetValue(string name, object value) => Target.SetValue(name, value);
    }
    public sealed class ItemInstance
    {
        private readonly Guid m_Guid;

        private readonly Item m_Data;
        private readonly ItemType[] m_ItemTypes;
        private readonly ItemEffectType[] m_ItemEffectTypes;
        private readonly ItemValue[] m_Values;

        public Guid Guid => m_Guid;

        internal ItemInstance(Item item)
        {
            m_Guid = Guid.NewGuid();

            m_Data = item;
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
            m_Values = new ItemValue[item.m_Values.Length];
            for (int i = 0; i < m_Values.Length; i++)
            {
                m_Values[i] = (ItemValue)item.m_Values[i].Clone();
            }
        }
    }

    [Serializable]
    public sealed class ItemType
    {
        public string m_Name;
        public string m_Guid;

        [Space]
        [SerializeReference] public ItemValue[] m_Values;

        [NonSerialized] private ItemTypeProxy m_Proxy = null;

        public ItemType()
        {
            m_Name = "NewItemType";
            m_Guid = Guid.NewGuid().ToString();
        }

        public ItemTypeProxy GetProxy()
        {
            if (m_Proxy == null)
            {
                m_Proxy = new ItemTypeProxy(this);
            }
            return m_Proxy;
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

        [Space]
        [SerializeReference] public ItemValue[] m_Values;

        [NonSerialized] private ItemEffectTypeProxy m_Proxy = null;

        public ItemEffectType()
        {
            m_Name = "NewItemEffectType";
            m_Guid = Guid.NewGuid().ToString();
        }

        public ItemEffectTypeProxy GetProxy()
        {
            if (m_Proxy == null) m_Proxy = new ItemEffectTypeProxy(this);
            return m_Proxy;
        }
    }
    public sealed class ItemEffectTypeProxy : LuaProxyEntity<ItemEffectType>
    {
        public ItemEffectTypeProxy(ItemEffectType itemEffectType) : base(itemEffectType) { }
    }
}
