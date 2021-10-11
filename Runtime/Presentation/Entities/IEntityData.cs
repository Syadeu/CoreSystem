using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Presentation.Attributes;
using System;
using System.Collections.Generic;

namespace Syadeu.Presentation.Entities
{
    /// <summary>
    /// <see cref="EntitySystem"/>을 통해 생성(혹은 생성되지 않은 저장된 데이터)한 엔티티 구조의 제일 하단 객체입니다.
    /// </summary>
    /// <remarks>
    /// class 맴버 선언을 그리 추천하고 싶지 않지만, 필요에 의해 선언이 내부에 되었다면,<br/>
    /// 해당 값을 복사하여 인스턴스를 만들기 위해 <see cref="ObjectBase.Copy"/>을 override 하여 해당 값을 복사하여야합니다.
    /// <br/>
    /// 이 인터페이스의 직접 상속은 허용하지않습니다.<br/>
    /// 오브젝트를 선언하고싶다면 <seealso cref="Entities.EntityDataBase"/>를 상속하세요.
    /// </remarks>
    public interface IEntityData : IObject, IValidation
    {
        
        //[JsonIgnore] Hash Hash { get; }
        
        //[JsonIgnore] Hash Idx { get; }
        
        //[JsonProperty(Order = -30, PropertyName = "Name")] string Name { get; }
        /// <summary>
        /// 이 엔티티가 가지고 있는 <see cref="AttributeBase"/> 배열입니다.
        /// </summary>
        [JsonIgnore] AttributeBase[] Attributes { get; }
        /// <summary>
        /// 이 엔티티가 <see cref="EntitySystem"/>을 통해 생성된 객체인지 반환합니다.<br/>
        /// <see langword="false"/> 일 경우, raw 데이터를 의미합니다.
        /// </summary>
        [JsonIgnore] bool isCreated { get; }

        bool HasAttribute(Hash attributeHash);
        bool HasAttribute(Type attributeType);
        bool HasAttribute<T>() where T : AttributeBase;
        /// <summary>
        /// 이 엔티티가 가지고있는 해당 타입의 어트리뷰트를 가져옵니다.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        AttributeBase GetAttribute(Type t);
        /// <summary>
        /// 이 엔티티가 가지고있는 해당 타입 <paramref name="t"/>의 어트리뷰트들을 가져옵니다.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        AttributeBase[] GetAttributes(Type t);
        /// <summary>
        /// 이 엔티티가 가지고있는 해당 타입 <typeparamref name="T"/>의 어트리뷰트를 가져옵니다.
        /// </summary>
        /// <remarks>
        /// 만약 여러 개의 중복된 타입의 어트리뷰트를 가지고 있다면, 배열의 첫번째 어트리뷰트를 반환합니다.
        /// </remarks>
        T GetAttribute<T>() where T : AttributeBase;
        /// <summary>
        /// 이 엔티티가 가지고있는 해당 타입 <typeparamref name="T"/>의 어트리뷰트들을 가져옵니다.
        /// </summary>
        T[] GetAttributes<T>() where T : AttributeBase;
    }
}
