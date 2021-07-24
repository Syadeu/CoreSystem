using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Database.Converters;
using Syadeu.Internal;

namespace Syadeu.Presentation
{
    [JsonConverter(typeof(ReferenceJsonConverter))]
    public interface IReference : IValidation
    {
        public Hash Hash { get; }
    }
    public interface IReference<T> : IReference where T : ObjectBase { }
    public struct Reference : IReference
    {
        [JsonProperty(Order = 0, PropertyName = "Hash")] public Hash m_Hash;

        [JsonIgnore] Hash IReference.Hash => m_Hash;

        [JsonConstructor]
        public Reference(Hash hash)
        {
            m_Hash = hash;
        }
        public Reference(ObjectBase obj)
        {
            m_Hash = obj.Hash;
        }

        public bool IsValid() => !m_Hash.Equals(Hash.Empty);

        public static implicit operator ObjectBase(Reference a) => EntityDataList.Instance.m_Objects[a.m_Hash];
        public static implicit operator Hash(Reference a) => a.m_Hash;
    }
    public struct Reference<T> : IReference<T> where T : ObjectBase
    {
        [JsonProperty(Order = 0, PropertyName = "Hash")] public Hash m_Hash;

        [JsonIgnore] Hash IReference.Hash => m_Hash;

        [JsonConstructor]
        public Reference(Hash hash)
        {
            m_Hash = hash;
        }
        public Reference(ObjectBase obj)
        {
            CoreSystem.Logger.True(TypeHelper.TypeOf<T>.Type.IsAssignableFrom(obj.GetType()),
                $"Object reference type is not match\n" +
                $"{obj.GetType().Name} != {TypeHelper.TypeOf<T>.Type.Name}");

            m_Hash = obj.Hash;
        }

        public bool IsValid() => !m_Hash.Equals(Hash.Empty);

        public static implicit operator ObjectBase(Reference<T> a) => EntityDataList.Instance.m_Objects[a.m_Hash];
        public static implicit operator Hash(Reference<T> a) => a.m_Hash;
        public static implicit operator Reference(Reference<T> a) => new Reference(a.m_Hash);
    }
}
