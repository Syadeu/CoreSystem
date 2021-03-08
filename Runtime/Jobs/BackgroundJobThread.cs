using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Syadeu
{
    internal class BackgroundJobThread
    {
        public ConcurrentQueue<BackgroundJob> m_Jobs;

        public Thread JobThread { get; }
        public int JobThreadIndex { get; }

        public BackgroundJobThread(int idx)
        {
            m_Jobs = new ConcurrentQueue<BackgroundJob>();
            JobThreadIndex = idx;

            JobThread = new Thread(Update)
            {
                IsBackground = true
            };
            JobThread.Start();
        }

        private void Update()
        {
            Thread.CurrentThread.CurrentCulture = global::System.Globalization.CultureInfo.InvariantCulture;

            while (true)
            {
                if (m_Jobs.Count == 0)
                {
                    StaticManagerEntity.ThreadAwaiter(10);
                }

                int count = m_Jobs.Count;
                for (int i = 0; i < count; i++)
                {
                    if (!m_Jobs.TryDequeue(out var job)) continue;

                    try
                    {
                        job.IsRunning = true;
                        job.Action.Invoke();
                    }
                    catch (UnityException mainthread)
                    {
                        job.Faild = true; job.IsRunning = false; job.m_IsDone = true;
                        //job.Result = $"{nameof(mainthread)}: {mainthread.Message}";

                        throw new CoreSystemException(CoreSystemExceptionFlag.Jobs, "유니티 API 가 사용되어 백그라운드에서 돌릴 수 없습니다", job.CalledFrom, mainthread);
                    }
                    catch (Exception ex)
                    {
                        job.Faild = true; job.IsRunning = false; job.m_IsDone = true; job.Exception = ex;
                        //job.Result = $"{nameof(ex)}: {ex.Message}";

                        throw new CoreSystemException(CoreSystemExceptionFlag.Jobs, "잡을 실행하는 도중 에러가 발생되었습니다", job.CalledFrom, ex);
                    }
                    finally
                    {
                        job.IsRunning = false;
                        job.m_IsDone = true;
                    }
                }

                StaticManagerEntity.ThreadAwaiter(10);
            }
        }
    }
}
