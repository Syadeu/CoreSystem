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
using Syadeu.Collections.Buffer.LowLevel;
using Unity.Burst;

namespace Syadeu.Presentation
{
    public struct CoroutineHandler : IEmpty, IValidation
    {
        public static readonly CoroutineHandler Null = new CoroutineHandler();

        private readonly UnsafeReference<UnsafeCoroutineHandler> m_Pointer;
        private readonly int m_Generation;

        public bool Running => m_Pointer.Value.m_Activated;

        internal CoroutineHandler(UnsafeReference<UnsafeCoroutineHandler> p)
        {
            m_Pointer = p;
            m_Generation = p.Value.m_Generation;
        }

        public bool IsEmpty() => !m_Pointer.IsCreated;

        [BurstDiscard]
        public bool IsValid()
        {
            if (IsEmpty() || m_Generation != m_Pointer.Value.m_Generation) return false;

            return Running;
        }
        public void Stop()
        {
            if (!IsValid())
            {
                return;
            }

            m_Pointer.Value.m_Activated = false;
        }
    }
    internal unsafe struct UnsafeCoroutineHandler
    {
        public readonly int m_Idx;
        public int m_Generation;
        public UpdateLoop m_Loop;
        public bool m_Activated;

        public UnsafeCoroutineHandler(int index)
        {
            this = default(UnsafeCoroutineHandler);

            m_Idx = index;
        }
    }
}
