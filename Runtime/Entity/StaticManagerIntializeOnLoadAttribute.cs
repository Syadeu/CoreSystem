using System;

namespace Syadeu
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class StaticManagerIntializeOnLoadAttribute : Attribute
    {
    }

    public sealed class StaticManagerDescriptionAttribute : Attribute
    {
        public string m_Description;
        public StaticManagerDescriptionAttribute(string description)
        {
            m_Description = description;
        }
    }
}
