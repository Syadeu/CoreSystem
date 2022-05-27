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

using Syadeu.Collections.Diagnostics;
using Syadeu.Collections.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Syadeu.Collections.Buffer
{
    /// <summary>
    /// 재사용을 위한 class 메모리 Pool 입니다.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Guid("03536d37-5801-4900-bc81-0ee1a5c7e296")]
    public sealed class ObjectPool<T> : IDisposable
    {
        public static ObjectPool<T> Shared { get; } = new ObjectPool<T>() { m_Factory = DefaultFactory, m_OnRelease = DefaultOnRelease };
        private static T DefaultFactory()
        {
            return Activator.CreateInstance<T>();
        }
        private static void DefaultOnRelease(T t)
        {
            if (t is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

#if DEBUG_MODE
        private readonly Dictionary<T, System.Diagnostics.StackFrame> m_DebugGets = new Dictionary<T, System.Diagnostics.StackFrame>();
#endif
        private Func<T> m_Factory;
        private Action<T>
            m_OnGet, m_OnReserve, m_OnRelease;

        private Stack<T> m_Pool = new Stack<T>();

        private ThreadInfo m_Owner;
        private int m_CheckSum;

        public bool EnableAtomicSafety { get; set; } = false;

        private ObjectPool() { }
        public ObjectPool(Func<T> factory, Action<T> onGet, Action<T> onReserve, Action<T> onRelease)
        {
            m_Factory = factory;
            m_OnGet = onGet;
            m_OnReserve = onReserve;
            m_OnRelease = onRelease;

            m_Pool = new Stack<T>();

            m_Owner = ThreadInfo.CurrentThread;
            m_CheckSum = 0;
        }
        public ObjectPool(bool enableAtomicSafety, Func<T> factory, Action<T> onGet, Action<T> onReserve, Action<T> onRelease)
        {
            m_Factory = factory;
            m_OnGet = onGet;
            m_OnReserve = onReserve;
            m_OnRelease = onRelease;

            m_Pool = new Stack<T>();

            m_Owner = ThreadInfo.CurrentThread;
            m_CheckSum = 0;

            EnableAtomicSafety = enableAtomicSafety;
        }

        public ObjectPool(ObjectPool<T> parent)
        {
            m_Factory = parent.m_Factory;
            m_OnGet = parent.m_OnGet;
            m_OnReserve = parent.m_OnReserve;
            m_OnRelease = parent.m_OnRelease;

            m_Pool = new Stack<T>();

            m_Owner = ThreadInfo.CurrentThread;
            m_CheckSum = 0;
        }

        public void AddObjects(int count)
        {
            while (0 < count--)
            {
                m_Pool.Push(m_Factory.Invoke());
            }
        }

        public T Get()
        {
#if DEBUG_MODE
            if (EnableAtomicSafety) m_Owner.Validate();
#endif
            T t;
            if (m_Pool.Count > 0)
            {
                t = m_Pool.Pop();
            }
            else
            {
                t = m_Factory.Invoke();
                if (t == null)
                {
#if DEBUG_MODE
                    CoreHelper.LogWarning(Channel.Collections,
                        $"Pool factory returned null object. Are you intended?");
#endif
                    return (T)TypeHelper.GetDefaultValue(TypeHelper.TypeOf<T>.Type);
                }
            }

            m_OnGet?.Invoke(t);

            int hash = t.GetHashCode();
            m_CheckSum ^= hash;

#if DEBUG_MODE
            m_DebugGets.Add(t, ScriptUtils.GetCallerFrame(1));
#endif
            return t;
        }
        public void Reserve(T t)
        {
#if DEBUG_MODE
            if (t == null)
            {
                "??".ToLogError();
                return;
            }
            if (EnableAtomicSafety) m_Owner.Validate();
#endif
            m_OnReserve?.Invoke(t);

            int hash = t.GetHashCode();
            m_CheckSum ^= hash;

            m_Pool.Push(t);
#if DEBUG_MODE
            m_DebugGets.Remove(t);
#endif
        }

        public void Dispose()
        {
#if DEBUG_MODE
            if (EnableAtomicSafety) m_Owner.Validate();
#endif
            if (m_CheckSum != 0)
            {
                $"Pool is not fully reserved.".ToLogError();
#if DEBUG_MODE
                foreach (var item in m_DebugGets)
                {
                    $"From: {item.Value}".ToLogError(LogChannel.Collections);
                }
#endif
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
