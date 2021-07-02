//#undef UNITY_ADDRESSABLES


#if UNITY_EDITOR
#endif

#if UNITY_ADDRESSABLES
#endif

using Syadeu.Database;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Presentation
{
    public sealed class PresentationManager : StaticDataManager<PresentationManager>
    {
        private readonly List<IPresentationSystem> m_Systems = new List<IPresentationSystem>();

        private readonly List<IInitPresentation> m_Initialzers = new List<IInitPresentation>();
        private readonly List<IBeforePresentation> m_BeforePresentations = new List<IBeforePresentation>();
        private readonly List<IOnPresentation> m_OnPresentations = new List<IOnPresentation>();
        private readonly List<IAfterPresentation> m_AfterPresentations = new List<IAfterPresentation>();

        private readonly ConcurrentQueue<Action> m_RequestSystemDelegates = new ConcurrentQueue<Action>();

        private bool m_PresentationStarted = false;
        private bool m_MainthreadSignal = false;
        private bool m_BackgroundthreadSignal = false;

        public static void RegisterSystem<T>(params T[] systems) where T : IPresentationSystem
        {
            for (int i = 0; i < systems.Length; i++)
            {
                if (Instance.m_Systems.Contains(systems[i]))
                {
                    throw new Exception();
                }

                Instance.m_Systems.Add(systems[i]);

                Instance.m_Initialzers.Add(systems[i]);
                if (systems[i].EnableBeforePresentation) Instance.m_BeforePresentations.Add(systems[i]);
                if (systems[i].EnableOnPresentation) Instance.m_OnPresentations.Add(systems[i]);
                if (systems[i].EnableAfterPresentation) Instance.m_AfterPresentations.Add(systems[i]);

                $"System: {systems[i].GetType().Name} Registered".ToLog();
            }
        }
        public static void StartPresentation()
        {
            if (Instance.m_PresentationStarted) return;

            Instance.StartUnityUpdate(Instance.Presentation());
            Instance.StartBackgroundUpdate(Instance.PresentationAsync());

            Instance.m_PresentationStarted = true;
        }

        public static T GetSystem<T>() where T : class, IPresentationSystem
        {
            IPresentationSystem system = m_Instance.m_Systems.FindFor((other) => other.GetType().Equals(typeof(T)));
            if (system == null) return null;
            return (T)system;
        }
        internal static void RegisterRequestSystem<T>(Action<T> setter) where T : class, IPresentationSystem
        {
            Instance.m_RequestSystemDelegates.Enqueue(() =>
            {
                T system = GetSystem<T>();
                if (system == null)
                {
                    $"Requested system ({typeof(T).Name}) not found".ToLogError();
                }
                else $"Requested system ({typeof(T).Name}) found".ToLog();

                setter.Invoke(system);
            });
        }

        private IEnumerator Presentation()
        {
            for (int i = 0; i < m_Initialzers.Count; i++)
            {
                m_Initialzers[i].OnInitialize();
            }
            yield return new WaitUntil(() => m_BackgroundthreadSignal);

            $"main pre in".ToLog();

            m_MainthreadSignal = true;
            $"{Instance.m_BeforePresentations.Count} : {Instance.m_OnPresentations.Count} : {Instance.m_AfterPresentations.Count}".ToLog();
            while (true)
            {
                for (int i = 0; i < m_BeforePresentations.Count; i++)
                {
                    m_BeforePresentations[i].BeforePresentation();
                }
                for (int i = 0; i < m_OnPresentations.Count; i++)
                {
                    m_OnPresentations[i].OnPresentation();
                }
                for (int i = 0; i < m_AfterPresentations.Count; i++)
                {
                    m_AfterPresentations[i].AfterPresentation();
                }

                yield return null;
            }
        }
        private IEnumerator PresentationAsync()
        {
            for (int i = 0; i < m_Initialzers.Count; i++)
            {
                m_Initialzers[i].OnInitializeAsync();
            }

            m_BackgroundthreadSignal = true;
            int requestSystemCount = m_RequestSystemDelegates.Count;
            for (int i = 0; i < requestSystemCount; i++)
            {
                if (!m_RequestSystemDelegates.TryDequeue(out Action action)) continue;
                action.Invoke();
            }

            yield return new WaitUntil(() => m_MainthreadSignal);

            $"back pre in".ToLog();

            while (true)
            {
                for (int i = 0; i < m_BeforePresentations.Count; i++)
                {
                    m_BeforePresentations[i].BeforePresentationAsync();
                }
                for (int i = 0; i < m_OnPresentations.Count; i++)
                {
                    m_OnPresentations[i].OnPresentationAsync();
                }
                for (int i = 0; i < m_AfterPresentations.Count; i++)
                {
                    m_AfterPresentations[i].AfterPresentationAsync();
                }

                yield return null;
            }
        }
    }

    [StaticManagerIntializeOnLoad]
    internal sealed class CSPresentationInterface : StaticDataManager<CSPresentationInterface>
    {
        public override void OnInitialize()
        {
            PresentationManager.RegisterSystem(new TestSystem());
            PresentationManager.RegisterSystem(new Test123System());

            
        }
        public override void OnStart()
        {
            PresentationManager.StartPresentation();
        }
    }

    public struct PresentationResult
    {
        public static PresentationResult Normal = new PresentationResult(ResultFlag.Normal, string.Empty);

        internal ResultFlag m_Result;
        internal Exception m_Exception;
        internal string m_Message;

        public PresentationResult(ResultFlag flag, string msg)
        {
            m_Result = flag;
            m_Exception = null;
            m_Message = msg;
        }
        public PresentationResult(Exception ex, ResultFlag flag = ResultFlag.Error)
        {
            m_Result = flag;
            m_Exception = ex;
            m_Message = string.Empty;
        }
    }

    public sealed class TestSystem : PresentationSystemEntity<TestSystem>
    {
        Test123System testsystem;

        public override PresentationResult OnInitialize()
        {
            RequestSystem<Test123System>((other) => testsystem = other);

            return base.OnInitialize();
        }

        public override PresentationResult OnPresentation()
        {
            //$"123123 system == null = {testsystem == null}".ToLog();

            return base.OnPresentation();
        }
    }
    public sealed class Test123System : PresentationSystemEntity<Test123System>
    {
        TestSystem testSystem;

        public override PresentationResult OnInitialize()
        {
            RequestSystem<TestSystem>((other) => testSystem = other);

            return base.OnInitialize();
        }

        public override PresentationResult OnPresentation()
        {
            //$"system == null = {testSystem == null}".ToLog();

            return base.OnPresentation();
        }
    }

    public abstract class PresentationSystemEntity<T> : IPresentationSystem, IDisposable where T : class
    {
        public virtual bool EnableBeforePresentation => true;
        public virtual bool EnableOnPresentation => true;
        public virtual bool EnableAfterPresentation => true;

        ~PresentationSystemEntity()
        {
            Dispose();
        }

        public virtual PresentationResult OnInitialize() { return PresentationResult.Normal; }
        public virtual PresentationResult OnInitializeAsync() { return PresentationResult.Normal; }

        public virtual PresentationResult BeforePresentation() { return PresentationResult.Normal; }
        public virtual PresentationResult BeforePresentationAsync() { return PresentationResult.Normal; }

        public virtual PresentationResult OnPresentation() { return PresentationResult.Normal; }
        public virtual PresentationResult OnPresentationAsync() { return PresentationResult.Normal; }

        public virtual PresentationResult AfterPresentation() { return PresentationResult.Normal; }
        public virtual PresentationResult AfterPresentationAsync() { return PresentationResult.Normal; }

        public virtual void Dispose() { }

        protected void RequestSystem<TA>(Action<TA> setter) where TA : class, IPresentationSystem
            => PresentationManager.RegisterRequestSystem(setter);
    }

    public interface IPresentationSystem : IInitPresentation, IBeforePresentation, IOnPresentation, IAfterPresentation
    {
        bool EnableBeforePresentation { get; }
        bool EnableOnPresentation { get; }
        bool EnableAfterPresentation { get; }
    }
    public interface IInitPresentation
    {
        PresentationResult OnInitialize();
        PresentationResult OnInitializeAsync();
    }
    public interface IBeforePresentation
    {
        PresentationResult BeforePresentation();
        PresentationResult BeforePresentationAsync();
    }
    public interface IOnPresentation
    {
        PresentationResult OnPresentation();
        PresentationResult OnPresentationAsync();
    }
    public interface IAfterPresentation
    {
        PresentationResult AfterPresentation();
        PresentationResult AfterPresentationAsync();
    }
}
