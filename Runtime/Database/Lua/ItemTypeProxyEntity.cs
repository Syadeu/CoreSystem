#if UNITY_ADDRESSABLES
#endif

namespace Syadeu.Database
{
    internal abstract class ItemTypeProxyEntity<T> : LuaProxyEntity<T> where T : ItemTypeEntity
    {
        public ItemTypeProxyEntity(T itemType) : base(itemType) { }

        public string Name => Target.m_Name;
        public string Guid => Target.m_Guid;
    }
}
