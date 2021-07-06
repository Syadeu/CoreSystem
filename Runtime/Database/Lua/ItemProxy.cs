using System;

namespace Syadeu.Database.Lua
{
    internal sealed class ItemProxy : LuaProxyEntity<Item>
    {
        public ItemProxy(Item item) : base(item) { }

        public string Name => Target.m_Name;
        public string Guid => Target.m_Hash.ToString();

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
