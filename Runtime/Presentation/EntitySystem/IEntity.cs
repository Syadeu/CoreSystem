using Newtonsoft.Json;
using Syadeu.Database;
using System;
using System.Collections.Generic;

namespace Syadeu.Presentation
{
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
