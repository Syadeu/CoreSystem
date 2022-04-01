// Copyright 2021 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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

        [UnityEngine.SerializeField]
        [JsonProperty(Order = 0, PropertyName = "Hash")] 
        private Hash m_Hash;

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
        public bool IsEmpty() => Equals(Empty);
        //public bool IsValid() => !m_Hash.Equals(Hash.Empty);

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

        [UnityEngine.SerializeField]
        [JsonProperty(Order = 0, PropertyName = "Hash")] 
        private Hash m_Hash;

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

        public bool IsEmpty() => Equals(Empty);
        //public bool IsValid() => !m_Hash.Equals(Hash.Empty) && EntityDataList.Instance.m_Objects.ContainsKey(m_Hash);

        public bool Equals(IFixedReference other) => m_Hash.Equals(other.Hash);
        public bool Equals(Reference<T> other) => m_Hash.Equals(other.m_Hash);

        public static implicit operator T(Reference<T> a) => a.GetObject();
        public static implicit operator Hash(Reference<T> a) => a.m_Hash;
        public static implicit operator Reference(Reference<T> a) => new Reference(a.m_Hash);
        public static implicit operator FixedReference<T>(Reference<T> a) => new FixedReference<T>(a.m_Hash);

        [Preserve]
        static void AOTCodeGeneration()
        {
            AotHelper.EnsureType<FixedReferenceList64<T>>();
            AotHelper.EnsureType<Reference<T>>();
            AotHelper.EnsureList<Reference<T>>();

            throw new InvalidOperationException();
        }
    }
}
