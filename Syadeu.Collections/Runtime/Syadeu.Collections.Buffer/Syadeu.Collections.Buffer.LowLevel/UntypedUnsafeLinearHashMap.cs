﻿// Copyright 2021 Seung Ha Kim
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
using Unity.Collections;

namespace Syadeu.Collections.Buffer.LowLevel
{
    [BurstCompatible]
    public struct UntypedUnsafeLinearHashMap
    {
        internal readonly int m_InitialCount;
        internal UnsafeAllocator m_Buffer;

        public bool IsCreated => m_Buffer.IsCreated;
        public long Size => m_Buffer.Size;

        internal UntypedUnsafeLinearHashMap(int count, UnsafeAllocator allocator)
        {
            m_InitialCount = count;
            m_Buffer = allocator;
        }
    }
}
