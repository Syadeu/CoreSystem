using System;

namespace Syadeu.Internal
{
    public sealed class ReflectionSealedViewAttribute : Attribute { }
    public sealed class ReflectionDescriptionAttribute : Attribute
    {
        public string m_Description;
        public ReflectionDescriptionAttribute(string description)
        {
            m_Description = description;
        }
    }
}
