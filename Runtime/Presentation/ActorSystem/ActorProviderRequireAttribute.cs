using System;

namespace Syadeu.Presentation.Actor
{
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public sealed class ActorProviderRequireAttribute : Attribute
    {
        public Type[] m_RequireTypes;

        public ActorProviderRequireAttribute(params Type[] requireTypes)
        {
            m_RequireTypes = requireTypes;
        }
    }
}
