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

using System;

namespace Syadeu.Collections
{
    public readonly struct InstanceID : IValidation, IEquatable<InstanceID>, IEquatable<EntityID>, IEquatable<Hash>
    {
        public static readonly InstanceID Empty = new InstanceID(Hash.Empty);

        private readonly Hash m_Hash;

        public Hash Hash => m_Hash;

        private InstanceID(Hash idx)
        {
            m_Hash = idx;
        }

        public bool Equals(InstanceID other) => m_Hash.Equals(other.m_Hash);
        public bool Equals(EntityID other) => m_Hash.Equals(other.Hash);
        public bool Equals(Hash other) => m_Hash.Equals(other);

        public bool IsEmpty() => m_Hash.IsEmpty();
        public bool IsValid() => !m_Hash.IsEmpty();

        public override string ToString()
        {
            return m_Hash.ToString();
        }

        public static implicit operator InstanceID(Hash hash) => new InstanceID(hash);
        public static implicit operator InstanceID(EntityID hash) => new InstanceID(hash.Hash);
        //public static implicit operator Hash(InstanceID id) => id.m_Idx;
    }
}
