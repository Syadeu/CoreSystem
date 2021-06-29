using System;

#if UNITY_ADDRESSABLES
#endif

namespace Syadeu.Database
{
    [Serializable]
    public abstract class ItemTypeEntity
    {
        public string m_Name;
        public string m_Guid;
    }
}
