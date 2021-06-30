using System;
using System.Collections;
using System.Collections.Generic;

namespace Syadeu.Database
{
    public class PoolContainer<T> where T : class
    {
        private IList m_List;

        private Timer m_ReleaseTimer;
        private BackgroundJob m_ResetTimerJob;
        private int m_ReleaseTriggerCount;

        public PoolContainer()
        {
            OnInitialize();
        }

        public void ReleaseObjects()
        {
            for (int i = 0; i < m_List.Count; i++)
            {
                OnReleaseObject(i, (T)m_List[i]);
                if (m_List[i] is IDisposable disposable) disposable.Dispose();
            }

            m_List.Clear();
        }
        public T Dequeue()
        {
            T output;
            if (m_List.Count == 0)
            {
                output = null;
            }
            else
            {
                output = (T)m_List[m_List.Count - 1];
                m_ResetTimerJob.Start();
            }
            return output;
        }
        public void Enqueue(T obj)
        {
            m_List.Add(obj);

            if (m_ReleaseTimer != null && ValidateReleaseTrigger())
            {
                m_ReleaseTimer.Start();
            }
        }

        protected virtual void OnInitialize()
        {
            SetContainer(new List<T>());

            m_ReleaseTriggerCount = 10;
        }
        protected virtual void OnReleaseObject(int idx, T obj) { }

        protected void SetContainer(List<T> list) => m_List = list;
        protected void SetReleaseTime(int seconds)
        {
            if (m_ReleaseTimer == null)
            {
                m_ReleaseTimer = new Timer()
                    .SetTargetTime(300)
                    .OnTimerEnd(ReleaseObjects);
                m_ResetTimerJob = new BackgroundJob(ResetTimer);
            }

            m_ReleaseTimer.SetTargetTime(seconds);
        }

        private void ResetTimer()
        {
            if (m_ReleaseTimer != null)
            {
                m_ReleaseTimer.Kill();
                if (ValidateReleaseTrigger())
                {
                    while (m_ReleaseTimer.IsTimerActive())
                    {
                        CoreSystem.ThreadAwaiter(10);
                    }
                    m_ReleaseTimer.Start();
                }
            }
        }
        protected virtual bool ValidateReleaseTrigger()
        {
            if (m_List.Count >= m_ReleaseTriggerCount)
            {
                return true;
            }
            return false;
        }
    }

    public static class PoolStaticContainer<T> where T : class
    {
        private static IList m_List;

        private static Timer m_ReleaseTimer;
        private static BackgroundJob m_ResetTimerJob;
        private static int m_ReleaseTriggerCount;
        private static bool m_ReleaseCalled = false;

        public static event Action<int, T> OnReleaseObject;

        static PoolStaticContainer()
        {
            m_List = new List<T>();

            m_ReleaseTriggerCount = 10;
        }

        public static void ReleaseObjects()
        {
            for (int i = 0; i < m_List.Count; i++)
            {
                OnReleaseObject?.Invoke(i, (T)m_List[i]);
                if (m_List[i] is IDisposable disposable) disposable.Dispose();
            }

            m_List.Clear();
        }
        public static T Dequeue()
        {
            T output;
            if (m_List.Count == 0)
            {
                output = null;
            }
            else
            {
                output = (T)m_List[m_List.Count - 1];
                m_ResetTimerJob.Start();
            }
            return output;
        }
        public static void Enqueue(T obj)
        {
            m_List.Add(obj);

            if (m_ReleaseTimer != null && ValidateReleaseTrigger() && !m_ReleaseCalled)
            {
                m_ReleaseCalled = true;
                m_ReleaseTimer.Start();
            }
        }

        public static void SetReleaseTime(int seconds)
        {
            if (m_ReleaseTimer == null)
            {
                m_ReleaseTimer = new Timer()
                    .SetTargetTime(seconds)
                    .OnTimerEnd(ReleaseObjects);
                m_ResetTimerJob = new BackgroundJob(ResetTimer);
            }
            else m_ReleaseTimer.SetTargetTime(seconds);
        }

        private static void ResetTimer()
        {
            if (m_ReleaseTimer != null)
            {
                m_ReleaseTimer.Kill();
                if (ValidateReleaseTrigger() && !m_ReleaseCalled)
                {
                    m_ReleaseCalled = true;
                    while (m_ReleaseTimer.IsTimerActive())
                    {
                        CoreSystem.ThreadAwaiter(10);
                    }
                    m_ReleaseTimer.Start();
                }
                else m_ReleaseCalled = false;
            }
        }
        private static bool ValidateReleaseTrigger()
        {
            if (m_List.Count >= m_ReleaseTriggerCount)
            {
                return true;
            }
            return false;
        }
    }
}
