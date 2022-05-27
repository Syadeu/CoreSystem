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

using Syadeu.Collections.Buffer.LowLevel;
using System;

namespace Syadeu.Collections
{
    public struct KeyValue<TKey, TValue> : IEmpty, IEquatable<KeyValue<TKey, TValue>>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        public readonly TKey Key;
        public TValue Value;

        public KeyValue(TKey key, TValue value)
        {
            this.Key = key;
            this.Value = value;
        }

        public bool IsEmpty()
        {
            if (Key is IEmpty emptyAble)
            {
                return emptyAble.IsEmpty();
            }

            return this.Key.Equals(default(TKey));
        }
        public bool IsKeyEmptyOrEquals(in TKey key)
        {
            return IsEmpty() || this.Key.Equals(key);
        }
        public bool IsKeyEquals(in TKey key)
        {
            return this.Key.Equals(key);
        }

        public bool Equals(KeyValue<TKey, TValue> other)
        {
            if (!Key.Equals(other.Key)) return false;

            return UnsafeBufferUtility.BinaryComparer(ref Value, ref other.Value);
        }
    }
}
