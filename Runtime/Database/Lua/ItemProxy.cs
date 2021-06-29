﻿using System;

#if UNITY_ADDRESSABLES
#endif

namespace Syadeu.Database
{
    internal sealed class ItemProxy : LuaProxyEntity<Item>
    {
        public ItemProxy(Item item) : base(item) { }

        public string Name => Target.m_Name;
        public string Guid => Target.m_Guid;

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

        #region Value
        public int GetValueCount() => Target.m_Values.Count;
        public bool HasValue(string name) => Target.m_Values.Contains(name);
        public object GetValue(string name) => Target.m_Values.GetValue(name);
        public void SetValue(string name, object value) => Target.m_Values.SetValue(name, value);
        public void AddValue(string name, object value) => Target.m_Values.Add(name, value);
        #endregion

        #region Instance
        public ItemInstance CreateInstance() => Target.CreateInstance();
        public ItemInstance GetInstance(string guid) => Target.GetInstance(System.Guid.Parse(guid));
        #endregion
    }
}
