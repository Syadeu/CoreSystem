using Newtonsoft.Json;
using Syadeu.Mono;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace Syadeu.Database
{
    [Serializable]
    public sealed class ItemValue
    {
        public string m_Name;
        /// <summary>
        /// <see cref="ItemValueType"/>
        /// </summary>
        public int m_Type;
        public string m_Value;

        public object GetValue()
        {
            switch ((ItemValueType)m_Type)
            {
                case ItemValueType.String:
                    return m_Value;
                case ItemValueType.Boolean:
                    return Convert.ChangeType(m_Value, typeof(bool));
                case ItemValueType.Float:
                    return Convert.ChangeType(m_Value, typeof(float));
                case ItemValueType.Integer:
                    return Convert.ChangeType(m_Value, typeof(int));
                default:
                    return null;
            }
        }
    }
    internal enum ItemValueType
    {
        Null,

        String,
        Boolean,
        Float,
        Integer
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

        public ItemValue[] m_Values;

        [NonSerialized] private ItemProxy m_Proxy = null;

        [NonSerialized] public Action m_OnEquip;
        [NonSerialized] public Action m_OnUse;

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

        private ItemValue InternalGetValue(string name)
        {
            for (int i = 0; i < m_Values.Length; i++)
            {
                if (m_Values[i].m_Name.Equals(name))
                {
                    return m_Values[i];
                }
            }
            throw new Exception();
        }
        public object GetValue(string name) => InternalGetValue(name).GetValue();
        public void SetValue(string name, object value)
        {
            ItemValue other = InternalGetValue(name);

            if (value == null) other.m_Value = null;
            else other.m_Value = value.ToString();

            ItemDataList.SetValueType(other);
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

    [Serializable]
    public sealed class ItemType
    {
        public string m_Name;
        public string m_Guid;

        [Space]
        public ItemValue[] m_Values;

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
        public ItemValue[] m_Values;

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
