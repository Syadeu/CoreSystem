using Syadeu.Internal;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Syadeu.Database
{
    public static class PoolContainer<T> where T : class
    {
        private static bool m_Initialized = false;

        private static readonly ConcurrentQueue<T> m_List;
        private static Func<T> m_InstantiateFunc;
        //private static int m_MaxCount;
        private static int m_Count = 0;

        //private static Timer m_ReleaseTimer;
        //private static BackgroundJob m_ResetTimerJob;
        //private static int m_ReleaseTriggerCount;
        //private static bool m_ReleaseCalled = false;

        //public static event Action<int, T> OnReleaseObject;

        public static bool Initialized => m_Initialized;

        static PoolContainer()
        {
            m_List = new ConcurrentQueue<T>();

            //m_MaxCount = -1;
            //m_ReleaseTriggerCount = 10;
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

        //public static void SetReleaseTime(int seconds)
        //{
        //    if (!m_Initialized) throw new Exception("Not Initialized");

        //    if (m_ReleaseTimer == null)
        //    {
        //        m_ReleaseTimer = new Timer()
        //            .SetTargetTime(seconds)
        //            .OnTimerEnd(ReleaseObjects);
        //        m_ResetTimerJob = new BackgroundJob(ResetTimer);
        //    }
        //    else m_ReleaseTimer.SetTargetTime(seconds);
        //}
        //public static void ReleaseObjects()
        //{
        //    if (!m_Initialized) throw new Exception("Not Initialized");

        //    for (int i = 0; i < m_List.Count; i++)
        //    {
        //        OnReleaseObject?.Invoke(i, (T)m_List[i]);
        //        if (m_List[i] is IDisposable disposable) disposable.Dispose();
        //    }

        //    m_Count -= m_List.Count;
        //    m_List.Clear();
        //}

        //private static void ResetTimer()
        //{
        //    if (m_ReleaseTimer != null)
        //    {
        //        m_ReleaseTimer.Kill();
        //        if (ValidateReleaseTrigger() && !m_ReleaseCalled)
        //        {
        //            m_ReleaseCalled = true;
        //            while (m_ReleaseTimer.IsTimerActive())
        //            {
        //                CoreSystem.ThreadAwaiter(10);
        //            }
        //            m_ReleaseTimer.Start();
        //        }
        //        else m_ReleaseCalled = false;
        //    }
        //}
        //private static bool ValidateReleaseTrigger()
        //{
        //    if (m_List.Count >= m_ReleaseTriggerCount)
        //    {
        //        return true;
        //    }
        //    return false;
        //}
    }
}
