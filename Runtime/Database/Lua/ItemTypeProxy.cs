#if UNITY_ADDRESSABLES
#endif

namespace Syadeu.Database
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
}
