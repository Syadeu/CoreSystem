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
using Unity.Burst;

namespace Syadeu.Presentation
{
    public struct CoroutineHandler : IValidation
    {
        public static readonly CoroutineHandler Null = new CoroutineHandler(PresentationSystemID<CoroutineSystem>.Null, -1);

        private readonly PresentationSystemID<CoroutineSystem> m_System;

        private readonly int m_Idx;
        internal int m_Generation;
        internal UpdateLoop m_Loop;
        internal bool m_Activated;

        public int Index => m_Idx;
        public int Generation => m_Generation;

        internal CoroutineHandler(PresentationSystemID<CoroutineSystem> system, int index)
        {
            m_System = system;

            m_Idx = index;
            m_Generation = 0;
            m_Loop = 0;
            m_Activated = false;
        }

        public bool IsNull() => m_Idx < 0;

        [BurstDiscard]
        public bool IsValid()
        {
            if (m_Idx < 0
#if DEBUG_MODE
                || m_System.IsNull() 
                || !m_System.IsValid()
#endif
                ) return false;

            CoroutineSystem system = m_System.System;

            return system.IsValidHandler(this);
        }
        public void Stop()
        {
            m_System.System.StopCoroutine(this);
        }
    }
}
