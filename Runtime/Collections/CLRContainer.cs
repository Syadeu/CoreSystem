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

using System;
using System.Collections.Concurrent;

namespace Syadeu.Collections
{
    [System.Obsolete("Use ObjectPool")]
    public sealed class CLRContainer<T>
    {
        private readonly ConcurrentStack<T> m_Stack;
        private readonly Func<T> m_Factory;

        public CLRContainer(Func<T> factory)
        {
            m_Stack = new ConcurrentStack<T>();
            m_Factory = factory;
        }

        public void Enqueue(T t)
        {
            m_Stack.Push(t);
        }
        public T Dequeue()
        {
            if (!m_Stack.TryPop(out T t))
            {
                t = m_Factory.Invoke();
            }
            return t;
        }
    }
}
