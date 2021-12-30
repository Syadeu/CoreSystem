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
using System.Collections.Generic;

namespace Syadeu.Collections.Buffer
{
    public sealed class ObjectPool<T> : IDisposable
        where T : class
    {
        private Func<T> m_Factory;
        private Action<T>
            m_OnGet, m_OnReserve, m_OnRelease;

        private Stack<T> m_Pool;
        private int m_CheckSum;

        private ObjectPool() { }
        public ObjectPool(Func<T> factory, Action<T> onGet, Action<T> onReserve, Action<T> onRelease)
        {
            m_Factory = factory;
            m_OnGet = onGet;
            m_OnReserve = onReserve;
            m_OnRelease = onRelease;

            m_Pool = new Stack<T>();
            m_CheckSum = 0;
        }

        public T Get()
        {
            T t;
            if (m_Pool.Count > 0)
            {
                t = m_Pool.Pop();
            }
            else t = m_Factory.Invoke();

            m_OnGet?.Invoke(t);

            int hash = t.GetHashCode();
            m_CheckSum ^= hash;

            return t;
        }
        public void Reserve(T t)
        {
            m_OnReserve?.Invoke(t);

            int hash = t.GetHashCode();
            m_CheckSum ^= hash;

            m_Pool.Push(t);
        }

        public void Dispose()
        {
            if (m_CheckSum != 0)
            {
                throw new Exception();
            }

            if (m_OnRelease != null)
            {
                int poolCount = m_Pool.Count;
                for (int i = 0; i < poolCount; i++)
                {
                    T t = m_Pool.Pop();
                    m_OnRelease.Invoke(t);
                }
            }

            m_Factory = null;
            m_OnGet = null;
            m_OnReserve = null;
            m_OnRelease = null;

            m_Pool = null;
        }
    }
}
