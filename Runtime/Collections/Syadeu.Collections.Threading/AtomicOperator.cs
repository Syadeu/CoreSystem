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
using System.Threading;

namespace Syadeu.Collections.Threading
{
    public struct AtomicOperator
    {
        private volatile int m_Value;
        private ThreadInfo m_Owner;

        public void Enter()
        {
            while (true)
            {
                int original = Interlocked.Exchange(ref m_Value, 1);

                if (original == 0)
                {
                    m_Owner = ThreadInfo.CurrentThread;
                    break;
                }
            }
        }
        public void Exit()
        {
            if (!m_Owner.Equals(ThreadInfo.CurrentThread))
            {
                throw new InvalidOperationException();
            }

            m_Value = 0;
        }
    }
}
