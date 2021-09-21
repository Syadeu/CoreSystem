using Syadeu.Presentation.Attributes;
using Syadeu.Presentation;
using Syadeu.Presentation.Actor;

namespace Syadeu.Presentation.MotionMatching
{
    [AttributeAcceptOnly(typeof(ActorEntity))]
    public abstract class MxMAttributeBase : AttributeBase { }
}
