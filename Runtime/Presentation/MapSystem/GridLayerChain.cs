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
    public readonly struct GridLayerChain : IEmpty, IEquatable<GridLayerChain>
    {
        public static GridLayerChain Empty => new GridLayerChain(0);

        private readonly int m_Hash;

        public int Hash => m_Hash;

        private GridLayerChain(int hash)
        {
            m_Hash = hash;
        }
        internal GridLayerChain(GridLayerChain a0, GridLayer a1)
        {
            m_Hash = a0.m_Hash ^ a1.Hash;
        }
        internal GridLayerChain(GridLayerChain x, GridLayerChain y)
        {
            m_Hash = x.m_Hash ^ y.m_Hash;
        }
        internal GridLayerChain(GridLayer x, params GridLayer[] others)
        {
            m_Hash = x.Hash;
            for (int i = 0; i < others.Length; i++)
            {
                m_Hash ^= others[i].Hash;
            }
        }

        public bool IsEmpty() => m_Hash == 0;
        public bool Equals(GridLayerChain other) => m_Hash.Equals(other.m_Hash);
    }
}
