using System;

namespace Syadeu.Internal
{
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public sealed class ReflectionSealedViewAttribute : Attribute { }
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public sealed class ReflectionDescriptionAttribute : Attribute
    {
        public string m_Description;
        public ReflectionDescriptionAttribute(string description)
        {
            m_Description = description;
        }
    }
}
