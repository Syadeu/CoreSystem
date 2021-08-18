using Newtonsoft.Json;
using Syadeu.Internal;
using Syadeu.Presentation.Entities;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Attributes
{
    [ReflectionDescription(
        "이 어트리뷰트가 달린 Entity가 생성될 때, 같이 생성될 Entity 를 설정할 수 있습니다."
        )]
    public sealed class CreateEntityAttribute : AttributeBase
    {
        [JsonProperty(Order = 0, PropertyName = "Entity")] public Reference<EntityBase> m_Entity;
        
        [JsonIgnore] public Entity<EntityBase> CreatedEntity { get; internal set; }
    }
    [Preserve]
    internal sealed class CreateEntityProcessor : AttributeProcessor<CreateEntityAttribute>
    {
        protected override void OnCreated(CreateEntityAttribute attribute, EntityData<IEntityData> entity)
        {
            ITransform tr = ((Entity<IEntity>)entity).Target.transform;
            attribute.CreatedEntity = CreateEntity(attribute.m_Entity, tr.position, tr.rotation);
        }
    }
}
