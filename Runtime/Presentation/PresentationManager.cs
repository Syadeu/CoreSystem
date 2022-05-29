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
using Syadeu.Collections.Buffer.LowLevel;
using Syadeu.Collections.ResourceControl;
using Syadeu.Collections.Threading;
using Syadeu.Internal;
using Syadeu.Mono;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Internal;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
//using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.LowLevel;

namespace Syadeu.Presentation
{
    [StaticManagerIntializeOnLoad]
    public sealed class PresentationManager : StaticDataManager<PresentationManager>
    {
        const string c_Instance = "Instance";

        #region Initialize

        internal class Group
        {
            public Type m_Name;
            public Hash m_Hash;
            public Type[] m_RegisteredSystemTypes = null;
            public (Type group, Type system)[] m_RequireSystemTypes = null;
            /// <summary>
            /// 전부 실행되고 돌아가기 직전이 아닌, 시작 명령이 내려졌을때 <see langword="true"/> 가 됩니다.<br/>
            /// 전부 실행을 체크하려면 <seealso cref="m_MainInitDone"/> 을 사용하세요.
            /// </summary>
            public bool m_IsStarted = false;

            public IPresentationSystemGroup m_SystemGroup;

            private readonly List<PresentationSystemEntity> m_Systems = new List<PresentationSystemEntity>();
            private readonly List<IInitPresentation> m_Initializers = new List<IInitPresentation>();
            private readonly List<IBeforePresentation> m_BeforePresentations = new List<IBeforePresentation>();
            private readonly List<IOnPresentation> m_OnPresentations = new List<IOnPresentation>();
            private readonly List<IAfterPresentation> m_AfterPresentations = new List<IAfterPresentation>();
            private readonly List<ITransformPresentation> m_TransformPresentations = new List<ITransformPresentation>();
            private readonly List<IAfterTransformPresentation> m_AfterTransformPresentations = new List<IAfterTransformPresentation>();

            public readonly ConcurrentQueue<Action> m_RequestSystemBindDelegates = new ConcurrentQueue<Action>();
            private readonly List<Hash> m_GroupDependences = new List<Hash>();

            //public CoreRoutine MainPresentation;
            //public CoreRoutine BackgroundPresentation;

            public bool m_MainthreadSignal = false;
            public bool m_BackgroundthreadSignal = false;
            
            public bool m_MainInitDone = false;
            public bool m_BackgroundInitDone = false;

            public ICustomYieldAwaiter m_StartAwaiter;
            public WaitUntil m_WaitUntilInitializeCompleted;

            public bool
                m_MainthreadBeforePre = false,
                m_MainthreadOnPre = false,
                m_MainthreadAfterPre = false,

                m_BackgroundthreadBeforePre = false,
                m_BackgroundthreadOnPre = false,
                m_BackgroundthreadAfterPre = false;

            public WaitUntil
                m_WaitBeforePre, m_WaitOnPre, m_WaitAfterPre;

            public JobHandle 
                m_BeforePresentationJobHandle,
                m_OnPresentationJobHandle,
                m_AfterPresentationJobHandle,
                m_TransformJobHandle,
                m_AfterTransformJobHandle;
            bool
                m_HasBeforePresentationJob,
                m_HasOnPresentationJob,
                m_HasAfterPresentationJob,
                m_HasTransformJob,
                m_HasAfterTransformJob;

            public int Count => m_Systems.Count;
            public List<PresentationSystemEntity> Systems => m_Systems;

            private Unity.Profiling.ProfilerMarker
                m_InitializeMarker, m_InitializeAsyncMarker, m_StartPreMarker,
                m_BeforePreJobMarker, m_OnPreJobMarker, m_AfterPreJobMarker,
                m_BeforePreMarker, m_OnPreMarker, m_AfterPreMarker;

            private List<Unity.Profiling.ProfilerMarker>
                m_BeforePreSystemMarkers, m_OnPreSystemMarkers, m_AfterPreSystemMarkers,
                m_BeforePreAsyncSystemMarkers, m_OnPreAsyncSystemMarkers, m_AfterPreAsyncSystemMarkers;

            public Group(Type name, Hash hash)
            {
                m_Name = name;
                m_Hash = hash;

                m_StartAwaiter = new YieldAwaiter()
                {
                    m_Predicate = () => m_MainInitDone && m_BackgroundInitDone
                };
                m_WaitUntilInitializeCompleted = new WaitUntil(() => m_MainInitDone && m_BackgroundInitDone);

                m_WaitBeforePre = new WaitUntil(() => m_MainthreadBeforePre && m_BackgroundthreadBeforePre);
                m_WaitOnPre = new WaitUntil(() => m_MainthreadOnPre && m_BackgroundthreadOnPre);
                m_WaitAfterPre = new WaitUntil(() => m_MainthreadAfterPre && m_BackgroundthreadAfterPre);

//#if DEBUG_MODE
                m_InitializeMarker = new Unity.Profiling.ProfilerMarker(Unity.Profiling.ProfilerCategory.Scripts, $"{m_Name.Name}.Initialize");
                m_InitializeAsyncMarker = new Unity.Profiling.ProfilerMarker(Unity.Profiling.ProfilerCategory.Scripts, $"{m_Name.Name}.InitializeAsync");
                m_StartPreMarker = new Unity.Profiling.ProfilerMarker(Unity.Profiling.ProfilerCategory.Scripts, $"{m_Name.Name}.StartPresentation");

                m_BeforePreJobMarker = new Unity.Profiling.ProfilerMarker(Unity.Profiling.ProfilerCategory.Scripts, $"{m_Name.Name}.BeforePresentation Complete JobHandle");
                m_OnPreJobMarker = new Unity.Profiling.ProfilerMarker(Unity.Profiling.ProfilerCategory.Scripts, $"{m_Name.Name}.OnPresentation Complete JobHandle");
                m_AfterPreJobMarker = new Unity.Profiling.ProfilerMarker(Unity.Profiling.ProfilerCategory.Scripts, $"{m_Name.Name}.AfterPresentation Complete JobHandle");

                m_BeforePreMarker = new Unity.Profiling.ProfilerMarker(Unity.Profiling.ProfilerCategory.Scripts, $"{m_Name.Name}.BeforePresentation");
                m_OnPreMarker = new Unity.Profiling.ProfilerMarker(Unity.Profiling.ProfilerCategory.Scripts, $"{m_Name.Name}.OnPresentation");
                m_AfterPreMarker = new Unity.Profiling.ProfilerMarker(Unity.Profiling.ProfilerCategory.Scripts, $"{m_Name.Name}.AfterPresentation");

                m_BeforePreSystemMarkers = new List<Unity.Profiling.ProfilerMarker>();
                m_OnPreSystemMarkers = new List<Unity.Profiling.ProfilerMarker>();
                m_AfterPreSystemMarkers = new List<Unity.Profiling.ProfilerMarker>();
                m_BeforePreAsyncSystemMarkers = new List<Unity.Profiling.ProfilerMarker>();
                m_OnPreAsyncSystemMarkers = new List<Unity.Profiling.ProfilerMarker>();
                m_AfterPreAsyncSystemMarkers = new List<Unity.Profiling.ProfilerMarker>();
//#endif
            }

            public sealed class YieldAwaiter : ICustomYieldAwaiter
            {
                public Func<bool> m_Predicate;
                public bool KeepWait => !m_Predicate.Invoke();
            }
            public bool HasSystem<T>(T system) where T : PresentationSystemEntity
                => m_Systems.FindFor((other) => other.Equals(system)) != null;
            public bool HasSystem(Type systemType)
            {
                for (int i = 0; i < m_RegisteredSystemTypes.Length; i++)
                {
                    if (m_RegisteredSystemTypes[i].Equals(systemType)) return true;
                }
                return false;
            }

            public TSystem GetSystem<TSystem>() where TSystem : PresentationSystemEntity
            {
                for (int i = 0; i < m_Systems.Count; i++)
                {
                    if (TypeHelper.TypeOf<TSystem>.Type.IsAssignableFrom(m_Systems[i].GetType()))
                    {
                        return (TSystem)m_Systems[i];
                    }
                }

                return null;
            }
            public bool TryGetSystem(Type type, out PresentationSystemEntity system, out int systemIdx)
            {
                for (int i = 0; i < m_Systems.Count; i++)
                {
                    if (type.IsAssignableFrom(m_Systems[i].GetType()))
                    {
                        system = m_Systems[i];
                        systemIdx = i;
                        return true;
                    }
                }
                systemIdx = -1;
                system = null;
                return false;
            }
            public bool TryGetSystem<T>(out T system, out int systemIdx) where T : PresentationSystemEntity
            {
                if (!TryGetSystem(TypeHelper.TypeOf<T>.Type, out PresentationSystemEntity temp, out systemIdx))
                {
                    system = null;
                    return false;
                }

                system = (T)temp;
                return true;
            }

            public void Add(PresentationSystemEntity system)
            {
                m_Systems.Add(system);
                m_Initializers.Add(system);
                if (system.EnableBeforePresentation)
                {
                    m_BeforePresentations.Add(system);

                    m_BeforePreSystemMarkers.Add(new Unity.Profiling.ProfilerMarker(Unity.Profiling.ProfilerCategory.Scripts, $"{system.GetType().Name}"));
                    m_BeforePreAsyncSystemMarkers.Add(new Unity.Profiling.ProfilerMarker(Unity.Profiling.ProfilerCategory.Scripts, $"{system.GetType().Name}"));
                }
                if (system.EnableOnPresentation)
                {
                    m_OnPresentations.Add(system);

                    m_OnPreSystemMarkers.Add(new Unity.Profiling.ProfilerMarker(Unity.Profiling.ProfilerCategory.Scripts, $"{system.GetType().Name}"));
                    m_OnPreAsyncSystemMarkers.Add(new Unity.Profiling.ProfilerMarker(Unity.Profiling.ProfilerCategory.Scripts, $"{system.GetType().Name}"));
                }
                if (system.EnableAfterPresentation)
                {
                    m_AfterPresentations.Add(system);

                    m_AfterPreSystemMarkers.Add(new Unity.Profiling.ProfilerMarker(Unity.Profiling.ProfilerCategory.Scripts, $"{system.GetType().Name}"));
                    m_AfterPreAsyncSystemMarkers.Add(new Unity.Profiling.ProfilerMarker(Unity.Profiling.ProfilerCategory.Scripts, $"{system.GetType().Name}"));
                }
                if (system.EnableTransformPresentation)
                {
                    m_TransformPresentations.Add(system);
                }
                if (system.EnableAfterTransformPresentation)
                {
                    m_AfterTransformPresentations.Add(system);
                }
            }
            public void Reset()
            {
                m_IsStarted = false;

                CoreSystem.Logger.Log(LogChannel.Presentation,
                    $"{TypeHelper.ToString(m_Name)} group invoke OnShutdown", TypeHelper.ToString(m_Name));
                for (int i = 0; i < m_Systems.Count; i++)
                {
                    m_Systems[i].InternalOnShutdown();
                }
                CoreSystem.Logger.Log(LogChannel.Presentation,
                    $"{TypeHelper.ToString(m_Name)} group invoke OnDispose", TypeHelper.ToString(m_Name));
                for (int i = 0; i < m_Systems.Count; i++)
                {
                    m_Systems[i].Dispose();
                }
                m_Systems.Clear();
                m_Initializers.Clear();
                m_BeforePresentations.Clear();
                m_OnPresentations.Clear();
                m_AfterPresentations.Clear();
                m_TransformPresentations.Clear();
                m_AfterTransformPresentations.Clear();

                m_MainthreadSignal = false;
                m_BackgroundthreadSignal = false;

                m_MainInitDone = false;
                m_BackgroundInitDone = false;
            }

            public void AddGroupDependence(Hash groupHash)
            {
                m_GroupDependences.Add(groupHash);
            }
            public List<Hash> GetGroupDependences() => m_GroupDependences;

            #region Unity Jobs

            public JobHandle GetJobHandle(int pos)
            {
                if (pos == 0) return m_BeforePresentationJobHandle;
                else if (pos == 1) return m_OnPresentationJobHandle;
                else return m_AfterPresentationJobHandle;
            }
            public void SetJobHandle(int pos, JobHandle jobHandle)
            {
                switch (pos)
                {
                    default:
                    case 0:
                        if (m_HasBeforePresentationJob)
                        {
                            m_BeforePresentationJobHandle = JobHandle.CombineDependencies(m_BeforePresentationJobHandle, jobHandle);

                            break;
                        }

                        m_BeforePresentationJobHandle = jobHandle;
                        m_HasBeforePresentationJob = true;

                        break;
                    case 1:
                        if (m_HasOnPresentationJob)
                        {
                            m_OnPresentationJobHandle = JobHandle.CombineDependencies(m_OnPresentationJobHandle, jobHandle);

                            break;
                        }

                        m_OnPresentationJobHandle = jobHandle;
                        m_HasOnPresentationJob = true;

                        break;
                    case 2:
                        if (m_HasAfterPresentationJob)
                        {
                            m_AfterPresentationJobHandle = JobHandle.CombineDependencies(m_AfterPresentationJobHandle, jobHandle);

                            break;
                        }

                        m_AfterPresentationJobHandle = jobHandle;
                        m_HasAfterPresentationJob = true;

                        break;
                    case 3:
                        if (m_HasTransformJob)
                        {
                            m_TransformJobHandle = JobHandle.CombineDependencies(m_TransformJobHandle, jobHandle);

                            break;
                        }

                        m_TransformJobHandle = jobHandle;
                        m_HasTransformJob = true;

                        break;
                    case 4:
                        if (m_HasAfterTransformJob)
                        {
                            m_AfterTransformJobHandle = JobHandle.CombineDependencies(m_AfterTransformJobHandle, jobHandle);

                            break;
                        }

                        m_AfterTransformJobHandle = jobHandle;
                        m_HasAfterTransformJob = true;
                        break;
                }
            }

            #endregion

            #region Presentation Methods

            public void Initialize()
            {
                CoreSystem.Logger.Log(LogChannel.Presentation,
                    $"{TypeHelper.ToString(m_Name)} group invoke OnInitialize", TypeHelper.ToString(m_Name));

                using (m_InitializeMarker.Auto())
                {
                    for (int i = 0; i < m_Initializers.Count; i++)
                    {
                        PresentationResult result = m_Initializers[i].OnInitialize();
                        LogMessage(result);
                    }

                    InitializeModules();
                }
            }
            private void InitializeModules()
            {
                for (int i = 0; i < m_Systems.Count; i++)
                {
                    for (int j = 0; j < m_Systems[i].m_Modules.Length; j++)
                    {
                        try
                        {
                            m_Systems[i].m_Modules[j].InternalOnInitialize();
                        }
                        catch (Exception ex)
                        {
                            CoreSystem.Logger.LogError(LogChannel.Presentation,
                                ex);
                        }
                    }
                }
            }
            public void InitializeAsync()
            {
                CoreSystem.Logger.Log(LogChannel.Presentation,
                    $"{TypeHelper.ToString(m_Name)} group invoke OnInitializeAsync");

                using (m_InitializeAsyncMarker.Auto())
                {
                    for (int i = 0; i < m_Initializers.Count; i++)
                    {
                        PresentationResult result = m_Initializers[i].OnInitializeAsync();
                        LogMessage(result);
                    }

                    InitializeAsyncModules();
                }
            }
            private void InitializeAsyncModules()
            {
                for (int i = 0; i < m_Systems.Count; i++)
                {
                    for (int j = 0; j < m_Systems[i].m_Modules.Length; j++)
                    {
                        try
                        {
                            m_Systems[i].m_Modules[j].InternalOnInitializeAsync();
                        }
                        catch (Exception ex)
                        {
                            CoreSystem.Logger.LogError(LogChannel.Presentation,
                                ex);
                        }
                    }
                }
            }
            public void OnStartPresentation()
            {
                CoreSystem.Logger.Log(LogChannel.Presentation,
                    $"{TypeHelper.ToString(m_Name)} group invoke OnStartPresentation", TypeHelper.ToString(m_Name));

                using (m_StartPreMarker.Auto())
                {
                    for (int i = 0; i < m_Initializers.Count; i++)
                    {
                        m_Initializers[i].OnStartPresentation();

                        for (int j = 0; j < m_Systems[i].m_Modules.Length; j++)
                        {
                            try
                            {
                                m_Systems[i].m_Modules[j].InternalOnStartPresentation();
                            }
                            catch (Exception ex)
                            {
                                CoreSystem.Logger.LogError(LogChannel.Presentation,
                                    ex);
                            }
                        }
                    }

                    //OnStartModules();
                }
            }
            private void OnStartModules()
            {
                for (int i = 0; i < m_Systems.Count; i++)
                {
                    for (int j = 0; j < m_Systems[i].m_Modules.Length; j++)
                    {
                        try
                        {
                            m_Systems[i].m_Modules[j].InternalOnStartPresentation();
                        }
                        catch (Exception ex)
                        {
                            CoreSystem.Logger.LogError(LogChannel.Presentation,
                                ex);
                        }
                    }
                }
            }
            public void BeforePresentation()
            {
                using (m_BeforePreMarker.Auto())
                {
                    m_MainthreadBeforePre = false;

                    // Unity Jobs
                    using (m_BeforePreJobMarker.Auto())
                    {
                        if (m_HasBeforePresentationJob)
                        {
                            m_BeforePresentationJobHandle.Complete();
                            m_HasBeforePresentationJob = false;
                        }
                    }

                    for (int i = 0; i < m_BeforePresentations.Count; i++)
                    {
                        using (m_BeforePreSystemMarkers[i].Auto())
                        {
                            PresentationResult result = m_BeforePresentations[i].BeforePresentation();
                            LogMessage(result);
                        }
                    }

                    BeforePresentationModules();

                    JobHandle.ScheduleBatchedJobs();

                    m_MainthreadBeforePre = true;
                }
            }
            private void BeforePresentationModules()
            {
                for (int i = 0; i < m_Systems.Count; i++)
                {
                    for (int j = 0; j < m_Systems[i].m_Modules.Length; j++)
                    {
                        try
                        {
                            m_Systems[i].m_Modules[j].InternalBeforePresentation();
                        }
                        catch (Exception ex)
                        {
                            CoreSystem.Logger.LogError(LogChannel.Presentation,
                                ex);
                        }
                    }
                }
            }
            public void BeforePresentationAsync()
            {
                for (int i = 0; i < m_BeforePresentations.Count; i++)
                {
                    using (m_BeforePreAsyncSystemMarkers[i].Auto())
                    {
                        PresentationResult result = m_BeforePresentations[i].BeforePresentationAsync();
                        LogMessage(result);
                    }
                }

                BeforePresentationAsyncModules();
            }
            private void BeforePresentationAsyncModules()
            {
                for (int i = 0; i < m_Systems.Count; i++)
                {
                    for (int j = 0; j < m_Systems[i].m_Modules.Length; j++)
                    {
                        try
                        {
                            m_Systems[i].m_Modules[j].InternalBeforePresentationAsync();
                        }
                        catch (Exception ex)
                        {
                            CoreSystem.Logger.LogError(LogChannel.Presentation,
                                ex);
                        }
                    }
                }
            }
            public void OnPresentation()
            {
                using (m_OnPreMarker.Auto())
                {
                    m_MainthreadOnPre = false;

                    // Unity Jobs
                    using (m_OnPreJobMarker.Auto())
                    {
                        if (m_HasOnPresentationJob)
                        {
                            m_OnPresentationJobHandle.Complete();
                            m_HasOnPresentationJob = false;
                        }
                    }

                    for (int i = 0; i < m_OnPresentations.Count; i++)
                    {
                        using (m_OnPreSystemMarkers[i].Auto())
                        {
                            PresentationResult result = m_OnPresentations[i].OnPresentation();
                            LogMessage(result);
                        }
                    }

                    OnPresentationModules();

                    JobHandle.ScheduleBatchedJobs();

                    m_MainthreadOnPre = true;
                }
            }
            private void OnPresentationModules()
            {
                for (int i = 0; i < m_Systems.Count; i++)
                {
                    for (int j = 0; j < m_Systems[i].m_Modules.Length; j++)
                    {
                        try
                        {
                            m_Systems[i].m_Modules[j].InternalOnPresentation();
                        }
                        catch (Exception ex)
                        {
                            CoreSystem.Logger.LogError(LogChannel.Presentation,
                                ex);
                        }
                    }
                }
            }
            public void OnPresentationAsync()
            {
                for (int i = 0; i < m_OnPresentations.Count; i++)
                {
                    using (m_OnPreAsyncSystemMarkers[i].Auto())
                    {
                        PresentationResult result = m_OnPresentations[i].OnPresentationAsync();
                        LogMessage(result);
                    }
                }

                OnPresentationAsyncModules();
            }
            private void OnPresentationAsyncModules()
            {
                for (int i = 0; i < m_Systems.Count; i++)
                {
                    for (int j = 0; j < m_Systems[i].m_Modules.Length; j++)
                    {
                        try
                        {
                            m_Systems[i].m_Modules[j].InternalOnPresentationAsync();
                        }
                        catch (Exception ex)
                        {
                            CoreSystem.Logger.LogError(LogChannel.Presentation,
                                ex);
                        }
                    }
                }
            }
            public void AfterPresentation()
            {
                using (m_AfterPreMarker.Auto())
                {
                    m_MainthreadAfterPre = false;

                    // Unity Jobs
                    using (m_AfterPreJobMarker.Auto())
                    {
                        if (m_HasAfterPresentationJob)
                        {
                            m_AfterPresentationJobHandle.Complete();
                            m_HasAfterPresentationJob = false;
                        }
                    }

                    for (int i = 0; i < m_AfterPresentations.Count; i++)
                    {
                        using (m_AfterPreSystemMarkers[i].Auto())
                        {
                            PresentationResult result = m_AfterPresentations[i].AfterPresentation();
                            LogMessage(result);
                        }
                    }

                    AfterPresentationModules();

                    JobHandle.ScheduleBatchedJobs();

                    m_MainthreadAfterPre = true;
                }
            }
            private void AfterPresentationModules()
            {
                for (int i = 0; i < m_Systems.Count; i++)
                {
                    for (int j = 0; j < m_Systems[i].m_Modules.Length; j++)
                    {
                        try
                        {
                            m_Systems[i].m_Modules[j].InternalAfterPresentation();
                        }
                        catch (Exception ex)
                        {
                            CoreSystem.Logger.LogError(LogChannel.Presentation,
                                ex);
                        }
                    }
                }
            }
            public void AfterPresentationAsync()
            {
                for (int i = 0; i < m_AfterPresentations.Count; i++)
                {
                    using (m_AfterPreAsyncSystemMarkers[i].Auto())
                    {
                        PresentationResult result = m_AfterPresentations[i].AfterPresentationAsync();
                        LogMessage(result);
                    }
                }

                AfterPresentationAsyncModules();
            }
            private void AfterPresentationAsyncModules()
            {
                for (int i = 0; i < m_Systems.Count; i++)
                {
                    for (int j = 0; j < m_Systems[i].m_Modules.Length; j++)
                    {
                        try
                        {
                            m_Systems[i].m_Modules[j].InternalAfterPresentationAsync();
                        }
                        catch (Exception ex)
                        {
                            CoreSystem.Logger.LogError(LogChannel.Presentation,
                                ex);
                        }
                    }
                }
            }

            public void TransformPresentation()
            {
                if (m_HasTransformJob)
                {
                    m_TransformJobHandle.Complete();
                    m_HasTransformJob = false;
                }

                for (int i = 0; i < m_TransformPresentations.Count; i++)
                {
                    try
                    {
                        PresentationResult result = m_TransformPresentations[i].TransformPresentation();
                        LogMessage(result);
                    }
                    catch (Exception ex)
                    {
                        CoreSystem.Logger.LogError(LogChannel.Presentation, ex);
                    }
                }

                TransformPresentationModules();

                JobHandle.ScheduleBatchedJobs();
            }
            private void TransformPresentationModules()
            {
                for (int i = 0; i < m_Systems.Count; i++)
                {
                    for (int j = 0; j < m_Systems[i].m_Modules.Length; j++)
                    {
                        try
                        {
                            m_Systems[i].m_Modules[j].InternalTransformPresentation();
                        }
                        catch (Exception ex)
                        {
                            CoreSystem.Logger.LogError(LogChannel.Presentation, ex);
                        }
                    }
                }
            }
            public void AfterTransformPresentation()
            {
                if (m_HasAfterTransformJob)
                {
                    m_AfterTransformJobHandle.Complete();
                    m_HasAfterTransformJob = false;
                }

                for (int i = 0; i < m_AfterTransformPresentations.Count; i++)
                {
                    PresentationResult result = m_AfterTransformPresentations[i].AfterTransformPresentation();
                    LogMessage(result);
                }

                AfterTransformPresentationModules();

                JobHandle.ScheduleBatchedJobs();
            }
            private void AfterTransformPresentationModules()
            {
                for (int i = 0; i < m_Systems.Count; i++)
                {
                    for (int j = 0; j < m_Systems[i].m_Modules.Length; j++)
                    {
                        try
                        {
                            m_Systems[i].m_Modules[j].InternalAfterTransformPresentation();
                        }
                        catch (Exception ex)
                        {
                            CoreSystem.Logger.LogError(LogChannel.Presentation, ex);
                        }
                    }
                }
            }

            #endregion
        }
        private readonly Hash m_DefaultGroupHash = GroupToHash(TypeHelper.TypeOf<DefaultPresentationGroup>.Type);

        internal readonly Dictionary<Hash, Group> m_PresentationGroups = new Dictionary<Hash, Group>();
        internal readonly Dictionary<string, List<Hash>> m_DependenceSceneList = new Dictionary<string, List<Hash>>();

        private Thread m_PresentationThread;

        private AtomicSafeInteger m_PrefabPreloadCounter;
        private int m_ExpectedPrefabPreloadCount;

        public override void OnInitialize()
        {
            SetPlayerLoop();

            m_BeforeUpdateAsyncSemaphore = new ManualResetEvent(true);
            m_OnUpdateAsyncSemaphore = new ManualResetEvent(true);
            m_AfterUpdateAsyncSemaphore = new ManualResetEvent(true);

            #region Prefab Preloader

            var m_PrefabPreloadStream = new List<PrefabReference>();
            var assetPreloadStream = new List<AssetIndex>();
            {
                using (PrefabPreloader prefabPreloader = new PrefabPreloader(m_PrefabPreloadStream, assetPreloadStream))
                {
                    foreach (var item in EntityDataList.Instance.GetData(t => t is IPrefabPreloader).Select(t => (IPrefabPreloader)t))
                    {
                        item.Register(prefabPreloader);
                    }
                }
                
                m_ExpectedPrefabPreloadCount = 0;
                for (int i = 0; i < m_PrefabPreloadStream.Count; i++)
                {
                    var prefab = m_PrefabPreloadStream[i];

                    if (prefab.Asset != null) continue;

                    var oper = prefab.LoadAssetAsync();
                    oper.Completed += ctx =>
                    {
                        $"loaded {((UnityEngine.Object)ctx.Result).name}".ToLog();
                        m_PrefabPreloadCounter.Increment();
                    };
                    m_ExpectedPrefabPreloadCount++;

#if DEBUG_MODE
                    CoreSystem.Logger.Log(LogChannel.Entity,
                        $"Preloading prefab({prefab.GetObjectSetting().Name}).");
#endif
                }
                for (int i = 0; i < assetPreloadStream.Count; i++)
                {
                    var asset = assetPreloadStream[i].AssetReference;

                    var promise = asset.LoadAssetAsync();
                    promise.OnCompleted += t =>
                    {
                        m_PrefabPreloadCounter.Increment();
                    };
                    m_ExpectedPrefabPreloadCount++;

#if DEBUG_MODE
                    CoreSystem.Logger.Log(LogChannel.Entity,
                        $"Preloading prefab({asset}).");
#endif
                }
            }
            m_PrefabPreloadStream = null;

            #endregion

            StartUnityUpdate(ThreadStart());
        }
        private IEnumerator ThreadStart()
        {
            const string c_ThreadName = "core.pre";

            while (!m_PrefabPreloadCounter.Equals(m_ExpectedPrefabPreloadCount))
            {
                yield return null;
            }
            $"{(int)m_PrefabPreloadCounter} :: {m_ExpectedPrefabPreloadCount} in".ToLog();

            #region Start Presentation

            Type[] registers = TypeHelper.GetTypes(t => !t.IsAbstract && !t.IsInterface && TypeHelper.TypeOf<IPresentationRegister>.Type.IsAssignableFrom(t)).ToArray();

            IPresentationRegister[] presentations = new IPresentationRegister[registers.Length];
            for (int i = 0; i < registers.Length; i++)
            {
                presentations[i] = (IPresentationRegister)Activator.CreateInstance(registers[i]);
                presentations[i].Register();
            }

            StartPresentation(m_DefaultGroupHash);
            for (int i = 0; i < presentations.Length; i++)
            {
                if (presentations[i].StartOnInitialize)
                {
                    StartPresentation(GroupToHash(registers[i]));
                }
                else if (presentations[i].DependenceGroup != null)
                {
#if DEBUG_MODE
                    if (presentations[i].DependenceGroup.IsAbstract ||
                        presentations[i].DependenceGroup.IsInterface)
                    {
                        CoreSystem.Logger.LogError(LogChannel.Presentation,
                            $"On Presentation Group({presentations[i].GetType().Name}), " +
                            $"dependence group cannot be an abstract or interface and must be absolute type." +
                            $"{presentations[i].DependenceGroup.Name} is not allowed.");
                        continue;
                    }
#endif
                    Hash groupHash = GroupToHash(presentations[i].DependenceGroup);
                    if (!Instance.m_PresentationGroups.TryGetValue(groupHash, out Group group))
                    {
                        throw new CoreSystemException(CoreSystemExceptionFlag.Presentation,
                            $"시스템 그룹 {presentations[i].DependenceGroup.Name} 은 등록되지 않았습니다.");
                    }

                    if (group.m_IsStarted)
                    {
                        StartPresentation(GroupToHash(registers[i]));
                        continue;
                    }

                    group.AddGroupDependence(GroupToHash(presentations[i].GetType()));
                }
            }

            #endregion

            m_PresentationThread = new Thread(PresentationAsyncUpdate)
            {
                Name = c_ThreadName,
                IsBackground = true,
                CurrentCulture = global::System.Globalization.CultureInfo.InvariantCulture,
                CurrentUICulture = global::System.Globalization.CultureInfo.InvariantCulture
            };
            //m_PresentationThread.SetApartmentState(ApartmentState.STA);

            m_PresentationThread.Start(this);

            yield break;
        }
        private void SetPlayerLoop()
        {
            PlayerLoopSystem defaultLoop = PlayerLoop.GetCurrentPlayerLoop();
            for (int i = 0; i < defaultLoop.subSystemList.Length; i++)
            {
                if (defaultLoop.subSystemList[i].type.Equals(TypeHelper.TypeOf<UnityEngine.PlayerLoop.PreUpdate>.Type))
                {
                    List<PlayerLoopSystem> list = defaultLoop.subSystemList[i].subSystemList.ToList();
                    PlayerLoopSystem loop = new PlayerLoopSystem
                    {
                        loopConditionFunction = defaultLoop.subSystemList[i].loopConditionFunction,
                        subSystemList = Array.Empty<PlayerLoopSystem>(),
                        type = TypeHelper.TypeOf<PresentationLoop.PresentationPreUpdate>.Type,
                        updateDelegate = PresentationPreUpdate,
                        updateFunction = defaultLoop.subSystemList[i].updateFunction
                    };
                    list.Add(loop);
                    defaultLoop.subSystemList[i].subSystemList = list.ToArray();
                }
                else if (defaultLoop.subSystemList[i].type.Equals(TypeHelper.TypeOf<UnityEngine.PlayerLoop.Update>.Type))
                {
                    List<PlayerLoopSystem> list = defaultLoop.subSystemList[i].subSystemList.ToList();
                    PlayerLoopSystem loop = new PlayerLoopSystem
                    {
                        loopConditionFunction = defaultLoop.subSystemList[i].loopConditionFunction,
                        subSystemList = Array.Empty<PlayerLoopSystem>(),
                        type = TypeHelper.TypeOf<PresentationLoop.PresentationBeforeUpdate>.Type,
                        updateDelegate = PresentationBeforeUpdate,
                        updateFunction = defaultLoop.subSystemList[i].updateFunction
                    };
                    PlayerLoopSystem loop1 = new PlayerLoopSystem
                    {
                        loopConditionFunction = defaultLoop.subSystemList[i].loopConditionFunction,
                        subSystemList = Array.Empty<PlayerLoopSystem>(),
                        type = TypeHelper.TypeOf<PresentationLoop.PresentationOnUpdate>.Type,
                        updateDelegate = PresentationOnUpdate,
                        updateFunction = defaultLoop.subSystemList[i].updateFunction
                    };
                    PlayerLoopSystem loop2 = new PlayerLoopSystem
                    {
                        loopConditionFunction = defaultLoop.subSystemList[i].loopConditionFunction,
                        subSystemList = Array.Empty<PlayerLoopSystem>(),
                        type = TypeHelper.TypeOf<PresentationLoop.PresentationAfterUpdate>.Type,
                        updateDelegate = PresentationAfterUpdate,
                        updateFunction = defaultLoop.subSystemList[i].updateFunction
                    };
                    list.Add(loop);
                    list.Add(loop1);
                    list.Add(loop2);
                    defaultLoop.subSystemList[i].subSystemList = list.ToArray();
                }
                else if (defaultLoop.subSystemList[i].type.Equals(TypeHelper.TypeOf<UnityEngine.PlayerLoop.PreLateUpdate>.Type))
                {
                    List<PlayerLoopSystem> list = defaultLoop.subSystemList[i].subSystemList.ToList();
                    PlayerLoopSystem loop = new PlayerLoopSystem
                    {
                        loopConditionFunction = defaultLoop.subSystemList[i].loopConditionFunction,
                        subSystemList = Array.Empty<PlayerLoopSystem>(),
                        type = TypeHelper.TypeOf<PresentationLoop.PresentationLateUpdate.TransformUpdate>.Type,
                        updateDelegate = PresentationLateTransformUpdate,
                        updateFunction = defaultLoop.subSystemList[i].updateFunction
                    };
                    PlayerLoopSystem loop2 = new PlayerLoopSystem
                    {
                        loopConditionFunction = defaultLoop.subSystemList[i].loopConditionFunction,
                        subSystemList = Array.Empty<PlayerLoopSystem>(),
                        type = TypeHelper.TypeOf<PresentationLoop.PresentationLateUpdate.AfterTransformUpdate>.Type,
                        updateDelegate = PresentationLateAfterTransformUpdate,
                        updateFunction = defaultLoop.subSystemList[i].updateFunction
                    };
                    list.Add(loop);
                    list.Add(loop2);
                    defaultLoop.subSystemList[i].subSystemList = list.ToArray();
                }
                else if (defaultLoop.subSystemList[i].type.Equals(TypeHelper.TypeOf<UnityEngine.PlayerLoop.PostLateUpdate>.Type))
                {
                    List<PlayerLoopSystem> list = defaultLoop.subSystemList[i].subSystemList.ToList();
                    PlayerLoopSystem loop = new PlayerLoopSystem
                    {
                        loopConditionFunction = defaultLoop.subSystemList[i].loopConditionFunction,
                        subSystemList = Array.Empty<PlayerLoopSystem>(),
                        type = TypeHelper.TypeOf<PresentationLoop.PresentationPostUpdate>.Type,
                        updateDelegate = PresentationPostUpdate,
                        updateFunction = defaultLoop.subSystemList[i].updateFunction
                    };
                    list.Add(loop);
                    defaultLoop.subSystemList[i].subSystemList = list.ToArray();
                }
            }

            PlayerLoop.SetPlayerLoop(defaultLoop);
        }
        public override void Dispose()
        {
            PresentationSystemEntity.s_GlobalJobHandle.Complete();
            foreach (var item in m_PresentationGroups)
            {
                if (item.Value.m_IsStarted)
                {
                    BeforeUpdate -= item.Value.BeforePresentation;
                    BeforeUpdateAsync -= item.Value.BeforePresentationAsync;
                    Update -= item.Value.OnPresentation;
                    UpdateAsync -= item.Value.OnPresentationAsync;
                    AfterUpdate -= item.Value.AfterPresentation;
                    AfterUpdateAsync -= item.Value.AfterPresentationAsync;

                    TransformUpdate -= item.Value.TransformPresentation;
                    AfterTransformUpdate -= item.Value.AfterTransformPresentation;

                    try
                    {
                        item.Value.Reset();
                    }
                    catch (Exception ex)
                    {
                        CoreSystem.Logger.LogError(LogChannel.Presentation, ex);
                    }
                }
            }

            PlayerLoopSystem defaultLoop = PlayerLoop.GetDefaultPlayerLoop();
            PlayerLoop.SetPlayerLoop(defaultLoop);

            UnsafeBufferUtility.DisposeAllSafeties();

            base.Dispose();
        }

        #endregion

        #region Player Loops

        internal event Action PreUpdate;
        
        internal event Action BeforeUpdate;
        internal event Action BeforeUpdateAsync;
        internal event Action Update;
        internal event Action UpdateAsync;
        internal event Action AfterUpdate;
        internal event Action AfterUpdateAsync;
        internal event Action TransformUpdate;
        internal event Action AfterTransformUpdate;

        internal event Action PostUpdate;

#if DEBUG_MODE
        private Unity.Profiling.ProfilerMarker
                m_PrePresentationMarker = new Unity.Profiling.ProfilerMarker("Update Method (PreUpdate)"),
                m_PostPresentationMarker = new Unity.Profiling.ProfilerMarker("Update Method (PostUpdate)"),

                m_BeforeSemaphoreMarker = new Unity.Profiling.ProfilerMarker(Unity.Profiling.ProfilerCategory.Internal, "Semaphore.Set (BeforeUpdate)"),
                m_OnSemaphoreMarker = new Unity.Profiling.ProfilerMarker(Unity.Profiling.ProfilerCategory.Internal, "Semaphore.Set (OnUpdate)"),
                m_AfterSemaphoreMarker = new Unity.Profiling.ProfilerMarker(Unity.Profiling.ProfilerCategory.Internal, "Semaphore.Set (AfterUpdate)");
#endif

        private void PresentationPreUpdate()
        {
            try
            {
#if DEBUG_MODE
                using (m_PrePresentationMarker.Auto())
#endif
                {
                    PreUpdate?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Dispose();
            }
        }

        private void PresentationBeforeUpdate()
        {
            try
            {
#if DEBUG_MODE
                using (m_BeforeSemaphoreMarker.Auto())
#endif
                {
                    m_BeforeUpdateAsyncSemaphore.Set();
                }
                BeforeUpdate?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Dispose();
            }
        }
        private void PresentationOnUpdate()
        {
            try
            {
#if DEBUG_MODE
                using (m_OnSemaphoreMarker.Auto())
#endif
                {
                    m_OnUpdateAsyncSemaphore.Set();
                }
                Update?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Dispose();
            }
        }
        private void PresentationAfterUpdate()
        {
            try
            {
#if DEBUG_MODE
                using (m_AfterSemaphoreMarker.Auto())
#endif
                {
                    m_AfterUpdateAsyncSemaphore.Set();
                }
                AfterUpdate?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Dispose();
            }
        }

        private void PresentationLateTransformUpdate()
        {
            try
            {
                TransformUpdate?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Dispose();
            }
        }
        private void PresentationLateAfterTransformUpdate()
        {
            try
            {
                AfterTransformUpdate?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Dispose();
            }
        }

        private void PresentationPostUpdate()
        {
            try
            {
#if DEBUG_MODE
                using (m_PostPresentationMarker.Auto())
#endif
                {
                    PostUpdate?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Dispose();
            }
        }

        private ManualResetEvent
            m_BeforeUpdateAsyncSemaphore,
            m_OnUpdateAsyncSemaphore,
            m_AfterUpdateAsyncSemaphore;

        private static void PresentationAsyncUpdate(object obj)
        {
#if DEBUG_MODE
            Unity.Profiling.ProfilerMarker
                simSemaphoreMarker = new Unity.Profiling.ProfilerMarker(Unity.Profiling.ProfilerCategory.Internal, "Semaphore.WaitOne (Simulation)"),

                beforeSemaphoreMarker = new Unity.Profiling.ProfilerMarker(Unity.Profiling.ProfilerCategory.Internal, "Semaphore.WaitOne (BeforeUpdate)"),
                onSemaphoreMarker = new Unity.Profiling.ProfilerMarker(Unity.Profiling.ProfilerCategory.Internal, "Semaphore.WaitOne (OnUpdate)"),
                afterSemaphoreMarker = new Unity.Profiling.ProfilerMarker(Unity.Profiling.ProfilerCategory.Internal, "Semaphore.WaitOne (AfterUpdate)"),

                beforeUpdateMarker = new Unity.Profiling.ProfilerMarker("BeforeUpdate"),
                onUpdateMarker = new Unity.Profiling.ProfilerMarker("OnUpdate"),
                afterUpdateMarker = new Unity.Profiling.ProfilerMarker("AfterUpdate");

            UnityEngine.Profiling.CustomSampler
                PresentationUpdateMarker = UnityEngine.Profiling.CustomSampler.Create("PresentationAsyncUpdate");

                //beforeUpdateMarker = UnityEngine.Profiling.CustomSampler.Create("BeforeUpdate"),
                //onUpdateMarker = UnityEngine.Profiling.CustomSampler.Create("OnUpdate"),
                //afterUpdateMarker = UnityEngine.Profiling.CustomSampler.Create("AfterUpdate");

            UnityEngine.Profiling.Profiler.BeginThreadProfiling("Syadeu", "CoreSystem.Presentation");
#endif
            PresentationManager mgr = (PresentationManager)obj;

            CoreSystem.SimulateWatcher.WaitOne();

            while (!CoreSystem.BlockCreateInstance)
            {
#if UNITY_EDITOR
                if (CoreSystem.IsEditorPaused) continue;
#endif
#if DEBUG_MODE
                using (simSemaphoreMarker.Auto())
#endif
                {
                    while (!CoreSystem.BlockCreateInstance)
                    {
                        if (CoreSystem.SimulateWatcher.WaitOne(1)) break;
                    }
                    if (CoreSystem.BlockCreateInstance) break;
                }
#if DEBUG_MODE
                PresentationUpdateMarker.Begin();
                using (beforeSemaphoreMarker.Auto())
#endif
                {
                    while (!CoreSystem.BlockCreateInstance)
                    {
                        if (mgr.m_BeforeUpdateAsyncSemaphore.WaitOne(1)) break;
                    }
                    if (CoreSystem.BlockCreateInstance) break;
                }
#if DEBUG_MODE
                using (beforeUpdateMarker.Auto())
#endif
                {
                    //"in befor".ToLog();
                    mgr.BeforeUpdateAsync?.Invoke();
                    mgr.m_BeforeUpdateAsyncSemaphore.Reset();

                    //TestStress();
                }
#if DEBUG_MODE
                using (onSemaphoreMarker.Auto())
#endif
                {
                    while (!CoreSystem.BlockCreateInstance)
                    {
                        if (mgr.m_OnUpdateAsyncSemaphore.WaitOne(1)) break;
                    }
                    if (CoreSystem.BlockCreateInstance) break;
                }
                
#if DEBUG_MODE
                using (onUpdateMarker.Auto())
#endif
                {
                    //"in on".ToLog();
                    mgr.UpdateAsync?.Invoke();
                    mgr.m_OnUpdateAsyncSemaphore.Reset();

                    //TestStress();
                }
#if DEBUG_MODE
                using (afterSemaphoreMarker.Auto())
#endif
                {
                    while (!CoreSystem.BlockCreateInstance)
                    {
                        if (mgr.m_AfterUpdateAsyncSemaphore.WaitOne(1)) break;
                    }
                    if (CoreSystem.BlockCreateInstance) break;
                }
#if DEBUG_MODE
                using (afterUpdateMarker.Auto())
#endif
                {
                    //"in after".ToLog();
                    mgr.AfterUpdateAsync?.Invoke();
                    mgr.m_AfterUpdateAsyncSemaphore.Reset();

                    //TestStress();
                }
#if DEBUG_MODE
                PresentationUpdateMarker.End();
#endif
            }

            mgr.m_BeforeUpdateAsyncSemaphore.Dispose();
            mgr.m_OnUpdateAsyncSemaphore.Dispose();
            mgr.m_AfterUpdateAsyncSemaphore.Dispose();

#if DEBUG_MODE
            UnityEngine.Profiling.Profiler.EndThreadProfiling();
#endif
            UnityEngine.Debug.Log("thread out");
        }

        // TODO: Debug, Profiling 을 위해 따로 Wrapping
        //private void AddPreUpdate(Action action)
        //{
        //    PreUpdate += action;
        //}
        //private void RemovePreUpdate(Action action)
        //{
        //    PreUpdate -= action;
        //}

        //private void AddPostUpdate(Action action)
        //{

        //}
        //private void RemovePostUpdate(Action action)
        //{

        //}

        [System.Diagnostics.Conditional("DEBUG_MODE")]
        private static void TestStress()
        {
            for (int i = 0; i < 100000; i++)
            {
                Math.Sqrt(2.5f);
            }
        }

        #endregion

        #region Internals

        internal static Hash GroupToHash(Type group)
        {
            return Hash.NewHash(group.Name);
        }

        internal static void RegisterSystem(Type groupName, SceneReference dependenceScene, params Type[] systems)
        {
            if (dependenceScene != null)
            {
                CoreSystem.Logger.Log(LogChannel.Presentation, $"Registration start ({groupName.Name.Split('.').Last()}), number of {systems.Length}, has dependece scene ({dependenceScene.ScenePath})");
            }
            else CoreSystem.Logger.Log(LogChannel.Presentation, $"Registration start ({groupName.Name.Split('.').Last()}), number of {systems.Length}");

            Hash groupHash = GroupToHash(groupName);
            if (!Instance.m_PresentationGroups.TryGetValue(groupHash, out Group group))
            {
                group = new Group(groupName, groupHash);
                Instance.m_PresentationGroups.Add(groupHash, group);

                //Type t = typeof(PresentationSystemGroup<>).MakeGenericType(groupName);
                //$"{t.Name}: {t.GenericTypeArguments[0]}".ToLog();
                PropertyInfo insProperty = typeof(PresentationSystemGroup<>).MakeGenericType(groupName).GetProperty(c_Instance, BindingFlags.NonPublic | BindingFlags.Static);
                
                CoreSystem.Logger.NotNull(insProperty);

                //$"{insProperty.Name}".ToLog();
                //Assert.IsNotNull(insProperty.GetValue(null, null));
                group.m_SystemGroup = (IPresentationSystemGroup)insProperty.GetValue(null, null);
                CoreSystem.Logger.NotNull(group.m_SystemGroup);
            }

            if (dependenceScene != null)
            {
                if (!Instance.m_DependenceSceneList.TryGetValue(dependenceScene, out List<Hash> list))
                {
                    list = new List<Hash>();
                    Instance.m_DependenceSceneList.Add(dependenceScene, list);
                }

                if (list.Contains(groupHash)) CoreSystem.Logger.LogError(LogChannel.Presentation, $"{groupName.Name.Split('.').Last()} 은 이미 해당 씬({dependenceScene.ScenePath})에 종속되었습니다. 중복 추가는 허용하지 않습니다.");
                list.Add(groupHash);
            }

            List<Type> registedTypes = new List<Type>();
            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i].IsAbstract || systems[i].IsInterface)
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Presentation,
                        $"{systems[i].Name} 은 등록할 수 없는 시스템입니다. absract 혹은 interface 클래스인가요?");
                }
                if (!typeof(PresentationSystemEntity<>).MakeGenericType(systems[i]).IsAssignableFrom(systems[i]))
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Presentation,
                        $"{systems[i].Name}은 PresentationSystemEntity 을 상속받지 않아, 등록할 수 없습니다.");
                }
                if (registedTypes.Contains(systems[i]))
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Presentation,
                        $"{systems[i].Name}은 이미 등록된 시스템입니다.");
                }

                registedTypes.Add(systems[i]);

                //if (Instance.m_RegisteredGroup.ContainsKey(systems[i]))
                //{
                //    CoreSystem.Logger.LogWarning(Channel.Presentation,
                //        $"Multiple system({TypeHelper.ToString(systems[i])}, in group {TypeHelper.ToString(groupName)}) detected. " +
                //        $"This instance will not gathered by PresentationSystem<T> but PresentationSystemGroup<T>.Systems.");
                //}
                //else
                //{
                //    Instance.m_RegisteredGroup.Add(systems[i], groupHash);
                //}
                
                CoreSystem.Logger.Log(LogChannel.Presentation, $"System ({groupName.Name.Split('.').Last()}): {systems[i].Name} Registered");
            }

            group.m_RegisteredSystemTypes = registedTypes.ToArray();

            var requireIter = registedTypes
                .Where((other) => other.GetCustomAttribute<SubSystemAttribute>() != null)
                .Select((other) => other.GetCustomAttribute<SubSystemAttribute>())
                .Select((other) => (other.m_TargetGroup, other.m_TargetSystem));
            if (requireIter.Any())
            {
                group.m_RequireSystemTypes = requireIter.ToArray();
            }
            else group.m_RequireSystemTypes = Array.Empty<(Type, Type)>();

            CoreSystem.Logger.Log(LogChannel.Presentation, $"Registration Ended ({groupName.Name.Split('.').Last()}), number of {systems.Length}");
        }
        internal ICustomYieldAwaiter StartPresentation(Hash groupHash)
        {
            Group group = m_PresentationGroups[groupHash];
            if (group.m_IsStarted)
            {
                CoreSystem.Logger.LogError(LogChannel.Presentation,
                    $"Presentation Group {group.m_Name.Name} has already started and running. Request ignored.");
                return new NullCustomYieldAwaiter();
            }

            CoreSystem.Logger.Log(LogChannel.Presentation,
                $"start call {group.m_Name.Name}");

            for (int i = 0; i < group.m_RegisteredSystemTypes.Length; i++)
            {
                Type t = group.m_RegisteredSystemTypes[i];
                ConstructorInfo ctor = t.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, CallingConventions.HasThis, Array.Empty<Type>(), null);
                object ins;
                if (ctor != null)
                {
                    ins = ctor.Invoke(null);
                }
                else ins = Activator.CreateInstance(t);

                PresentationSystemEntity system = (PresentationSystemEntity)ins;

                system.m_GroupIndex = groupHash;
                system.m_SystemIndex = group.Count;

                system.SetJobHandle = group.SetJobHandle;
                system.GetJobHandle = group.GetJobHandle;

                // Modules
                var moduleIter = t.GetInterfaces()
                    .Where(i => i.IsGenericType)
                    .Where(i => i.GetGenericTypeDefinition() == typeof(INotifySystemModule<>));
                system.m_Modules = new PresentationSystemModule[moduleIter.Count()];
                int index = 0;
#if DEBUG_MODE
                HashSet<Type> addedModules = new HashSet<Type>();
#endif
                foreach (Type moduleType in moduleIter)
                {
                    Type module = moduleType.GetGenericArguments()[0];
#if DEBUG_MODE
                    if (addedModules.Contains(module))
                    {
                        CoreSystem.Logger.LogError(LogChannel.Presentation,
                            $"You\'re adding same module({TypeHelper.ToString(module)}) " +
                            $"more then one at {TypeHelper.ToString(t)}. " +
                            $"This is not allowed.");

                        throw new InvalidOperationException("See error log");
                    }
                    addedModules.Add(module);
#endif
                    system.m_Modules[index] 
                        = (PresentationSystemModule)Activator.CreateInstance(module);

                    system.m_Modules[index].m_System = system;

                    index += 1;
                }
                //

                group.Add(system);
            }

            //group.MainPresentation = Instance.StartUnityUpdate(Presentation(group));
            Instance.StartUnityUpdate(Presentation(group));
            Instance.StartBackgroundUpdate(PresentationAsync(group));
            group.m_IsStarted = true;

            CoreSystem.Logger.Log(LogChannel.Presentation, $"{group.m_Name.Name} group is started");

            List<Hash> connectedGroups = group.GetGroupDependences();

            CoreSystem.Logger.Log(LogChannel.Presentation,
                $"{group.m_Name.Name} has connected group {connectedGroups.Count}");

            for (int i = 0; i < connectedGroups.Count; i++)
            {
                StartPresentation(connectedGroups[i]);
            }

            return group.m_StartAwaiter;
        }
        internal void StopPresentation(Hash groupHash)
        {
            Group group = m_PresentationGroups[groupHash];
            if (!group.m_IsStarted)
            {
                CoreSystem.Logger.LogWarning(LogChannel.Presentation, 
                    $"Presentation Group {group.m_Name.Name} has already stopped. Request ignored.");
                return;
            }

            Instance.BeforeUpdate -= group.BeforePresentation;
            Instance.Update -= group.OnPresentation;
            Instance.AfterUpdate -= group.AfterPresentation;

            Instance.BeforeUpdateAsync -= group.BeforePresentationAsync;
            Instance.UpdateAsync -= group.OnPresentationAsync;
            Instance.AfterUpdateAsync -= group.AfterPresentationAsync;

            TransformUpdate -= group.TransformPresentation;
            AfterTransformUpdate -= group.AfterTransformPresentation;

            group.Reset();

            CoreSystem.Logger.Log(LogChannel.Presentation, $"{group.m_Name.Name} group is stopped");

            List<Hash> connectedGroups = group.GetGroupDependences();
            for (int i = 0; i < connectedGroups.Count; i++)
            {
                StopPresentation(connectedGroups[i]);
            }
        }

        /// <summary>
        /// 시스템을 요청합니다. <typeparamref name="TGroup"/> 은 요청할 <typeparamref name="TSystem"/>이 속한 그룹입니다. 기본 시스템 그룹은 <seealso cref="DefaultPresentationGroup"/> 입니다.
        /// </summary>
        /// <remarks>
        /// OnInitialize 혹은 OnInitializeAsync 에서만 수행되어야합니다.
        /// </remarks>
        /// <typeparam name="TGroup"></typeparam>
        /// <typeparam name="TSystem"></typeparam>
        /// <param name="bind"></param>
        internal static void RegisterRequest<TGroup, TSystem>(Action<TSystem> bind
#if DEBUG_MODE
            , string methodName
#endif
            )
            where TGroup : PresentationGroupEntity
            where TSystem : PresentationSystemEntity
        {
#if DEBUG_MODE
            if (TypeHelper.TypeOf<TGroup>.IsAbstract || TypeHelper.TypeOf<TGroup>.Type.IsInterface)
            {
                CoreSystem.Logger.LogError(LogChannel.Presentation,
                    $"Requesting group type must be not abstract or interface but {TypeHelper.TypeOf<TGroup>.Name} is.");
                return;
            }
#endif
            Hash groupHash = GroupToHash(TypeHelper.TypeOf<TGroup>.Type);
#if DEBUG_MODE
            if (!Instance.m_PresentationGroups.ContainsKey(groupHash))
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Presentation,
                    $"시스템 그룹 {typeof(TGroup).Name} 은 등록되지 않았습니다.");
            }
#endif
            Group group = Instance.m_PresentationGroups[groupHash];

            if (!group.m_MainthreadSignal)
            {
                group.m_RequestSystemBindDelegates.Enqueue(() =>
                {
                    if (!group.TryGetSystem<TSystem>(out TSystem target, out _))
                    {
#if DEBUG_MODE
                        CoreSystem.Logger.LogError(LogChannel.Presentation, $"Requested system ({TypeHelper.TypeOf<TSystem>.Name}) not found, from {methodName}");
#endif
                        return;

                    }

                    CoreSystem.Logger.Log(LogChannel.Presentation, $"Requested system ({TypeHelper.TypeOf<TSystem>.Name}) found");

                    bind.Invoke(target);
                });
                return;
            }

            if (group.TryGetSystem<TSystem>(out TSystem system, out _))
            {
                try
                {
                    bind.Invoke(system);
                }
                catch (Exception ex)
                {
                    CoreSystem.Logger.LogError(LogChannel.Presentation, ex);
                }
                
                return;
            }

#if DEBUG_MODE
            CoreSystem.Logger.LogError(LogChannel.Presentation,
                $"Requested system ({TypeHelper.TypeOf<TSystem>.Name}) is not part of group {TypeHelper.TypeOf<TGroup>.Name}, from {methodName}");
#endif
        }
        internal static TSystem GetSystem<TGroup, TSystem>(out int systemIdx)
            where TGroup : PresentationGroupEntity
            where TSystem : PresentationSystemEntity
        {
            Hash groupHash = GroupToHash(TypeHelper.TypeOf<TGroup>.Type);

            if (!Instance.m_PresentationGroups.TryGetValue(groupHash, out Group group))
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Presentation,
                    $"시스템 그룹 {typeof(TGroup).Name} 은 등록되지 않았습니다.");
            }

            if (!group.TryGetSystem<TSystem>(out TSystem target, out systemIdx))
            {
                CoreSystem.Logger.LogError(LogChannel.Presentation, $"Requested system ({TypeHelper.TypeOf<TSystem>.Name}) not found");
            }
            else CoreSystem.Logger.Log(LogChannel.Presentation, $"Requested system ({TypeHelper.TypeOf<TSystem>.Name}) found");

            return target;
        }
        internal static bool TryGetSystem<TGroup, TSystem>(out TSystem system, out Hash groupHash, out int systemIdx)
            where TGroup : PresentationGroupEntity
            where TSystem : PresentationSystemEntity
        {
            groupHash = GroupToHash(TypeHelper.TypeOf<TGroup>.Type);

            if (!Instance.m_PresentationGroups.TryGetValue(groupHash, out Group group))
            {
                systemIdx = -1;
                system = null;
                return false;
            }

            if (!group.TryGetSystem<TSystem>(out system, out systemIdx))
            {
                return false;
            }
            else CoreSystem.Logger.Log(LogChannel.Presentation, $"Requested system ({TypeHelper.TypeOf<TSystem>.Name}) found");

            return true;
        }
        internal static TSystem GetSystem<TSystem>(in Hash groupHash)
            where TSystem : PresentationSystemEntity
        {
            if (Instance == null) return null;
            
            if (!Instance.m_PresentationGroups.TryGetValue(groupHash, out Group group))
            {
                return null;
            }

            return group.GetSystem<TSystem>();
        }

        #endregion

        #region Presentation Group Method

        [System.Diagnostics.Conditional("DEBUG_MODE")]
        private static void LogMessage(PresentationResult result)
        {
            if (result.m_Result == ResultFlag.Normal) return;

            if (result.m_Result == ResultFlag.Warning) CoreSystem.Logger.LogWarning(LogChannel.Presentation, result.m_Message);
            else if (result.m_Result == ResultFlag.Error) CoreSystem.Logger.LogError(LogChannel.Presentation, result.m_Message);
        }
        private static IEnumerator Presentation(Group group)
        {
            group.Initialize();
            group.m_MainthreadSignal = true;

            float dateTime = Time.realtimeSinceStartup;
            for (int i = 0; i < group.m_RequireSystemTypes.Length; i++)
            {
                //if (group.m_RequireSystemTypes[i] == null) continue;

                Hash groupHash = GroupToHash(group.m_RequireSystemTypes[i].group);

                if (!Instance.m_PresentationGroups.TryGetValue(groupHash, out Group targetGroup))
                {
                    CoreSystem.Logger.LogError(LogChannel.Presentation,
                        $"{group.m_RequireSystemTypes[i].system.Name} is not registered. Request ignored.");
                    continue;
                }
#if DEBUG_MODE
                if (targetGroup.Equals(group))
                {
                    CoreSystem.Logger.LogError(LogChannel.Presentation,
                        $"This system({group.m_RegisteredSystemTypes[i].Name}) declared sub system of it\'s own system group. This is not allowed.");
                    continue;
                }
#endif
                dateTime = Time.realtimeSinceStartup;
                while (!targetGroup.m_MainInitDone)
                {
                    if (dateTime + 10 < Time.realtimeSinceStartup)
                    {
                        CoreSystem.Logger.LogWarning(LogChannel.Presentation,
                            $"The system group({group.m_Name.Name}) is awaiting too long because waiting system initialize target ({group.m_RequireSystemTypes[i].system.Name})");

                        dateTime = Time.realtimeSinceStartup;
                    }
                    yield return null;
                }
            }

            for (int i = 0; i < group.Systems.Count; i++)
            {
                while (!group.Systems[i].IsStartable)
                {
                    yield return null;
                }
            }

            dateTime = Time.realtimeSinceStartup;
            yield return new WaitUntil(() =>
            {
                if (dateTime + 10 < Time.realtimeSinceStartup)
                {
                    CoreSystem.Logger.LogWarning(LogChannel.Presentation,
                        $"The system({group.m_Name.Name}) is awaiting too long. Maybe unexpected handles?");

                    dateTime = Time.realtimeSinceStartup;
                }
                return group.m_BackgroundthreadSignal;
            });

            group.OnStartPresentation();
            
            group.m_MainInitDone = true;

            yield return group.m_WaitUntilInitializeCompleted;
            CoreSystem.Logger.Log(LogChannel.Presentation, $"Presentation group ({group.m_Name.Name}) started");

            Instance.BeforeUpdate += group.BeforePresentation;
            Instance.Update += group.OnPresentation;
            Instance.AfterUpdate += group.AfterPresentation;
            Instance.TransformUpdate += group.TransformPresentation;
            Instance.AfterTransformUpdate += group.AfterTransformPresentation;
        }
        private static IEnumerator PresentationAsync(Group group)
        {
            #region Initialize

            group.InitializeAsync();

            yield return new WaitUntil(() => group.m_MainthreadSignal);
            int requestSystemCount = group.m_RequestSystemBindDelegates.Count;
            for (int i = 0; i < requestSystemCount; i++)
            {
                //$"asd : {i} = {requestSystemCount}".ToLog();
                if (!group.m_RequestSystemBindDelegates.TryDequeue(out Action action)) continue;
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex);
                    throw;
                }
            }

            group.m_BackgroundthreadSignal = true;
            group.m_BackgroundInitDone = true;

            #endregion

            yield return group.m_WaitUntilInitializeCompleted;

            Instance.BeforeUpdateAsync += group.BeforePresentationAsync;
            Instance.UpdateAsync += group.OnPresentationAsync;
            Instance.AfterUpdateAsync += group.AfterPresentationAsync;
        }
        #endregion

        private sealed class UpdateHelper
        {
            public Action m_Action;
            public string m_CalledFrom;

            public void Invoke()
            {
                m_Action.Invoke();
            }
        }
    }
}
