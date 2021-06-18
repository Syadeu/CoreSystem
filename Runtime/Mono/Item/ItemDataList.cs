using UnityEngine;

namespace Syadeu.Database
{
    [PreferBinarySerialization][CustomStaticSetting("Syadeu/Item")]
    public sealed class ItemDataList : StaticSettingEntity<ItemDataList>
    {
        public override bool RuntimeModifiable => base.RuntimeModifiable;

        public Item[] m_Items;
        public ItemType[] m_ItemTypes;
        public ItemEffectType[] m_ItemEffectTypes;
    }
}
