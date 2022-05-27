// Copyright 2022 Seung Ha Kim
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
using System.Threading;
using Unity.Collections;

namespace Syadeu.Collections.Threading
{
    [BurstCompatible]
    public struct AtomicOperator
    {
        private volatile int m_Value;
#if DEBUG_MODE
        private ThreadInfo m_Owner;
#endif

        public void Enter(in int index)
        {
            int value = 1 << (index % 32);

            while (true)
            {
                int original = Interlocked.Exchange(ref m_Value, m_Value | value);

                if ((original & value) != value)
                {
#if DEBUG_MODE
                    m_Owner = ThreadInfo.CurrentThread;
#endif
                    break;
                }
            }
        }
        public void Enter()
        {
            while (true)
            {
                int original = Interlocked.Exchange(ref m_Value, 1);

                if (original == 0)
                {
#if DEBUG_MODE
                    m_Owner = ThreadInfo.CurrentThread;
#endif
                    break;
                }
            }
        }

        public void Exit(in int index)
        {
            int value = 1 << (index % 32);
#if DEBUG_MODE
            if (!m_Owner.Equals(ThreadInfo.CurrentThread))
            {
                throw new InvalidOperationException();
            }
            else if ((m_Value & value) != value)
            {
                throw new InvalidOperationException();
            }
#endif
            m_Value -= value;
        }
        public void Exit()
        {
#if DEBUG_MODE
            if (!m_Owner.Equals(ThreadInfo.CurrentThread))
            {
                throw new InvalidOperationException();
            }
#endif
            m_Value = 0;
        }
    }
}
