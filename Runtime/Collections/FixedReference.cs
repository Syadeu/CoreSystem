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

using Newtonsoft.Json.Utilities;
using System;
using UnityEngine.Scripting;

namespace Syadeu.Collections
{
    public struct FixedReference : IFixedReference, IEquatable<FixedReference>
    {
        private readonly Hash m_Hash;

        public Hash Hash => m_Hash;
        public FixedReference(Hash hash)
        {
            m_Hash = hash;
        }

        public bool IsEmpty() => m_Hash.Equals(Hash.Empty);
        //public bool IsValid() => !IsEmpty();

        public bool Equals(IFixedReference other) => m_Hash.Equals(other.Hash);
        public bool Equals(FixedReference other) => m_Hash.Equals(other.m_Hash);
    }
    public struct FixedReference<T> : IFixedReference<T>, IEquatable<FixedReference<T>>
        where T : class, IObject
    {
        public static FixedReference<T> Empty => new FixedReference<T>(Hash.Empty);

        private readonly Hash m_Hash;

        public Hash Hash => m_Hash;
        public FixedReference(Hash hash)
        {
            m_Hash = hash;
        }

        public bool IsEmpty() => m_Hash.Equals(Hash.Empty);
        //public bool IsValid() => !IsEmpty();

        public bool Equals(IFixedReference other) => m_Hash.Equals(other.Hash);
        public bool Equals(FixedReference<T> other) => m_Hash.Equals(other.m_Hash);

        public static implicit operator FixedReference(FixedReference<T> t) => new FixedReference(t.Hash);

        [Preserve]
        static void AOTCodeGeneration()
        {
            AotHelper.EnsureType<FixedReferenceList64<T>>();
            AotHelper.EnsureType<FixedReference<T>>();
            AotHelper.EnsureList<FixedReference<T>>();

            throw new InvalidOperationException();
        }
    }
}
