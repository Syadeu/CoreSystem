using System;
using UnityEngine;

#if UNITY_ADDRESSABLES
#endif

namespace Syadeu.Database
{
    [Serializable]
    public sealed class ItemEffectType
    {
        public string m_Name;
        public string m_Guid;

        [Space]
        public ValuePairContainer m_Values = new ValuePairContainer();

        [NonSerialized] private ItemEffectTypeProxy m_Proxy = null;

        public ItemEffectType()
        {
            m_Name = "NewItemEffectType";
            m_Guid = Guid.NewGuid().ToString();
        }
        public ItemEffectType(string name)
        {
            m_Name = name;
            m_Guid = Guid.NewGuid().ToString();
        }

        internal ItemEffectTypeProxy GetProxy()
        {
            if (m_Proxy == null) m_Proxy = new ItemEffectTypeProxy(this);
            return m_Proxy;
        }
    }
}
