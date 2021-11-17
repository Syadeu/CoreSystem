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

using Syadeu.Collections;
using Syadeu.Internal;
using System;
using UnityEngine;

namespace Syadeu.Presentation.Proxy
{
    /// <inheritdoc cref="IComponentID"/>
    public readonly struct ComponentID : IComponentID
    {
        private readonly ulong m_Hash;

        ulong IComponentID.Hash => m_Hash;
        internal ComponentID(ulong hash) { m_Hash = hash; }
        public static IComponentID GetID(Type t) => new ComponentID(Hash.NewHash(t.Name));
        bool IEquatable<IComponentID>.Equals(IComponentID other) => m_Hash.Equals(other.Hash);
        public override string ToString() => m_Hash.ToString();
    }
    /// <inheritdoc cref="IComponentID"/>
    public readonly struct ComponentID<T> where T : Component
    {
        private static readonly ulong s_Hash = Hash.NewHash(TypeHelper.TypeOf<T>.Name);
        public static readonly IComponentID ID = new ComponentID(s_Hash);
    }
}
