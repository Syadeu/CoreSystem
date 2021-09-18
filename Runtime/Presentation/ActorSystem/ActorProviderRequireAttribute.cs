using System;

namespace Syadeu.Presentation.Actor
{
    public sealed class ActorProviderRequireAttribute : Attribute
    {
        public Type[] m_RequireTypes;

        public ActorProviderRequireAttribute(params Type[] requireTypes)
        {
            m_RequireTypes = requireTypes;
        }
    }
}
