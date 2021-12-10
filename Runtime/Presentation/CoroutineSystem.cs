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

using Syadeu.Collections;
using Syadeu.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.LowLevel;

namespace Syadeu.Presentation
{
    public sealed class CoroutineSystem : PresentationSystemEntity<CoroutineSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => true;
        public override bool EnableAfterPresentation => false;

        private NativeList<CoroutineHandler> m_CoroutineHandlers;
        private List<CoroutineJobPayload> m_Coroutines;

        private NativeQueue<int> m_ReservedIndices;

        private readonly List<int>
            m_UpdateIndices = new List<int>(),
            m_TransformIndices = new List<int>(),
            m_AfterTransformIndices = new List<int>();

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            m_CoroutineHandlers = new NativeList<CoroutineHandler>(128, AllocatorManager.Persistent);
            m_Coroutines = new List<CoroutineJobPayload>();

            m_ReservedIndices = new NativeQueue<int>(AllocatorManager.Persistent);

#if UNITY_EDITOR
            PresentationManager.Instance.TransformUpdate -= PresentationTransformUpdateHandler;
            PresentationManager.Instance.AfterTransformUpdate -= PresentationAfterTransformUpdateHandler;
#endif
            PresentationManager.Instance.TransformUpdate += PresentationTransformUpdateHandler;
            PresentationManager.Instance.AfterTransformUpdate += PresentationAfterTransformUpdateHandler;

            return base.OnInitialize();
        }
        public override void OnDispose()
        {
            //if (m_CurrentIterationJob != null)
            //{
            //    m_CurrentIterationJob.Disposable.Dispose();
            //}
            //m_CurrentIterationJob = null;
            //int m_iterjobCount = m_IterationJobs.Count;
            //for (int i = 0; i < m_iterjobCount; i++)
            //{
            //    m_IterationJobs.Dequeue().Disposable.Dispose();
            //}

            //for (int i = 0; i < m_CoroutineIterators.Count; i++)
            //{
            //    if (m_CoroutineIterators[i] == null) continue;

            //    else if (!CoreSystem.BlockCreateInstance)
            //    {
            //        m_CoroutineIterators[i].Disposable.Dispose();
            //    }
            //}

            //m_CoroutineJobs.Clear();
            //m_CoroutineIterators.Clear();
            //m_UsedUpdateIndices.Clear();
            //m_TerminatedCoroutineIndices.Clear();

            for (int i = 0; i < m_Coroutines.Count; i++)
            {
                m_Coroutines[i].Dispose();
            }

            m_Coroutines.Clear();

            //PresentationManager.Instance.Update -= PresenationUpdateHandler;
            PresentationManager.Instance.TransformUpdate -= PresentationTransformUpdateHandler;
            PresentationManager.Instance.AfterTransformUpdate -= PresentationAfterTransformUpdateHandler;

            m_CoroutineHandlers.Dispose();

            m_ReservedIndices.Dispose();
        }

        private void CoroutineUpdate(List<int> updateIndices)
        {
            for (int i = updateIndices.Count - 1; i >= 0; i--)
            {
                int idx = updateIndices[i];
                ref CoroutineHandler handler = ref m_CoroutineHandlers.ElementAt(idx);
                if (!handler.m_Activated)
                {
                    m_Coroutines[idx].Reset();

                    m_ReservedIndices.Enqueue(idx);
                    updateIndices.RemoveAt(i);
                    continue;
                }

                CoroutineJobPayload iter = m_Coroutines[idx];

                if (iter.Iter.Current == null)
                {
                    if (!iter.Iter.MoveNext())
                    {
                        handler.m_Activated = false;

                        m_Coroutines[idx].Disposable.Dispose();
                        m_Coroutines[idx].Reset();

                        m_ReservedIndices.Enqueue(idx);
                        updateIndices.RemoveAt(i);
                        continue;
                    }
                }
                else if (iter.Iter.Current is CustomYieldInstruction @yield && !yield.keepWaiting)
                {
                    if (!iter.Iter.MoveNext())
                    {
                        handler.m_Activated = false;

                        m_Coroutines[idx].Disposable.Dispose();
                        m_Coroutines[idx].Reset();

                        m_ReservedIndices.Enqueue(idx);
                        updateIndices.RemoveAt(i);
                        continue;
                    }
                }
                else if (iter.Iter.Current is UnityEngine.AsyncOperation oper && oper.isDone)
                {
                    if (!iter.Iter.MoveNext())
                    {
                        handler.m_Activated = false;

                        m_Coroutines[idx].Disposable.Dispose();
                        m_Coroutines[idx].Reset();

                        m_ReservedIndices.Enqueue(idx);
                        updateIndices.RemoveAt(i);
                        continue;
                    }
                }
                else if (iter.Iter.Current is UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle operHandle && operHandle.IsDone)
                {
                    if (!iter.Iter.MoveNext())
                    {
                        handler.m_Activated = false;

                        m_Coroutines[idx].Disposable.Dispose();
                        m_Coroutines[idx].Reset();

                        m_ReservedIndices.Enqueue(idx);
                        updateIndices.RemoveAt(i);
                        continue;
                    }
                }
                else if (iter.Iter.Current is ICustomYieldAwaiter yieldAwaiter &&
                    !yieldAwaiter.KeepWait)
                {
                    if (!iter.Iter.MoveNext())
                    {
                        handler.m_Activated = false;

                        m_Coroutines[idx].Disposable.Dispose();
                        m_Coroutines[idx].Reset();

                        m_ReservedIndices.Enqueue(idx);
                        updateIndices.RemoveAt(i);
                        continue;
                    }
                }
                else if (iter.Iter.Current is YieldInstruction &&
                    !(iter.Iter.Current is UnityEngine.AsyncOperation))
                {
                    CoreSystem.Logger.LogError(Channel.Presentation,
                        $"해당 yield return 타입({TypeHelper.ToString(iter.Iter.Current.GetType())})은 지원하지 않습니다");

                    handler.m_Activated = false;

                    m_Coroutines[idx].Disposable.Dispose();
                    m_Coroutines[idx].Reset();

                    m_ReservedIndices.Enqueue(idx);
                    updateIndices.RemoveAt(i);
                    continue;
                }
            }
        }

        protected override PresentationResult OnPresentation()
        {
            CoroutineUpdate(m_UpdateIndices);

            return base.OnPresentation();
        }
        private void PresentationTransformUpdateHandler()
        {
            CoroutineUpdate(m_TransformIndices);
        }
        private void PresentationAfterTransformUpdateHandler()
        {
            CoroutineUpdate(m_AfterTransformIndices);
        }

        #endregion

        private class CoroutineJobPayload : IDisposable
        {
            public IEnumerator Iter;
            public IDisposable Disposable;

            public void Reset()
            {
                if (Disposable != null)
                {
                    Disposable.Dispose();
                }

                Iter = null;
                Disposable = null;
            }
            public void Dispose()
            {
                Reset();
            }
        }

        public bool IsValidHandler(CoroutineHandler handler)
        {
            int index = handler.Index;
            if (index < 0) return false;

            return m_CoroutineHandlers[index].Generation == handler.Generation;
        }
        public bool IsActivatedHandler(CoroutineHandler handler)
        {
            int index = handler.Index;
            if (index < 0) return false;

            return m_CoroutineHandlers[index].m_Activated;
        }

        public CoroutineHandler StartCoroutine<T>(T job) where T : ICoroutineJob
        {
            CoreSystem.Logger.ThreadBlock(nameof(StartCoroutine), ThreadInfo.Unity);

            CoroutineHandler coroutineJob;
            if (m_ReservedIndices.Count == 0)
            {
                coroutineJob = new CoroutineHandler(SystemID, m_Coroutines.Count)
                {
                    m_Generation = 0,
                    m_Loop = job.Loop,
                    m_Activated = true
                };
                m_CoroutineHandlers.Add(coroutineJob);
                m_Coroutines.Add(new CoroutineJobPayload
                {
                    Iter = job.Execute(),
                    Disposable = job
                });
            }
            else
            {
                int index = m_ReservedIndices.Dequeue();

                ref CoroutineHandler corJob = ref m_CoroutineHandlers.ElementAt(index);
                corJob.m_Generation++;

                corJob.m_Loop = job.Loop;
                corJob.m_Activated = true;

                m_Coroutines[index].Iter = job.Execute();
                m_Coroutines[index].Disposable = job;

                coroutineJob = corJob;
            }

            if (job.Loop == UpdateLoop.Transform)
            {
                m_TransformIndices.Add(coroutineJob.Index);
            }
            else if (job.Loop == UpdateLoop.AfterTransform)
            {
                m_AfterTransformIndices.Add(coroutineJob.Index);
            }
            else m_UpdateIndices.Add(coroutineJob.Index);

            return coroutineJob;
        }
        public void StopCoroutine(CoroutineHandler job)
        {
            CoreSystem.Logger.ThreadBlock(nameof(StopCoroutine), ThreadInfo.Unity);

            if (!IsValidHandler(job))
            {

                return;
            }

            ref CoroutineHandler handler = ref m_CoroutineHandlers.ElementAt(job.Index);
            handler.m_Activated = false;
        }
    }
}
