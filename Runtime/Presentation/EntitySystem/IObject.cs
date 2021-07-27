using Newtonsoft.Json;
using Syadeu.Database;
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

        AttributeBase GetAttribute(Type t);
        AttributeBase[] GetAttributes(Type t);
        T GetAttribute<T>() where T : AttributeBase;
        T[] GetAttributes<T>() where T : AttributeBase;
    }
}
