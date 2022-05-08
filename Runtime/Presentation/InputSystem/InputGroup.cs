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

using System;
using Syadeu.Collections;

#if UNITY_EDITOR
#endif

namespace Syadeu.Presentation.Input
{
    public readonly struct InputGroup : IEquatable<InputGroup>
    {
        private readonly Hash m_Hash;

        public InputGroup(string key)
        {
            m_Hash = Hash.NewHash(key);
        }

        public bool Equals(InputGroup other) => m_Hash.Equals(other.m_Hash);

        public static implicit operator InputGroup(string t) => new InputGroup(t);
    }
}
