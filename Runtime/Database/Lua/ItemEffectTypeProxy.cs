namespace Syadeu.Database.Lua
{
    [System.Obsolete("", true)]
    internal sealed class ItemEffectTypeProxy : LuaProxyEntity<ItemEffectType>
    {
        public string Name => Target.Name;
        public ulong Hash => Target.Hash;

        public ItemEffectTypeProxy(ItemEffectType itemEffectType) : base(itemEffectType) { }

        #region Value
        public int GetValueCount() => Target.m_Values.Count;
        public bool HasValue(string name) => Target.m_Values.Contains(name);
        public object GetValue(string name) => Target.m_Values.GetValue(name);
        public void SetValue(string name, object value) => Target.m_Values.SetValue(name, value);
        public void AddValue(string name, object value) => Target.m_Values.Add(name, value);
        #endregion
    }
}
