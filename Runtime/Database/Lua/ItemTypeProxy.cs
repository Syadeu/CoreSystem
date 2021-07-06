namespace Syadeu.Database.Lua
{
    internal sealed class ItemTypeProxy : ItemTypeProxyEntity<ItemType>
    {
        public ItemTypeProxy(ItemType itemType) : base(itemType) { }

        #region Value
        public int GetValueCount() => Target.m_Values.Count;
        public bool HasValue(string name) => Target.m_Values.Contains(name);
        public object GetValue(string name) => Target.m_Values.GetValue(name);
        public void SetValue(string name, object value) => Target.m_Values.SetValue(name, value);
        public void AddValue(string name, object value) => Target.m_Values.Add(name, value);
        #endregion
    }
    internal sealed class ItemUseableTypeProxy : ItemTypeProxyEntity<ItemUseableType>
    {
        public bool RemoveOnUse { get => Target.m_RemoveOnUse; set => Target.m_RemoveOnUse = value; }

        public ItemUseableTypeProxy(ItemUseableType itemType) : base(itemType) { }

        #region Value
        public int GetValueCount() => Target.m_OnUse.Count;
        public bool HasValue(string name) => Target.m_OnUse.Contains(name);
        public object GetValue(string name) => Target.m_OnUse.GetValue(name);
        public void SetValue(string name, object value) => Target.m_OnUse.SetValue(name, value);
        public void AddValue(string name, object value) => Target.m_OnUse.Add(name, value);
        #endregion
    }
}
