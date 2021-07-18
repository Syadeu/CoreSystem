using Newtonsoft.Json;
using Syadeu.Database;
using System;
using System.Collections.Generic;

namespace Syadeu.Presentation
{
    /// <summary>
    /// <see cref="EntitySystem"/>을 통해 생성하는(혹은 생성된) 오보젝트입니다.
    /// </summary>
    /// <remarks>
    /// 이 인터페이스의 직접 상속은 허용하지않습니다.<br/>
    /// 오브젝트를 선언하고싶다면 <seealso cref="EntityBase"/>를 상속하세요.
    /// </remarks>
    public interface IEntity : IValidation
    {
        [JsonProperty(Order = -30, PropertyName = "Name")] string Name { get; }

        [JsonIgnore] DataGameObject gameObject { get; }
        [JsonIgnore] DataTransform transform { get; }

        [JsonIgnore] List<AttributeBase> Attributes { get; }

        AttributeBase GetAttribute(Type t);
        AttributeBase[] GetAttributes(Type t);
        T GetAttribute<T>() where T : AttributeBase;
        T[] GetAttributes<T>() where T : AttributeBase;
    }
}
