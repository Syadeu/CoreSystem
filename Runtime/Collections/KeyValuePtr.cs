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

using Syadeu.Collections.Buffer;
using System;

namespace Syadeu.Collections
{
    public struct KeyValuePtr<TKey, TValue> : IEmpty, IEquatable<KeyValuePtr<TKey, TValue>>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        public readonly TKey key;
        public NativeReference<TValue> value;

        public KeyValuePtr(TKey key, NativeReference<TValue> value)
        {
            this.key = key;
            this.value = value;
        }

        public bool IsEmpty()
        {
            if (key is IEmpty emptyAble)
            {
                return emptyAble.IsEmpty();
            }

            return this.key.Equals(default(TKey));
        }
        public bool IsKeyEmptyOrEquals(in TKey key)
        {
            return IsEmpty() || this.key.Equals(key);
        }

        public bool Equals(KeyValuePtr<TKey, TValue> other) => key.Equals(other.key) && value.Equals(other.value);
    }
}
