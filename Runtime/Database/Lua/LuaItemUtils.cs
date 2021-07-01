#if UNITY_EDITOR
#endif

namespace Syadeu.Database.Lua
{
    internal sealed class LuaItemUtils
    {
        public static ValuePair NewValue(string name, object value) => ValuePair.New(name, value);

        public static ItemProxy GetItem(ulong hash)
        {
            Item item = ItemDataList.Instance.GetItem(hash);
            if (item == null)
            {
                $"item {hash} is null".ToLogConsole();
                return null;
            }
            else
            {
                return item.GetProxy();
            }
        }
        public static ItemTypeEntity GetItemType(ulong hash) => ItemDataList.Instance.GetItemType(hash);
        public static ItemEffectTypeProxy GetItemEffectType(ulong hash)
        {
            ItemEffectType type = ItemDataList.Instance.GetItemEffectType(hash);
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
        public static ItemUseableTypeProxy CreateItemUseableType(string name)
        {
            ItemUseableType type = new ItemUseableType(name);
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
