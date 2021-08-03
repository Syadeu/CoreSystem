using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Database.Converters;
using Syadeu.Internal;

namespace Syadeu.Presentation
{
    [JsonConverter(typeof(ReferenceJsonConverter))]
    public interface IReference : IValidation
    {
        Hash Hash { get; }

        ObjectBase GetObject();
    }
    public interface IReference<T> : IReference where T : ObjectBase
    {
        new T GetObject();
    }
    public struct Reference : IReference
    {
        public static Reference Empty = new Reference(Hash.Empty);

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

        public ObjectBase GetObject() => EntityDataList.Instance.m_Objects[m_Hash];
        public bool IsValid() => !m_Hash.Equals(Hash.Empty);

        public static implicit operator ObjectBase(Reference a) => EntityDataList.Instance.m_Objects[a.m_Hash];
        public static implicit operator Hash(Reference a) => a.m_Hash;
    }
    public struct Reference<T> : IReference<T> where T : ObjectBase
    {
        public static Reference<T> Empty = new Reference<T>(Hash.Empty);

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

        ObjectBase IReference.GetObject() => EntityDataList.Instance.m_Objects[m_Hash];
        public T GetObject()
        {
            if (EntityDataList.Instance.m_Objects.TryGetValue(m_Hash, out ObjectBase value)) return (T)value;
            return null;
        }
        public bool IsValid() => !m_Hash.Equals(Hash.Empty) && EntityDataList.Instance.m_Objects.ContainsKey(m_Hash);

        public static implicit operator T(Reference<T> a) => a.GetObject();
        public static implicit operator Hash(Reference<T> a) => a.m_Hash;
        public static implicit operator Reference(Reference<T> a) => new Reference(a.m_Hash);
    }
}
