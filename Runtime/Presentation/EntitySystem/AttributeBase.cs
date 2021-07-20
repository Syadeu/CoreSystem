using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Internal;
using System;

namespace Syadeu.Presentation
{
    /// <inheritdoc cref="IAttribute"/>
    public abstract class AttributeBase : ObjectBase, IAttribute, ICloneable
    {
        [JsonIgnore] public IEntity Parent { get; internal set; }

        public virtual object Clone()
        {
            AttributeBase att = (AttributeBase)MemberwiseClone();
            att.Name = string.Copy(Name);

            return att;
        }

        public override string ToString() => Name;
    }

    public interface IReference
    {
        public Hash Hash { get; }
        public Type Type { get; }
        public bool IsEntity { get; }
        public bool IsAttribute { get; }
    }
    public interface IReference<T> : IReference
    {
    }
    public struct Reference<T> : IReference<T> where T : ObjectBase
    {
        [JsonProperty(Order = 0, PropertyName = "Hash")] public Hash m_Hash;

        [JsonIgnore] Hash IReference.Hash => m_Hash;
        [JsonIgnore] Type IReference.Type => TypeHelper.TypeOf<T>.Type;
        [JsonIgnore] bool IReference.IsEntity => TypeHelper.TypeOf<EntityBase>.Type.IsAssignableFrom(TypeHelper.TypeOf<T>.Type);
        [JsonIgnore] bool IReference.IsAttribute => TypeHelper.TypeOf<AttributeBase>.Type.IsAssignableFrom(TypeHelper.TypeOf<T>.Type);

        public Reference(ObjectBase obj)
        {
            CoreSystem.Logger.True(TypeHelper.TypeOf<T>.Type.IsAssignableFrom(obj.GetType()), 
                $"Object reference type is not match\n" +
                $"{obj.GetType().Name} != {TypeHelper.TypeOf<T>.Type.Name}");

            m_Hash = obj.Hash;
        }

        public static implicit operator ObjectBase(Reference<T> a) => EntityDataList.Instance.m_Objects[a.m_Hash];
        //public static implicit operator Reference<T>(Hash a) => new Reference<T>(EntityDataList.Instance.m_Objects[a]);
        //public static implicit operator Reference<T>(ObjectBase a) => new Reference<T>(a);
        public static implicit operator Hash(Reference<T> a) => a.m_Hash;
    }
}
