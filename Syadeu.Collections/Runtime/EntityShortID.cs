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
    public readonly struct EntityShortID : IEmpty, IEquatable<EntityShortID>, IEquatable<InstanceID>
    {
        private readonly uint m_Hash;

        public uint Hash => m_Hash;

        public EntityShortID(InstanceID id)
        {
            ulong hash = id.Hash;
            m_Hash = unchecked((uint)hash * 397);
        }

        public bool Equals(EntityShortID other) => m_Hash.Equals(other.Hash);
        public bool Equals(InstanceID other) => m_Hash.Equals(new EntityShortID(other));

        public bool IsEmpty() => m_Hash == 0;
    }
}
