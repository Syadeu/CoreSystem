﻿using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Mono;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Internal;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
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
            public SubSystemAttribute[] m_RequireSystemTypes = null;
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

            public readonly ConcurrentQueue<Action> m_RequestSystemDelegates = new ConcurrentQueue<Action>();
            private readonly List<Hash> m_GroupDependences = new List<Hash>();

            //public CoreRoutine MainPresentation;
            public CoreRoutine BackgroundPresentation;

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
                m_AfterPresentationJobHandle;

            public int Count => m_Systems.Count;
            public List<PresentationSystemEntity> Systems => m_Systems;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            private Unity.Profiling.ProfilerMarker
                m_InitializeMarker, m_InitializeAsyncMarker, m_StartPreMarker,
                m_BeforePreMarker, m_OnPreMarker, m_AfterPreMarker;

            private List<Unity.Profiling.ProfilerMarker>
                m_BeforePreSystemMarkers, m_OnPreSystemMarkers, m_AfterPreSystemMarkers;
#endif

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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                m_InitializeMarker = new Unity.Profiling.ProfilerMarker(Unity.Profiling.ProfilerCategory.Scripts, $"{m_Name.Name}.Initialize");
                m_InitializeAsyncMarker = new Unity.Profiling.ProfilerMarker(Unity.Profiling.ProfilerCategory.Scripts, $"{m_Name.Name}.InitializeAsync");
                m_StartPreMarker = new Unity.Profiling.ProfilerMarker(Unity.Profiling.ProfilerCategory.Scripts, $"{m_Name.Name}.StartPresentation");

                m_BeforePreMarker = new Unity.Profiling.ProfilerMarker(Unity.Profiling.ProfilerCategory.Scripts, $"{m_Name.Name}.BeforePresentation");
                m_OnPreMarker = new Unity.Profiling.ProfilerMarker(Unity.Profiling.ProfilerCategory.Scripts, $"{m_Name.Name}.OnPresentation");
                m_AfterPreMarker = new Unity.Profiling.ProfilerMarker(Unity.Profiling.ProfilerCategory.Scripts, $"{m_Name.Name}.AfterPresentation");

                m_BeforePreSystemMarkers = new List<Unity.Profiling.ProfilerMarker>();
                m_OnPreSystemMarkers = new List<Unity.Profiling.ProfilerMarker>();
                m_AfterPreSystemMarkers = new List<Unity.Profiling.ProfilerMarker>();
#endif
            }

            public sealed class YieldAwaiter : ICustomYieldAwaiter
            {
                public Func<bool> m_Predicate;
                public bool KeepWait => !m_Predicate.Invoke();
            }
            public bool HasSystem<T>(T system) where T : PresentationSystemEntity
                => m_Systems.FindFor((other) => other.Equals(system)) != null;
            public bool TryGetSystem(Type type, out PresentationSystemEntity system)
            {
                for (int i = 0; i < m_Systems.Count; i++)
                {
                    if (type.IsAssignableFrom(m_Systems[i].GetType()))
                    {
                        system = m_Systems[i];
                        return true;
                    }
                }
                system = null;
                return false;
            }
            public bool TryGetSystem<T>(out T system) where T : PresentationSystemEntity
            {
                if (!TryGetSystem(TypeHelper.TypeOf<T>.Type, out PresentationSystemEntity temp))
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    m_BeforePreSystemMarkers.Add(new Unity.Profiling.ProfilerMarker(Unity.Profiling.ProfilerCategory.Scripts, $"{system.GetType().Name}"));
#endif
                }
                if (system.EnableOnPresentation)
                {
                    m_OnPresentations.Add(system);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    m_OnPreSystemMarkers.Add(new Unity.Profiling.ProfilerMarker(Unity.Profiling.ProfilerCategory.Scripts, $"{system.GetType().Name}"));
#endif
                }
                if (system.EnableAfterPresentation)
                {
                    m_AfterPresentations.Add(system);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    m_AfterPreSystemMarkers.Add(new Unity.Profiling.ProfilerMarker(Unity.Profiling.ProfilerCategory.Scripts, $"{system.GetType().Name}"));
#endif
                }
            }
            public void Reset()
            {
                m_IsStarted = false;

                for (int i = 0; i < m_Systems.Count; i++)
                {
                    m_Systems[i].Dispose();
                }
                m_Systems.Clear();
                m_Initializers.Clear();
                m_BeforePresentations.Clear();
                m_OnPresentations.Clear();
                m_AfterPresentations.Clear();

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
                if (pos == 0) m_BeforePresentationJobHandle = JobHandle.CombineDependencies(m_BeforePresentationJobHandle, jobHandle);
                else if (pos == 1) m_OnPresentationJobHandle = JobHandle.CombineDependencies(m_OnPresentationJobHandle, jobHandle);
                else m_AfterPresentationJobHandle = JobHandle.CombineDependencies(m_AfterPresentationJobHandle, jobHandle);
            }

            #endregion

            #region Presentation Methods

            public void Initialize()
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                m_InitializeMarker.Begin();
#endif
                for (int i = 0; i < m_Initializers.Count; i++)
                {
                    PresentationResult result = m_Initializers[i].OnInitialize();
                    LogMessage(result);
                }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                m_InitializeMarker.End();
#endif
            }
            public void InitializeAsync()
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                m_InitializeAsyncMarker.Begin();
#endif
                for (int i = 0; i < m_Initializers.Count; i++)
                {
                    PresentationResult result = m_Initializers[i].OnInitializeAsync();
                    LogMessage(result);
                }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                m_InitializeAsyncMarker.End();
#endif
            }
            public void OnStartPresentation()
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                m_StartPreMarker.Begin();
#endif
                for (int i = 0; i < m_Initializers.Count; i++)
                {
                    m_Initializers[i].OnStartPresentation();
                }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                m_StartPreMarker.End();
#endif
            }
            public void BeforePresentation()
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                m_BeforePreMarker.Begin();
#endif
                m_MainthreadBeforePre = false;

                // Unity Jobs
                m_BeforePresentationJobHandle.Complete();

                for (int i = 0; i < m_BeforePresentations.Count; i++)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    m_BeforePreSystemMarkers[i].Begin();
#endif
                    PresentationResult result = m_BeforePresentations[i].BeforePresentation();
                    LogMessage(result);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    m_BeforePreSystemMarkers[i].End();
#endif
                }

                m_MainthreadBeforePre = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                m_BeforePreMarker.End();
#endif
            }
            public void BeforePresentationAsync()
            {
                for (int i = 0; i < m_BeforePresentations.Count; i++)
                {
                    PresentationResult result = m_BeforePresentations[i].BeforePresentationAsync();
                    LogMessage(result);
                }
            }
            public void OnPresentation()
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                m_OnPreMarker.Begin();
#endif
                m_MainthreadOnPre = false;

                // Unity Jobs
                m_OnPresentationJobHandle.Complete();

                for (int i = 0; i < m_OnPresentations.Count; i++)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    m_OnPreSystemMarkers[i].Begin();
#endif
                    PresentationResult result = m_OnPresentations[i].OnPresentation();
                    LogMessage(result);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    m_OnPreSystemMarkers[i].End();
#endif
                }

                m_MainthreadOnPre = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                m_OnPreMarker.End();
#endif
            }
            public void OnPresentationAsync()
            {
                for (int i = 0; i < m_OnPresentations.Count; i++)
                {
                    PresentationResult result = m_OnPresentations[i].OnPresentationAsync();
                    LogMessage(result);
                }
            }
            public void AfterPresentation()
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                m_AfterPreMarker.Begin();
#endif
                m_MainthreadAfterPre = false;

                // Unity Jobs
                m_AfterPresentationJobHandle.Complete();

                for (int i = 0; i < m_AfterPresentations.Count; i++)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    m_AfterPreSystemMarkers[i].Begin();
#endif
                    PresentationResult result = m_AfterPresentations[i].AfterPresentation();
                    LogMessage(result);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    m_AfterPreSystemMarkers[i].End();
#endif
                }

                m_MainthreadAfterPre = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                m_AfterPreMarker.End();
#endif
            }
            public void AfterPresentationAsync()
            {
                for (int i = 0; i < m_AfterPresentations.Count; i++)
                {
                    PresentationResult result = m_AfterPresentations[i].AfterPresentationAsync();
                    LogMessage(result);
                }
            }

            #endregion
        }
        private readonly Hash m_DefaultGroupHash = GroupToHash(TypeHelper.TypeOf<DefaultPresentationGroup>.Type);

        internal readonly Dictionary<Hash, Group> m_PresentationGroups = new Dictionary<Hash, Group>();
        internal readonly Dictionary<Type, Hash> m_RegisteredGroup = new Dictionary<Type, Hash>();
        internal readonly Dictionary<string, List<Hash>> m_DependenceSceneList = new Dictionary<string, List<Hash>>();

        private Thread m_PresentationThread;

        public override void OnInitialize()
        {
            const string register = "Register";

            SetPlayerLoop();

            Type[] registers = TypeHelper.GetTypes(t => !t.IsAbstract && t.GetInterfaces().FindFor(ta => ta.Equals(TypeHelper.TypeOf<IPresentationRegister>.Type)) != null).ToArray();

            MethodInfo registerMethod = TypeHelper.TypeOf<IPresentationRegister>.Type.GetMethod(register);
            IPresentationRegister[] presentations = new IPresentationRegister[registers.Length];
            for (int i = 0; i < registers.Length; i++)
            {
                presentations[i] = (IPresentationRegister)Activator.CreateInstance(registers[i]);
                registerMethod.Invoke(presentations[i], null);
            }

            m_BeforeUpdateAsyncSemaphore = new ManualResetEvent(true);
            m_OnUpdateAsyncSemaphore = new ManualResetEvent(true);
            m_AfterUpdateAsyncSemaphore = new ManualResetEvent(true);

            StartPresentation(m_DefaultGroupHash);
            for (int i = 0; i < presentations.Length; i++)
            {
                if (presentations[i].StartOnInitialize)
                {
                    StartPresentation(GroupToHash(registers[i]));
                }
                else if (presentations[i].DependenceGroup != null)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    if (presentations[i].DependenceGroup.IsAbstract ||
                        presentations[i].DependenceGroup.IsInterface)
                    {
                        CoreSystem.Logger.LogError(Channel.Presentation,
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

                    group.AddGroupDependence(GroupToHash(presentations[i].DependenceGroup));
                }
            }
            
            StartUnityUpdate(ThreadStart());
        }
        private IEnumerator ThreadStart()
        {
            m_PresentationThread = new Thread(PresentationAsyncUpdate)
            {
                IsBackground = true
            };
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

                    item.Value.Reset();
                }
            }

            PlayerLoopSystem defaultLoop = PlayerLoop.GetDefaultPlayerLoop();
            PlayerLoop.SetPlayerLoop(defaultLoop);

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

        private void PresentationPreUpdate()
        {
            PreUpdate?.Invoke();
        }

        private void PresentationBeforeUpdate()
        {
            m_BeforeUpdateAsyncSemaphore.Set();
            BeforeUpdate?.Invoke();
        }
        private void PresentationOnUpdate()
        {
            m_OnUpdateAsyncSemaphore.Set();
            Update?.Invoke();
        }
        private void PresentationAfterUpdate()
        {
            m_AfterUpdateAsyncSemaphore.Set();
            AfterUpdate?.Invoke();
        }

        private void PresentationLateTransformUpdate()
        {
            TransformUpdate?.Invoke();
        }
        private void PresentationLateAfterTransformUpdate()
        {
            AfterTransformUpdate?.Invoke();
        }

        private void PresentationPostUpdate()
        {
            PostUpdate?.Invoke();
        }

        private ManualResetEvent
            m_BeforeUpdateAsyncSemaphore,
            m_OnUpdateAsyncSemaphore,
            m_AfterUpdateAsyncSemaphore;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static Unity.Profiling.ProfilerMarker
            simSemaphoreMarker = new Unity.Profiling.ProfilerMarker(Unity.Profiling.ProfilerCategory.Internal, "Semaphore.WaitOne (Simulation)"),

            beforeSemaphoreMarker = new Unity.Profiling.ProfilerMarker(Unity.Profiling.ProfilerCategory.Internal, "Semaphore.WaitOne (BeforeUpdate)"),
            onSemaphoreMarker = new Unity.Profiling.ProfilerMarker(Unity.Profiling.ProfilerCategory.Internal, "Semaphore.WaitOne (OnUpdate)"),
            afterSemaphoreMarker = new Unity.Profiling.ProfilerMarker(Unity.Profiling.ProfilerCategory.Internal, "Semaphore.WaitOne (AfterUpdate)");

        private static UnityEngine.Profiling.CustomSampler
            PresentationUpdateMarker = UnityEngine.Profiling.CustomSampler.Create("PresentationAsyncUpdate"),

            beforeUpdateMarker = UnityEngine.Profiling.CustomSampler.Create("BeforeUpdate"),
            onUpdateMarker = UnityEngine.Profiling.CustomSampler.Create("OnUpdate"),
            afterUpdateMarker = UnityEngine.Profiling.CustomSampler.Create("AfterUpdate");
#endif
        private static void PresentationAsyncUpdate(object obj)
        {
            PresentationManager mgr = (PresentationManager)obj;
            Thread.CurrentThread.CurrentCulture = global::System.Globalization.CultureInfo.InvariantCulture;

            CoreSystem.SimulateWatcher.WaitOne();

            while (!CoreSystem.BlockCreateInstance)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (CoreSystem.IsEditorPaused) continue;

                UnityEngine.Profiling.Profiler.BeginThreadProfiling("Syadeu", "CoreSystem.Presentation");
                PresentationUpdateMarker.Begin();

                using (simSemaphoreMarker.Auto())
#endif
                {
                    while (!CoreSystem.BlockCreateInstance)
                    {
                        if (CoreSystem.SimulateWatcher.WaitOne(100)) break;
                    }
                    if (CoreSystem.BlockCreateInstance) break;
                }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                beforeSemaphoreMarker.Begin();
#endif
                while (!CoreSystem.BlockCreateInstance)
                {
                    if (mgr.m_BeforeUpdateAsyncSemaphore.WaitOne(1)) break;
                }
                if (CoreSystem.BlockCreateInstance) break;
                
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                beforeSemaphoreMarker.End();
                beforeUpdateMarker.Begin();
#endif
                {
                    mgr.BeforeUpdateAsync?.Invoke();
                    mgr.m_BeforeUpdateAsyncSemaphore.Reset();
                    //for (int i = 0; i < 100000; i++)
                    //{
                    //    Math.Sqrt(2.5f);
                    //}
                    //UnityEngine.Debug.Log("before");
                }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                beforeUpdateMarker.End();
                onSemaphoreMarker.Begin();
#endif
                while (!CoreSystem.BlockCreateInstance)
                {
                    if (mgr.m_OnUpdateAsyncSemaphore.WaitOne(1)) break;
                }
                if (CoreSystem.BlockCreateInstance) break;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                onSemaphoreMarker.End();
                onUpdateMarker.Begin();
#endif
                mgr.UpdateAsync?.Invoke();
                mgr.m_OnUpdateAsyncSemaphore.Reset();
                //for (int i = 0; i < 100000; i++)
                //{
                //    Math.Sqrt(2.5f);
                //}
                //UnityEngine.Debug.Log("on");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                onUpdateMarker.End();
                afterSemaphoreMarker.Begin();
#endif
                while (!CoreSystem.BlockCreateInstance)
                {
                    if (mgr.m_AfterUpdateAsyncSemaphore.WaitOne(1)) break;
                }
                if (CoreSystem.BlockCreateInstance) break;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                afterSemaphoreMarker.End();
                afterUpdateMarker.Begin();
#endif
                mgr.AfterUpdateAsync?.Invoke();
                mgr.m_AfterUpdateAsyncSemaphore.Reset();
                //for (int i = 0; i < 100000; i++)
                //{
                //    Math.Sqrt(2.5f);
                //}
                //UnityEngine.Debug.Log("after");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                afterUpdateMarker.End();
                PresentationUpdateMarker.End();
                UnityEngine.Profiling.Profiler.EndThreadProfiling();
#endif
            }

            mgr.m_BeforeUpdateAsyncSemaphore.Dispose();
            mgr.m_OnUpdateAsyncSemaphore.Dispose();
            mgr.m_AfterUpdateAsyncSemaphore.Dispose();

            UnityEngine.Debug.Log("thread out");
        }

        #endregion

        #region Internals

        private static Hash GroupToHash(Type group)
        {
            return Hash.NewHash(group.Name);
        }

        internal static void RegisterSystem(Type groupName, SceneReference dependenceScene, params Type[] systems)
        {
            if (dependenceScene != null)
            {
                CoreSystem.Logger.Log(Channel.Presentation, $"Registration start ({groupName.Name.Split('.').Last()}), number of {systems.Length}, has dependece scene ({dependenceScene.ScenePath})");
            }
            else CoreSystem.Logger.Log(Channel.Presentation, $"Registration start ({groupName.Name.Split('.').Last()}), number of {systems.Length}");

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

                if (list.Contains(groupHash)) CoreSystem.Logger.LogError(Channel.Presentation, $"{groupName.Name.Split('.').Last()} 은 이미 해당 씬({dependenceScene.ScenePath})에 종속되었습니다. 중복 추가는 허용하지 않습니다.");
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

                if (Instance.m_RegisteredGroup.ContainsKey(systems[i]))
                {
                    CoreSystem.Logger.LogWarning(Channel.Presentation,
                        $"Multiple system({TypeHelper.ToString(systems[i])}, in group {TypeHelper.ToString(groupName)}) detected. " +
                        $"This instance will not gathered by PresentationSystem<T> but PresentationSystemGroup<T>.Systems.");
                }
                else
                {
                    Instance.m_RegisteredGroup.Add(systems[i], groupHash);
                }
                
                CoreSystem.Logger.Log(Channel.Presentation, $"System ({groupName.Name.Split('.').Last()}): {systems[i].Name} Registered");
            }

            group.m_RegisteredSystemTypes = registedTypes.ToArray();
            group.m_RequireSystemTypes = registedTypes.Select((other) => other.GetCustomAttribute<SubSystemAttribute>()).ToArray();

            CoreSystem.Logger.Log(Channel.Presentation, $"Registration Ended ({groupName.Name.Split('.').Last()}), number of {systems.Length}");
        }
        internal ICustomYieldAwaiter StartPresentation(Hash groupHash)
        {
            Group group = m_PresentationGroups[groupHash];
            if (group.m_IsStarted)
            {
                CoreSystem.Logger.LogWarning(Channel.Presentation,
                    $"Presentation Group {group.m_Name.Name} has already started and running. Request ignored.");
                return null;
            }

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

                group.Add(system);
            }

            //group.MainPresentation = Instance.StartUnityUpdate(Presentation(group));
            Instance.StartUnityUpdate(Presentation(group));
            group.BackgroundPresentation = Instance.StartBackgroundUpdate(PresentationAsync(group));
            group.m_IsStarted = true;

            CoreSystem.Logger.Log(Channel.Presentation, $"{group.m_Name.Name} group is started");

            List<Hash> connectedGroups = group.GetGroupDependences();
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
                CoreSystem.Logger.LogWarning(Channel.Presentation, 
                    $"Presentation Group {group.m_Name.Name} has already stopped. Request ignored.");
                return;
            }

            Instance.BeforeUpdate -= group.BeforePresentation;
            Instance.Update -= group.OnPresentation;
            Instance.AfterUpdate -= group.AfterPresentation;

            //Instance.StopUnityUpdate(group.MainPresentation);
            //Instance.StopBackgroundUpdate(group.BackgroundPresentation);
            Instance.BeforeUpdateAsync -= group.BeforePresentationAsync;
            Instance.UpdateAsync -= group.OnPresentationAsync;
            Instance.AfterUpdateAsync -= group.AfterPresentationAsync;

            group.Reset();

            CoreSystem.Logger.Log(Channel.Presentation, $"{group.m_Name.Name} group is stopped");
        }

        [Obsolete]
        internal static void RegisterRequestSystem<T, TA>(Action<TA> setter) 
            where T : PresentationSystemEntity
            where TA : PresentationSystemEntity
        {
            if (!Instance.m_RegisteredGroup.TryGetValue(TypeHelper.TypeOf<T>.Type, out Hash groupHash) ||
                !Instance.m_PresentationGroups.TryGetValue(groupHash, out Group group))
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Presentation,
                    $"시스템 {typeof(T).Name} 은 등록되지 않았습니다.");
            }

            group.m_RequestSystemDelegates.Enqueue(() =>
            {
                TA system = PresentationSystem<TA>.System;
                if (system == null)
                {
                    CoreSystem.Logger.LogError(Channel.Presentation, $"Requested system ({TypeHelper.TypeOf<TA>.Name}) not found");
                }
                else CoreSystem.Logger.Log(Channel.Presentation, $"Requested system ({TypeHelper.TypeOf<TA>.Name}) found");

                setter.Invoke(system);
            });
            //"request in".ToLog();
        }
        internal static void RegisterRequest<TGroup, TSystem>(Action<TSystem> setter)
            where TGroup : PresentationGroupEntity
            where TSystem : PresentationSystemEntity
        {
            Hash groupHash = GroupToHash(TypeHelper.TypeOf<TGroup>.Type);

            if (!Instance.m_PresentationGroups.TryGetValue(groupHash, out Group group))
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Presentation,
                    $"시스템 그룹 {typeof(TGroup).Name} 은 등록되지 않았습니다.");
            }

            if (group.m_IsStarted && !group.m_MainthreadSignal)
            {
                group.m_RequestSystemDelegates.Enqueue(() =>
                {
                    TSystem system = PresentationSystem<TSystem>.System;
                    if (system == null)
                    {
                        CoreSystem.Logger.LogError(Channel.Presentation, $"Requested system ({TypeHelper.TypeOf<TSystem>.Name}) not found");
                    }
                    else CoreSystem.Logger.Log(Channel.Presentation, $"Requested system ({TypeHelper.TypeOf<TSystem>.Name}) found");

                    setter.Invoke(system);
                });
                return;
            }

            if (group.TryGetSystem<TSystem>(out TSystem system))
            {
                setter.Invoke(system);
                return;
            }

            throw new Exception("unknown");
        }

        #endregion

        #region Presentation Group Method
        private static void LogMessage(PresentationResult result)
        {
            if (result.m_Result == ResultFlag.Normal) return;

            if (result.m_Result == ResultFlag.Warning) CoreSystem.Logger.LogWarning(Channel.Presentation, result.m_Message);
            else if (result.m_Result == ResultFlag.Error) CoreSystem.Logger.LogError(Channel.Presentation, result.m_Message);
        }
        private static IEnumerator Presentation(Group group)
        {
            group.Initialize();
            group.m_MainthreadSignal = true;

            float dateTime = Time.realtimeSinceStartup;
            for (int i = 0; i < group.m_RequireSystemTypes.Length; i++)
            {
                if (group.m_RequireSystemTypes[i] == null) continue;

                if (!Instance.m_RegisteredGroup.TryGetValue(group.m_RequireSystemTypes[i].m_Target, out Hash groupHash) ||
                        !Instance.m_PresentationGroups.TryGetValue(groupHash, out Group targetGroup))
                {
                    CoreSystem.Logger.LogError(Channel.Presentation,
                        $"{group.m_RequireSystemTypes[i].m_Target.Name} is not registered. Request ignored.");
                    continue;
                }

                if (targetGroup.Equals(group))
                {
                    CoreSystem.Logger.LogError(Channel.Presentation,
                        $"This system({group.m_RegisteredSystemTypes[i].Name}) declared sub system of it\'s own system group. This is not allowed.");
                    continue;
                }

                dateTime = Time.realtimeSinceStartup;
                while (!targetGroup.m_MainInitDone)
                {
                    if (dateTime + 10 < Time.realtimeSinceStartup)
                    {
                        CoreSystem.Logger.LogWarning(Channel.Presentation,
                            $"The system group({group.m_Name.Name}) is awaiting too long because waiting system initialize target ({group.m_RequireSystemTypes[i].m_Target.Name})");

                        dateTime = Time.realtimeSinceStartup;
                    }
                    yield return null;
                }
            }

            for (int i = 0; i < group.Count; i++)
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
                    CoreSystem.Logger.LogWarning(Channel.Presentation,
                        $"The system({group.m_Name.Name}) is awaiting too long. Maybe unexpected handles?");

                    dateTime = Time.realtimeSinceStartup;
                }
                return group.m_BackgroundthreadSignal;
            });

            group.OnStartPresentation();
            
            group.m_MainInitDone = true;

            yield return group.m_WaitUntilInitializeCompleted;
            CoreSystem.Logger.Log(Channel.Presentation, $"Presentation group ({group.m_Name.Name}) started");

            Instance.BeforeUpdate += group.BeforePresentation;
            Instance.Update += group.OnPresentation;
            Instance.AfterUpdate += group.AfterPresentation;
        }
        private static IEnumerator PresentationAsync(Group group)
        {
            #region Initialize

            group.InitializeAsync();

            yield return new WaitUntil(() => group.m_MainthreadSignal);
            int requestSystemCount = group.m_RequestSystemDelegates.Count;
            for (int i = 0; i < requestSystemCount; i++)
            {
                //$"asd : {i} = {requestSystemCount}".ToLog();
                if (!group.m_RequestSystemDelegates.TryDequeue(out Action action)) continue;
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

            //while (true)
            //{
            //    group.m_BackgroundthreadBeforePre = false;

            //    group.BeforePresentationAsync();

            //    group.m_BackgroundthreadBeforePre = true;
            //    yield return group.m_WaitBeforePre;
            //    group.m_BackgroundthreadOnPre = false;

            //    group.OnPresentationAsync();

            //    group.m_BackgroundthreadOnPre = true;
            //    yield return group.m_WaitOnPre;
            //    group.m_BackgroundthreadAfterPre = false;

            //    group.AfterPresentationAsync();

            //    group.m_BackgroundthreadAfterPre = true;
            //    yield return group.m_WaitAfterPre;

            //    yield return null;
            //}
        }
        #endregion
    }
}
