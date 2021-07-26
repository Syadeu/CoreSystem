using Newtonsoft.Json;
using Syadeu.Database;
using System;
using System.Collections.Generic;

namespace Syadeu.Presentation
{
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
