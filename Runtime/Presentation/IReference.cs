﻿using Newtonsoft.Json;
using Newtonsoft.Json.Utilities;
using Syadeu.Database;
using Syadeu.Database.Converters;
using Syadeu.Internal;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Converters;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Map;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace Syadeu.Presentation
{
    [JsonConverter(typeof(ReferenceJsonConverter)), RequireImplementors]
    public interface IReference : IValidation, IEquatable<IReference>
    {
        Hash Hash { get; }

        ObjectBase GetObject();
    }
    public interface IReference<T> : IReference, IEquatable<IReference<T>> where T : ObjectBase
    {
        new T GetObject();
    }
    public struct Reference : IReference, IEquatable<Reference>
    {
        public static Reference Empty = new Reference(Hash.Empty);

        [JsonProperty(Order = 0, PropertyName = "Hash")] public Hash m_Hash;

        [JsonIgnore] Hash IReference.Hash => m_Hash;

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

        public ObjectBase GetObject() => EntityDataList.Instance.m_Objects[m_Hash];
        public bool IsValid() => !m_Hash.Equals(Hash.Empty);

        public bool Equals(IReference other) => m_Hash.Equals(other.Hash);
        public bool Equals(Reference other) => m_Hash.Equals(other.m_Hash);

        public static implicit operator ObjectBase(Reference a) => EntityDataList.Instance.m_Objects[a.m_Hash];
        public static implicit operator Hash(Reference a) => a.m_Hash;
    }
    public struct Reference<T> : IReference<T>, IEquatable<Reference<T>> where T : ObjectBase
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

        ObjectBase IReference.GetObject() => EntityDataList.Instance.m_Objects[m_Hash];
        public T GetObject()
        {
            if (EntityDataList.Instance.m_Objects.TryGetValue(m_Hash, out ObjectBase value)) return (T)value;
            return null;
        }
        public bool IsValid() => !m_Hash.Equals(Hash.Empty) && EntityDataList.Instance.m_Objects.ContainsKey(m_Hash);

        public bool Equals(IReference other) => m_Hash.Equals(other.Hash);
        public bool Equals(IReference<T> other) => m_Hash.Equals(other.Hash);
        public bool Equals(Reference<T> other) => m_Hash.Equals(other.m_Hash);

        public static implicit operator T(Reference<T> a) => a.GetObject();
        public static implicit operator Hash(Reference<T> a) => a.m_Hash;
        public static implicit operator Reference(Reference<T> a) => new Reference(a.m_Hash);

        [Preserve]
        static void AOTCodeGeneration()
        {
            AotHelper.EnsureType<Reference<T>>();
            AotHelper.EnsureList<Reference<T>>();

            throw new InvalidOperationException();
        }
    }
}
