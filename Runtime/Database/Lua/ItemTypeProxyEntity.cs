namespace Syadeu.Database.Lua
{
    internal abstract class ItemTypeProxyEntity<T> : LuaProxyEntity<T> where T : ItemTypeEntity
    {
        public ItemTypeProxyEntity(T itemType) : base(itemType) { }

        public string Name => Target.m_Name;
        public ulong Hash => Target.m_Hash;
    }
}
