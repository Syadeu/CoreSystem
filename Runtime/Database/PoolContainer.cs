#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Internal;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Syadeu.Collections
{
    public static class PoolContainer<T> where T : class
    {
        private static bool m_Initialized = false;

        private static readonly ConcurrentQueue<T> m_List;
        private static Func<T> m_InstantiateFunc;
        private static int m_Count = 0;

        public static bool Initialized => m_Initialized;

        static PoolContainer()
        {
            m_List = new ConcurrentQueue<T>();
        }

        public static void Initialize(Func<T> instantiate, int initialCount/*, int maxCount*/)
        {
            m_InstantiateFunc = instantiate;
            for (int i = 0; i < initialCount; i++)
            {
                m_List.Enqueue(m_InstantiateFunc.Invoke());
            }
            m_Count = initialCount;
            //m_MaxCount = maxCount;

            m_Initialized = true;
        }
        public static void Initialize()
        {
            m_Initialized = true;
        }

        public static T Dequeue()
        {
            if (!m_Initialized)
            {
                CoreSystem.Logger.LogError(Channel.Data, $"Pool Container<{TypeHelper.TypeOf<T>.Type}> is not initialized");
                throw new Exception("Not Initialized");
            }

            T output;
            if (m_List.Count == 0)
            {
                if (m_InstantiateFunc != null)
                {
                    output = m_InstantiateFunc.Invoke();
                    m_Count++;
                }
                else return null;
            }
            else
            {
                if (!m_List.TryDequeue(out output))
                {
                    if (m_InstantiateFunc != null)
                    {
                        output = m_InstantiateFunc.Invoke();
                        m_Count++;
                    }
                    else return null;
                }
            }
            return output;
        }
        public static void Enqueue(T obj)
        {
            if (!m_Initialized)
            {
                CoreSystem.Logger.LogError(Channel.Data, $"Pool Container<{TypeHelper.TypeOf<T>.Type}> is not initialized");
                throw new Exception("Not Initialized");
            }
            m_List.Enqueue(obj);

            //if (m_ReleaseTimer != null && ValidateReleaseTrigger() && !m_ReleaseCalled)
            //{
            //    m_ReleaseCalled = true;
            //    m_ReleaseTimer.Start();
            //}
        }

        public static void Dispose()
        {
            int count = m_List.Count;
            for (int i = 0; i < count; i++)
            {
                if (!m_List.TryDequeue(out var temp))
                {
                    i--;
                    continue;
                }

                if (temp is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            m_InstantiateFunc = null;
            m_Initialized = false;
        }
    }
}
