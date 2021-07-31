using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Presentation.Attributes;
using System;
using System.Collections.Generic;

namespace Syadeu.Presentation
{
    /// <summary>
    /// <see cref="EntitySystem"/>을 통해 생성(혹은 생성되지 않은 저장된 데이터)한 객체입니다.
    /// </summary>
    public interface IObject : IValidation
    {
        [JsonIgnore] Hash Idx { get; }
        [JsonProperty(Order = -30, PropertyName = "Name")] string Name { get; }
        [JsonIgnore] List<AttributeBase> Attributes { get; }

        [JsonIgnore] bool isCreated { get; }

        /// <summary>
        /// 이 엔티티가 가지고있는 해당 타입의 어트리뷰트를 가져옵니다.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        AttributeBase GetAttribute(Type t);
        /// <summary>
        /// 이 엔티티가 가지고있는 해당 타입의 어트리뷰트들을 가져옵니다.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        AttributeBase[] GetAttributes(Type t);
        /// <inheritdoc cref="GetAttribute(Type)"/>
        T GetAttribute<T>() where T : AttributeBase;
        /// <inheritdoc cref="GetAttributes(Type)"/>
        T[] GetAttributes<T>() where T : AttributeBase;
    }
}
