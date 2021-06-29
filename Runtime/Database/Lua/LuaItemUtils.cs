#if UNITY_EDITOR
#endif

namespace Syadeu.Database.Lua
{
    internal sealed class LuaItemUtils
    {
        public static ValuePair NewValue(string name, object value) => ValuePair.New(name, value);

        public static ItemProxy GetItem(string guid)
        {
            Item item = ItemDataList.Instance.GetItem(guid);
            if (item == null)
            {
                $"item {guid} is null".ToLogConsole();
                return null;
            }
            else
            {
                return item.GetProxy();
            }
        }
        public static ItemTypeEntity GetItemType(string guid) => ItemDataList.Instance.GetItemType(guid);
        public static ItemEffectTypeProxy GetItemEffectType(string guid)
        {
            ItemEffectType type = ItemDataList.Instance.GetItemEffectType(guid);
            if (type == null) return null;
            else
            {
                return type.GetProxy();
            }
        }

        public static ItemProxy CreateItem(string name)
        {
            Item item = new Item(name);
            ItemDataList.Instance.m_Items.Add(item);
            return item.GetProxy();
        }
        public static ItemTypeProxy CreateItemType(string name)
        {
            ItemType type = new ItemType(name);
            ItemDataList.Instance.m_ItemTypes.Add(type);
            return type.GetProxy();
        }
        public static ItemEffectTypeProxy CreateItemEffectType(string name)
        {
            ItemEffectType effectType = new ItemEffectType(name);
            ItemDataList.Instance.m_ItemEffectTypes.Add(effectType);
            return effectType.GetProxy();
        }
    }
}
