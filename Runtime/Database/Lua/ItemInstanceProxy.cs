#if UNITY_EDITOR
#endif

namespace Syadeu.Database.Lua
{
    public sealed class ItemInstanceProxy : LuaProxyEntity<ItemInstance>
    {
        public ItemInstanceProxy(ItemInstance item) : base(item) { }

        public Item Data => Target.Data;
        public string Hash => Target.Hash.ToString();

        public ItemTypeEntity[] ItemTypes => Target.ItemTypes;
        public ItemEffectType[] EffectTypes => Target.EffectTypes;
        
        public object GetValue(string name) => Target.Values.GetValue(name);
        public void SetValueInt(string name, int value) => Target.Values.SetValue(name, value);
        public void SetValueDouble(string name, double value) => Target.Values.SetValue(name, value);
        public void SetValueBool(string name, bool value) => Target.Values.SetValue(name, value);
        public void SetValueString(string name, string value) => Target.Values.SetValue(name, value);
    }
}
