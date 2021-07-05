using System;

namespace Syadeu
{
    public sealed class StaticManagerDescriptionAttribute : Attribute
    {
#if UNITY_EDITOR
        public string m_Description;
#endif
        public StaticManagerDescriptionAttribute(string description)
        {
#if UNITY_EDITOR
            m_Description = description;
#endif
        }
    }
}
