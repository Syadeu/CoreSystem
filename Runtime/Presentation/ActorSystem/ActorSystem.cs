using Newtonsoft.Json;
using Syadeu.Internal;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Map;

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

    [ReflectionDescription("이 액터의 타입을 설정합니다.")]
    public sealed class ActorTypeAttribute : ActorAttributeBase
    {
        [JsonProperty(Order = 0, PropertyName = "ActorType")] public ActorType m_ActorType;
    }

    [ReflectionDescription("이 어트리뷰트 상단에 GridSizeAttribute가 있어야 동작합니다.")]
    public sealed class ActorGridAttribute : ActorAttributeBase
    {
        
    }
    //internal sealed class ActorGridProcessor : AttributeProcessor<ActorGridAttribute>
    //{
    //    protected override void OnCreated(ActorGridAttribute attribute, EntityData<IEntityData> entity)
    //    {
    //        GridSystem gridSystem = PresentationSystem<GridSystem>.System;
    //        if (gridSystem == null) throw new System.Exception("System null");
    //        if (gridSystem.GridMap == null) throw new System.Exception("Grid null");

    //        gridSystem.UpdateGridEntity(entity, attribute.GetCurrentGridCells());
    //    }
    //}
}
