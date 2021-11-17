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

namespace Syadeu.Presentation.Map
{
    public readonly struct GridLayer : IEmpty, IEquatable<GridLayer>
    {
        public static GridLayer Empty => new GridLayer(0, false);

        private readonly int m_Hash;
        private readonly bool m_Inverse;

        public int Hash => m_Hash;
        public bool Inverse => m_Inverse;

        internal GridLayer(int hash, bool inverse)
        {
            m_Hash = hash;
            m_Inverse = inverse;
        }

        public bool IsEmpty() => m_Hash == 0;
        public bool Equals(GridLayer other) => m_Hash.Equals(other.m_Hash);
    }
}
