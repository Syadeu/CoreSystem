﻿using Syadeu.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.LowLevel;

namespace Syadeu.Presentation
{
    public sealed class CoroutineSystem : PresentationSystemEntity<CoroutineSystem>
    {
        public struct PresentationUpdate { }
        public struct PresentationTransformUpdate { }

        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private readonly Queue<CoroutineJobPayload> m_IterationJobs = new Queue<CoroutineJobPayload>();
        private CoroutineJobPayload m_CurrentIterationJob = null;

        internal readonly List<CoroutineJob> m_CoroutineJobs = new List<CoroutineJob>();
        readonly List<CoroutineJobPayload> m_CoroutineIterators = new List<CoroutineJobPayload>();
        readonly List<int>
            m_UsedUpdateIndices = new List<int>(),
            m_UsedTransformIndices = new List<int>();
        readonly Queue<int> m_TerminatedCoroutineIndices = new Queue<int>();

        public event Action OnUpdate;
        public event Action OnTransformUpdate;

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            PlayerLoopSystem defaultLoop = PlayerLoop.GetCurrentPlayerLoop();
            for (int i = 0; i < defaultLoop.subSystemList.Length; i++)
            {
                //$"{i}: {defaultLoop.subSystemList[i].type?.FullName}".ToLog();

                if (defaultLoop.subSystemList[i].type.Equals(TypeHelper.TypeOf<UnityEngine.PlayerLoop.Update>.Type))
                {
                    //for (int a = 0; a < defaultLoop.subSystemList[i].subSystemList?.Length; a++)
                    //{
                    //    $"{i}_{a}: {defaultLoop.subSystemList[i].subSystemList[a].type.Name}".ToLog();
                    //}
                    
                    List<PlayerLoopSystem> list = defaultLoop.subSystemList[i].subSystemList.ToList();
                    PlayerLoopSystem loop = new PlayerLoopSystem
                    {
                        loopConditionFunction = defaultLoop.subSystemList[i].loopConditionFunction,
                        subSystemList = Array.Empty<PlayerLoopSystem>(),
                        type = TypeHelper.TypeOf<PresentationUpdate>.Type,
                        updateDelegate = PresenationUpdateHandler,
                        updateFunction = defaultLoop.subSystemList[i].updateFunction
                    };
                    list.Add(loop);
                    defaultLoop.subSystemList[i].subSystemList = list.ToArray();
                }
                else if (defaultLoop.subSystemList[i].type.Equals(TypeHelper.TypeOf<UnityEngine.PlayerLoop.PostLateUpdate>.Type))
                {
                    //for (int a = 0; a < defaultLoop.subSystemList[i].subSystemList?.Length; a++)
                    //{
                    //    $"{i}_{a}: {defaultLoop.subSystemList[i].subSystemList[a].type.Name}".ToLog();
                    //}

                    List<PlayerLoopSystem> list = defaultLoop.subSystemList[i].subSystemList.ToList();
                    PlayerLoopSystem loop = new PlayerLoopSystem
                    {
                        loopConditionFunction = defaultLoop.subSystemList[i].loopConditionFunction,
                        subSystemList = Array.Empty<PlayerLoopSystem>(),
                        type = TypeHelper.TypeOf<PresentationTransformUpdate>.Type,
                        updateDelegate = PresentationTransformUpdateHandler,
                        updateFunction = defaultLoop.subSystemList[i].updateFunction
                    };
                    list.Add(loop);
                    defaultLoop.subSystemList[i].subSystemList = list.ToArray();
                }
            }

            PlayerLoop.SetPlayerLoop(defaultLoop);

            return base.OnInitialize();
        }
        public override void OnDispose()
        {
            if (m_CurrentIterationJob != null)
            {
                m_CurrentIterationJob.Disposable.Dispose();
            }
            m_CurrentIterationJob = null;
            int m_iterjobCount = m_IterationJobs.Count;
            for (int i = 0; i < m_iterjobCount; i++)
            {
                m_IterationJobs.Dequeue().Disposable.Dispose();
            }

            for (int i = 0; i < m_CoroutineIterators.Count; i++)
            {
                if (m_CoroutineIterators[i] == null) continue;
                m_CoroutineIterators[i].Disposable.Dispose();
            }

            m_CoroutineJobs.Clear();
            m_CoroutineIterators.Clear();
            m_UsedUpdateIndices.Clear();
            m_TerminatedCoroutineIndices.Clear();

            PlayerLoopSystem defaultLoop = PlayerLoop.GetDefaultPlayerLoop();
            PlayerLoop.SetPlayerLoop(defaultLoop);

            base.OnDispose();
        }

        private void PresenationUpdateHandler()
        {
            OnUpdate?.Invoke();

            #region Sequence Iterator Jobs
            if (m_CurrentIterationJob != null)
            {
                if (m_CurrentIterationJob.Iter.Current == null)
                {
                    if (!m_CurrentIterationJob.Iter.MoveNext())
                    {
                        m_CurrentIterationJob.Disposable.Dispose();
                        m_CurrentIterationJob = null;
                    }
                }
                else
                {
                    if (m_CurrentIterationJob.Iter.Current is CustomYieldInstruction @yield && !yield.keepWaiting)
                    {
                        if (!m_CurrentIterationJob.Iter.MoveNext())
                        {
                            m_CurrentIterationJob.Disposable.Dispose();
                            m_CurrentIterationJob = null;
                        }
                    }
                    else if (m_CurrentIterationJob.Iter.Current is UnityEngine.AsyncOperation oper &&
                        oper.isDone)
                    {
                        if (!m_CurrentIterationJob.Iter.MoveNext())
                        {
                            m_CurrentIterationJob.Disposable.Dispose();
                            m_CurrentIterationJob = null;
                        }
                    }
                    else if (m_CurrentIterationJob.Iter.Current is ICustomYieldAwaiter yieldAwaiter &&
                        !yieldAwaiter.KeepWait)
                    {
                        if (!m_CurrentIterationJob.Iter.MoveNext())
                        {
                            m_CurrentIterationJob.Disposable.Dispose();
                            m_CurrentIterationJob = null;
                        }
                    }
                    else if (m_CurrentIterationJob.Iter.Current is YieldInstruction &&
                        !(m_CurrentIterationJob.Iter.Current is UnityEngine.AsyncOperation))
                    {
                        CoreSystem.Logger.LogError(Channel.Presentation,
                            $"해당 yield return 타입({m_CurrentIterationJob.Iter.Current.GetType().Name})은 지원하지 않습니다");

                        m_CurrentIterationJob.Disposable.Dispose();
                        m_CurrentIterationJob = null;
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

            #region Iterator Jobs

            for (int i = m_UsedUpdateIndices.Count - 1; i >= 0; i--)
            {
                int idx = m_UsedUpdateIndices[i];
                CoroutineJobPayload iter = m_CoroutineIterators[idx];
                if (iter.Iter.Current == null)
                {
                    if (!iter.Iter.MoveNext())
                    {
                        m_CoroutineIterators[idx].Disposable.Dispose();
                        m_CoroutineIterators[idx] = null;

                        m_TerminatedCoroutineIndices.Enqueue(idx);
                        m_UsedUpdateIndices.RemoveAt(i);
                        continue;
                    }
                }

                if (iter.Iter.Current is CustomYieldInstruction @yield && !yield.keepWaiting)
                {
                    if (!iter.Iter.MoveNext())
                    {
                        m_CoroutineIterators[idx].Disposable.Dispose();
                        m_CoroutineIterators[idx] = null;

                        m_TerminatedCoroutineIndices.Enqueue(idx);
                        m_UsedUpdateIndices.RemoveAt(i);
                        continue;
                    }
                }
                else if (iter.Iter.Current is UnityEngine.AsyncOperation oper && oper.isDone)
                {
                    if (!m_CurrentIterationJob.Iter.MoveNext())
                    {
                        m_CoroutineIterators[idx].Disposable.Dispose();
                        m_CoroutineIterators[idx] = null;

                        m_TerminatedCoroutineIndices.Enqueue(idx);
                        m_UsedUpdateIndices.RemoveAt(i);
                        continue;
                    }
                }
                else if (iter.Iter.Current is ICustomYieldAwaiter yieldAwaiter &&
                    !yieldAwaiter.KeepWait)
                {
                    if (!m_CurrentIterationJob.Iter.MoveNext())
                    {
                        m_CoroutineIterators[idx].Disposable.Dispose();
                        m_CoroutineIterators[idx] = null;

                        m_TerminatedCoroutineIndices.Enqueue(idx);
                        m_UsedUpdateIndices.RemoveAt(i);
                        continue;
                    }
                }
                else if (iter.Iter.Current is YieldInstruction &&
                    !(iter.Iter.Current is UnityEngine.AsyncOperation))
                {
                    CoreSystem.Logger.LogError(Channel.Presentation,
                        $"해당 yield return 타입({m_CurrentIterationJob.Iter.Current.GetType().Name})은 지원하지 않습니다");

                    m_CoroutineIterators[idx].Disposable.Dispose();
                    m_CoroutineIterators[idx] = null;

                    m_TerminatedCoroutineIndices.Enqueue(idx);
                    m_UsedUpdateIndices.RemoveAt(i);
                    continue;
                }
            }

            #endregion
        }
        private void PresentationTransformUpdateHandler()
        {
            OnTransformUpdate?.Invoke();

            #region Iterator Jobs

            for (int i = m_UsedTransformIndices.Count - 1; i >= 0; i--)
            {
                int idx = m_UsedTransformIndices[i];
                CoroutineJobPayload iter = m_CoroutineIterators[idx];
                if (iter.Iter.Current == null)
                {
                    if (!iter.Iter.MoveNext())
                    {
                        m_CoroutineIterators[idx].Disposable.Dispose();
                        m_CoroutineIterators[idx] = null;

                        m_TerminatedCoroutineIndices.Enqueue(idx);
                        m_UsedTransformIndices.RemoveAt(i);
                        continue;
                    }
                }

                if (iter.Iter.Current is CustomYieldInstruction @yield && !yield.keepWaiting)
                {
                    if (!iter.Iter.MoveNext())
                    {
                        m_CoroutineIterators[idx].Disposable.Dispose();
                        m_CoroutineIterators[idx] = null;

                        m_TerminatedCoroutineIndices.Enqueue(idx);
                        m_UsedTransformIndices.RemoveAt(i);
                        continue;
                    }
                }
                else if (iter.Iter.Current is UnityEngine.AsyncOperation oper && oper.isDone)
                {
                    if (!m_CurrentIterationJob.Iter.MoveNext())
                    {
                        m_CoroutineIterators[idx].Disposable.Dispose();
                        m_CoroutineIterators[idx] = null;

                        m_TerminatedCoroutineIndices.Enqueue(idx);
                        m_UsedTransformIndices.RemoveAt(i);
                        continue;
                    }
                }
                else if (iter.Iter.Current is ICustomYieldAwaiter yieldAwaiter &&
                    !yieldAwaiter.KeepWait)
                {
                    if (!m_CurrentIterationJob.Iter.MoveNext())
                    {
                        m_CoroutineIterators[idx].Disposable.Dispose();
                        m_CoroutineIterators[idx] = null;

                        m_TerminatedCoroutineIndices.Enqueue(idx);
                        m_UsedTransformIndices.RemoveAt(i);
                        continue;
                    }
                }
                else if (iter.Iter.Current is YieldInstruction &&
                    !(iter.Iter.Current is UnityEngine.AsyncOperation))
                {
                    CoreSystem.Logger.LogError(Channel.Presentation,
                        $"해당 yield return 타입({m_CurrentIterationJob.Iter.Current.GetType().Name})은 지원하지 않습니다");

                    m_CoroutineIterators[idx].Disposable.Dispose();
                    m_CoroutineIterators[idx] = null;

                    m_TerminatedCoroutineIndices.Enqueue(idx);
                    m_UsedTransformIndices.RemoveAt(i);
                    continue;
                }
            }

            #endregion
        }

        #endregion

        private class CoroutineJobPayload
        {
            public IEnumerator Iter;
            public IDisposable Disposable;
        }

        public void PostSequenceIterationJob<T>(T job) where T : ICoroutineJob
        {
            m_IterationJobs.Enqueue(new CoroutineJobPayload
            {
                Iter = job.Execute(),
                Disposable = job
            });
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
                m_CoroutineIterators.Add(new CoroutineJobPayload
                {
                    Iter = job.Execute(),
                    Disposable = job
                });
            }
            else
            {
                int index = m_TerminatedCoroutineIndices.Dequeue();
                coroutineJob = m_CoroutineJobs[index];
                coroutineJob.m_Generation++;
                m_CoroutineJobs[index] = coroutineJob;
                m_CoroutineIterators[index] = new CoroutineJobPayload
                {
                    Iter = job.Execute(),
                    Disposable = job
                };
            }

            coroutineJob.m_Loop = job.Loop;
            if (job.Loop == UpdateLoop.Transform)
            {
                m_UsedTransformIndices.Add(coroutineJob.Index);
            }
            else m_UsedUpdateIndices.Add(coroutineJob.Index);

            return coroutineJob;
        }
        public void StopCoroutineJob(CoroutineJob job)
        {
            CoreSystem.Logger.ThreadBlock(nameof(StopCoroutineJob), ThreadInfo.Unity);

            if (!job.IsValid()) return;
            else if (!job.Generation.Equals(m_CoroutineJobs[job.Index].Generation))
            {
                return;
            }
            else if (m_CoroutineIterators[job.Index] == null)
            {
                return;
            }

            m_CoroutineIterators[job.Index] = null;
            m_TerminatedCoroutineIndices.Enqueue(job.Index);

            if (job.m_Loop == UpdateLoop.Transform)
            {
                m_UsedTransformIndices.Remove(job.Index);
            }
            else m_UsedUpdateIndices.Remove(job.Index);
        }
    }
}
