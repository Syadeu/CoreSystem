namespace Syadeu.Database.Lua
{
    [System.Obsolete("", true)]
    public sealed class ItemInstanceProxy : LuaProxyEntity<ItemInstance>
    {
        public ItemInstanceProxy(ItemInstance item) : base(item) { }

        public Item Data => Target.Data;
        public string Hash => Target.Hash.ToString();

        //public ItemTypeEntity[] ItemTypes => Target.ItemTypes;
        //public ItemEffectType[] EffectTypes => Target.EffectTypes;
        
        public object GetValue(string name) => Target.Values.GetValue(name);
        public void SetValueInt(string name, int value) => Target.Values.SetValue(name, value);
        public void SetValueDouble(string name, double value) => Target.Values.SetValue(name, value);
        public void SetValueBool(string name, bool value) => Target.Values.SetValue(name, value);
        public void SetValueString(string name, string value) => Target.Values.SetValue(name, value);

        //public bool HasItemType(string name) => ItemTypes.FindFor((other) => other.Name.Equals(name)) != null;
        //public ItemTypeEntity GetItemType(string name) => ItemTypes.FindFor((other) => other.Name.Equals(name));

        //public bool HasEffectType(string name) => EffectTypes.FindFor((other) => other.Name.Equals(name)) != null;
        //public ItemEffectType GetEffectType(string name) => EffectTypes.FindFor((other) => other.Name.Equals(name));
    }
}
