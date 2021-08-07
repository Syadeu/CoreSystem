using Syadeu.Presentation.Attributes;

namespace Syadeu.Presentation.Actor
{
    [AttributeAcceptOnly(typeof(ActorEntity))]
    public abstract class ActorAttributeBase : AttributeBase { }
}
