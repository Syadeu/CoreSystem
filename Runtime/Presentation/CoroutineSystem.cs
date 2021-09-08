using Syadeu.Internal;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Presentation
{
    public sealed class CoroutineSystem : PresentationSystemEntity<CoroutineSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => true;
        public override bool EnableAfterPresentation => false;

        private readonly Queue<IEnumerator> m_IterationJobs = new Queue<IEnumerator>();
        private IEnumerator m_CurrentIterationJob = null;

        readonly List<CoroutineJob> m_CoroutineJobs = new List<CoroutineJob>();
        readonly List<IEnumerator> m_CoroutineIterators = new List<IEnumerator>();
        readonly List<int> m_UsedCoroutineIndices = new List<int>();
        readonly Queue<int> m_TerminatedCoroutineIndices = new Queue<int>();

        protected override PresentationResult OnPresentation()
        {
            #region Sequence Iterator Jobs
            if (m_CurrentIterationJob != null)
            {
                if (m_CurrentIterationJob.Current == null)
                {
                    if (!m_CurrentIterationJob.MoveNext())
                    {
                        m_CurrentIterationJob = null;
                    }
                }
                else
                {
                    if (m_CurrentIterationJob.Current is CustomYieldInstruction @yield && !yield.keepWaiting)
                    {
                        if (!m_CurrentIterationJob.MoveNext())
                        {
                            m_CurrentIterationJob = null;
                        }
                    }
                    else if (m_CurrentIterationJob.Current is UnityEngine.AsyncOperation oper &&
                        oper.isDone)
                    {
                        if (!m_CurrentIterationJob.MoveNext())
                        {
                            m_CurrentIterationJob = null;
                        }
                    }
                    else if (m_CurrentIterationJob.Current is ICustomYieldAwaiter yieldAwaiter &&
                        !yieldAwaiter.KeepWait)
                    {
                        if (!m_CurrentIterationJob.MoveNext())
                        {
                            m_CurrentIterationJob = null;
                        }
                    }
                    else if (m_CurrentIterationJob.Current is YieldInstruction &&
                        !(m_CurrentIterationJob.Current is UnityEngine.AsyncOperation))
                    {
                        m_CurrentIterationJob = null;
                        CoreSystem.Logger.LogError(Channel.Presentation,
                            $"해당 yield return 타입({m_CurrentIterationJob.Current.GetType().Name})은 지원하지 않습니다");
                    }
                }
            }
            if (m_CurrentIterationJob == null)
            {
                if (m_IterationJobs.Count > 0)
                {
                    m_CurrentIterationJob = m_IterationJobs.Dequeue();
                }
            }

            #endregion

            for (int i = m_UsedCoroutineIndices.Count - 1; i >= 0; i--)
            {
                int idx = m_UsedCoroutineIndices[i];
                IEnumerator iter = m_CoroutineIterators[idx];
                if (iter.Current == null)
                {
                    if (!iter.MoveNext())
                    {
                        m_CoroutineIterators[idx] = null;
                        m_TerminatedCoroutineIndices.Enqueue(idx);
                        m_UsedCoroutineIndices.RemoveAt(i);
                        continue;
                    }
                }

                if (iter.Current is CustomYieldInstruction @yield && !yield.keepWaiting)
                {
                    if (!iter.MoveNext())
                    {
                        m_CoroutineIterators[idx] = null;
                        m_TerminatedCoroutineIndices.Enqueue(idx);
                        m_UsedCoroutineIndices.RemoveAt(i);
                        continue;
                    }
                }
                else if (iter.Current is UnityEngine.AsyncOperation oper && oper.isDone)
                {
                    if (!m_CurrentIterationJob.MoveNext())
                    {
                        m_CoroutineIterators[idx] = null;
                        m_TerminatedCoroutineIndices.Enqueue(idx);
                        m_UsedCoroutineIndices.RemoveAt(i);
                        continue;
                    }
                }
                else if (iter.Current is ICustomYieldAwaiter yieldAwaiter &&
                    !yieldAwaiter.KeepWait)
                {
                    if (!m_CurrentIterationJob.MoveNext())
                    {
                        m_CoroutineIterators[idx] = null;
                        m_TerminatedCoroutineIndices.Enqueue(idx);
                        m_UsedCoroutineIndices.RemoveAt(i);
                        continue;
                    }
                }
                else if (iter.Current is YieldInstruction &&
                    !(iter.Current is UnityEngine.AsyncOperation))
                {
                    m_CoroutineIterators[idx] = null;
                    m_TerminatedCoroutineIndices.Enqueue(idx);
                    m_UsedCoroutineIndices.RemoveAt(i);

                    CoreSystem.Logger.LogError(Channel.Presentation,
                        $"해당 yield return 타입({m_CurrentIterationJob.Current.GetType().Name})은 지원하지 않습니다");
                    continue;
                }
            }

            return base.OnPresentation();
        }

        public void PostSequenceIterationJob<T>(T job) where T : ICoroutineJob
        {
            m_IterationJobs.Enqueue(job.Execute());
        }

        public CoroutineJob PostCoroutineJob<T>(T job) where T : ICoroutineJob
        {
            CoreSystem.Logger.ThreadBlock(nameof(PostCoroutineJob), ThreadInfo.Unity);

            CoroutineJob coroutineJob;
            if (m_TerminatedCoroutineIndices.Count == 0)
            {
                coroutineJob = new CoroutineJob(SystemID, m_CoroutineJobs.Count)
                {
                    m_Generation = 0
                };
                m_CoroutineJobs.Add(coroutineJob);
                m_CoroutineIterators.Add(job.Execute());
            }
            else
            {
                int index = m_TerminatedCoroutineIndices.Dequeue();
                coroutineJob = m_CoroutineJobs[index];
                coroutineJob.m_Generation++;
                m_CoroutineJobs[index] = coroutineJob;
                m_CoroutineIterators[index] = job.Execute();
            }
            m_UsedCoroutineIndices.Add(coroutineJob.Index);
            return coroutineJob;
        }
        public void StopCoroutineJob(CoroutineJob job)
        {
            CoreSystem.Logger.ThreadBlock(nameof(StopCoroutineJob), ThreadInfo.Unity);

            if (!job.Generation.Equals(m_CoroutineJobs[job.Index].Generation))
            {
                return;
            }
            if (m_CoroutineIterators[job.Index] == null)
            {
                return;
            }

            m_CoroutineIterators[job.Index] = null;
            m_TerminatedCoroutineIndices.Enqueue(job.Index);
            m_UsedCoroutineIndices.Remove(job.Index);
        }
    }
}
