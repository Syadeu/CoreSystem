using Newtonsoft.Json;
using Syadeu.Presentation.Proxy;
using Unity.Mathematics;

namespace Syadeu.Presentation.Entities
{
    /// <summary>
    /// <see cref="EntitySystem"/>을 통해 생성하는(혹은 생성된) <see cref="ProxyTransform"/>이 있는 엔티티입니다.
    /// </summary>
    /// <remarks>
    /// class 맴버 선언을 그리 추천하고 싶지 않지만, 필요에 의해 선언이 내부에 되었다면,<br/>
    /// 해당 값을 복사하여 인스턴스를 만들기 위해 <see cref="ObjectBase.Copy"/>을 override 하여 해당 값을 복사하여야합니다.<br/>
    /// <br/>
    /// 이 인터페이스의 직접 상속은 허용하지않습니다.<br/>
    /// 오브젝트를 선언하고싶다면 <seealso cref="EntityBase"/>를 상속하세요.
    /// </remarks>
    public interface IEntity : IEntityData
    {
        //[JsonIgnore] DataGameObject gameObject { get; }
        [JsonIgnore] ITransform transform { get; }

        /// <summary>
        /// 이 엔티티의 프리팹 <see cref="Prefab"/>의 AABB Center translation 값입니다.
        /// </summary>
        [JsonProperty(Order = -8, PropertyName = "Center")] float3 Center { get; }
        /// <summary>
        /// 이 엔티티 프리팹 <see cref="Prefab"/>의 AABB Size 값입니다.
        /// </summary>
        [JsonProperty(Order = -7, PropertyName = "Size")] public float3 Size { get; }
    }
}
