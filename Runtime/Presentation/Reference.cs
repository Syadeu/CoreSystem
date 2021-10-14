using Newtonsoft.Json;
using Newtonsoft.Json.Utilities;
using Syadeu.Collections;
using Syadeu.Presentation.Entities;
using System;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Syadeu.Presentation
{
    /// <summary>
    /// Editor Only Reference. In Runtime use <see cref="FixedReference"/>
    /// </summary>
    [Serializable]
    public struct Reference : IFixedReference, IEquatable<Reference>
    {
        public static readonly Reference Empty = new Reference(Hash.Empty);

        [JsonProperty(Order = 0, PropertyName = "Hash")] private Hash m_Hash;

        [JsonIgnore] public Hash Hash => m_Hash;

        [JsonConstructor]
        public Reference(Hash hash)
        {
            m_Hash = hash;
        }
        [Preserve]
        public Reference(ObjectBase obj)
        {
            if (obj == null) m_Hash = Hash.Empty;
            else m_Hash = obj.Hash;
        }
        public static Reference GetReference(string name) => new Reference(EntityDataList.Instance.GetObject(name));

        //public IObject GetObject()
        //{
        //    if (EntityDataList.Instance.m_Objects.TryGetValue(m_Hash, out ObjectBase value)) return value;
        //    return null;
        //}
        public bool IsEmpty() => Equals(Empty);
        public bool IsValid() => !m_Hash.Equals(Hash.Empty);

        public bool Equals(IFixedReference other) => m_Hash.Equals(other.Hash);
        public bool Equals(Reference other) => m_Hash.Equals(other.m_Hash);

        public static implicit operator ObjectBase(Reference a) => EntityDataList.Instance.m_Objects[a.m_Hash];
        public static implicit operator Hash(Reference a) => a.m_Hash;
    }
    /// <summary>
    /// Editor Only Reference. In Runtime use <see cref="FixedReference{T}"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public struct Reference<T> : IFixedReference<T>, IEquatable<Reference<T>>
        where T : class, IObject
    {
        public static readonly Reference<T> Empty = new Reference<T>(Hash.Empty);
        private static PresentationSystemID<EntitySystem> s_EntitySystem = PresentationSystemID<EntitySystem>.Null;

        [JsonProperty(Order = 0, PropertyName = "Hash")] private Hash m_Hash;

        [JsonIgnore] public Hash Hash => m_Hash;

        [JsonConstructor]
        public Reference(Hash hash)
        {
            m_Hash = hash;
        }
        public Reference(ObjectBase obj)
        {
            if (obj == null)
            {
                m_Hash = Hash.Empty;
                return;
            }
            CoreSystem.Logger.True(TypeHelper.TypeOf<T>.Type.IsAssignableFrom(obj.GetType()),
                $"Object reference type is not match\n" +
                $"{obj.GetType().Name} != {TypeHelper.TypeOf<T>.Type.Name}");

            m_Hash = obj.Hash;
        }
        public static Reference<T> GetReference(string name) => new Reference<T>(EntityDataList.Instance.GetObject(name));

        //IObject IReference.GetObject()
        //{
        //    if (EntityDataList.Instance.m_Objects.TryGetValue(m_Hash, out ObjectBase value)) return value;
        //    return null;
        //}
        //public T GetObject()
        //{
        //    if (EntityDataList.Instance.m_Objects.TryGetValue(m_Hash, out ObjectBase value) &&
        //        value is T t) return t;
        //    return null;
        //}

        public bool IsEmpty() => Equals(Empty);
        public bool IsValid() => !m_Hash.Equals(Hash.Empty) && EntityDataList.Instance.m_Objects.ContainsKey(m_Hash);

        public bool Equals(IFixedReference other) => m_Hash.Equals(other.Hash);
        //public bool Equals(IReference<T> other) => m_Hash.Equals(other.Hash);
        public bool Equals(Reference<T> other) => m_Hash.Equals(other.m_Hash);

        //public Instance<T> CreateInstance()
        //{
        //    if (IsEmpty() || !IsValid())
        //    {
        //        CoreSystem.Logger.LogError(Channel.Entity, "You cannot create instance of null reference.");
        //        return Instance<T>.Empty;
        //    }

        //    if (s_EntitySystem.IsNull())
        //    {
        //        s_EntitySystem = PresentationSystem<DefaultPresentationGroup, EntitySystem>.SystemID;
        //        if (s_EntitySystem.IsNull())
        //        {
        //            CoreSystem.Logger.LogError(Channel.Entity, "Unexpected error has been raised.");
        //            return Instance<T>.Empty;
        //        }
        //    }

        //    Type t = this.GetObject().GetType();
        //    if (TypeHelper.TypeOf<EntityBase>.Type.IsAssignableFrom(t))
        //    {
        //        var temp = s_EntitySystem.System.CreateEntity(in m_Hash, float3.zero);
        //        return new Instance<T>(temp.Idx);
        //    }
        //    else if (TypeHelper.TypeOf<EntityDataBase>.Type.IsAssignableFrom(t))
        //    {
        //        var temp = s_EntitySystem.System.CreateObject(m_Hash);
        //        return new Instance<T>(temp.Idx);
        //    }

        //    return s_EntitySystem.System.CreateInstance(this);
        //}

        public static implicit operator T(Reference<T> a) => a.GetObject();
        public static implicit operator Hash(Reference<T> a) => a.m_Hash;
        public static implicit operator Reference(Reference<T> a) => new Reference(a.m_Hash);
        public static implicit operator FixedReference<T>(Reference<T> a) => new FixedReference<T>(a.m_Hash);

        [Preserve]
        static void AOTCodeGeneration()
        {
            //AotHelper.EnsureType<ReferenceArray<Reference<T>>>();
            AotHelper.EnsureType<Reference<T>>();
            AotHelper.EnsureList<Reference<T>>();

            throw new InvalidOperationException();
        }
    }
}
