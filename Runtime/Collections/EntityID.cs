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

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using System;

namespace Syadeu.Collections
{
    /// <summary>
    /// <see cref="EntityData{T}"/>, <see cref="Entity{T}"/> 의 인스턴스 ID
    /// </summary>
    public readonly struct EntityID : IValidation, IEmpty, IEquatable<EntityID>, IEquatable<InstanceID>, IEquatable<Hash>
    {
        public static readonly EntityID Empty = new EntityID(Hash.Empty);

        private readonly Hash m_Hash;

        public Hash Hash => m_Hash;

        private EntityID(Hash idx)
        {
            m_Hash = idx;
        }

        public bool Equals(EntityID other) => m_Hash.Equals(other.m_Hash);
        public bool Equals(InstanceID other) => m_Hash.Equals(other.Hash);
        public bool Equals(Hash other) => m_Hash.Equals(other);

        public bool IsEmpty() => m_Hash.IsEmpty();
        public bool IsValid() => !m_Hash.IsEmpty();

        public static implicit operator EntityID(Hash hash) => new EntityID(hash);
        public static implicit operator EntityID(InstanceID hash) => new EntityID(hash.Hash);
        public static implicit operator Hash(EntityID id) => id.m_Hash;
    }
}
