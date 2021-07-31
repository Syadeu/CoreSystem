using Newtonsoft.Json;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Actor
{
    /// <summary>
    /// 이 시스템은 기본 시스템 그룹에서 실행되지 않습니다.
    /// </summary>
    [SubSystem(typeof(EntitySystem))]
    public sealed class ActorSystem : PresentationSystemEntity<ActorSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;
    }

    public sealed class ActorEntity : EntityBase
    {

    }

    [AttributeAcceptOnly(typeof(ActorEntity))]
    public abstract class ActorAttributeBase : AttributeBase { }

    public sealed class ActorTypeAttribute : ActorAttributeBase
    {
        [JsonProperty(Order = 0, PropertyName = "ActorType")] public ActorType m_ActorType;
    }
}
