using System;
using UnityEngine;

#if UNITY_ADDRESSABLES
#endif

namespace Syadeu.Database
{
    [Serializable]
    public sealed class ItemType : ItemTypeEntity
    {
        [Space]
        public ValuePairContainer m_Values = new ValuePairContainer();

        [NonSerialized] private ItemTypeProxy m_Proxy = null;

        public ItemType()
        {
            m_Name = "NewItemType";
            m_Guid = Guid.NewGuid().ToString();
        }
        public ItemType(string name)
        {
            m_Name = name;
            m_Guid = Guid.NewGuid().ToString();
        }
        internal ItemTypeProxy GetProxy()
        {
            if (m_Proxy == null)
            {
                m_Proxy = new ItemTypeProxy(this);
            }
            return m_Proxy;
        }
    }
}
