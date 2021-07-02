//#undef UNITY_ADDRESSABLES


#if UNITY_EDITOR
#endif

#if UNITY_ADDRESSABLES
#endif

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
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

        private bool m_IsPresentationStarted = false;

        private bool m_PresentationStartCalled = false;
        private bool m_MainthreadSignal = false;
        private bool m_BackgroundthreadSignal = false;

        public static event Action OnPresentationStarted;

        public static bool IsPresentationStarted => m_Instance != null && m_Instance.m_IsPresentationStarted;

        public override void OnInitialize()
        {
            const string instance = "Instance";
            const string register = "Register";

            List<Type> registers = new List<Type>();
            registers.AddRange(CoreSystem.GetInternalTypes((other) => other.GetInterfaces().FindFor(t => t.Equals(typeof(IPresentationRegister))) != null));
            registers.AddRange(CoreSystem.GetMainAssemblyTypes((other) => other.GetInterfaces().FindFor(t => t.Equals(typeof(IPresentationRegister))) != null));

            MethodInfo registerMethod = typeof(IPresentationRegister).GetMethod(register);

            for (int i = 0; i < registers.Count; i++)
            {
                object ins;
                PropertyInfo instanceProperty = registers[i].GetProperty(instance, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (instanceProperty != null)
                {
                    ins = instanceProperty.GetGetMethod().Invoke(null, null);
                }
                else ins = Activator.CreateInstance(registers[i]);

                registerMethod.Invoke(ins, null);
            }
        }

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
            if (Instance.m_PresentationStartCalled) return;

            Instance.StartUnityUpdate(Instance.Presentation());
            Instance.StartBackgroundUpdate(Instance.PresentationAsync());

            Instance.m_PresentationStartCalled = true;
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

            //$"main pre in".ToLog();

            m_MainthreadSignal = true;
            //$"{Instance.m_BeforePresentations.Count} : {Instance.m_OnPresentations.Count} : {Instance.m_AfterPresentations.Count}".ToLog();
            m_IsPresentationStarted = true;
            OnPresentationStarted?.Invoke();
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

            //$"back pre in".ToLog();

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

    //public sealed class TestSystem : PresentationSystemEntity<TestSystem>
    //{
    //    Test123System testsystem;

    //    public override bool EnableBeforePresentation => false;
    //    public override bool EnableOnPresentation => true;
    //    public override bool EnableAfterPresentation => false;

    //    public override PresentationResult OnInitialize()
    //    {
    //        RequestSystem<Test123System>((other) => testsystem = other);

    //        return base.OnInitialize();
    //    }

    //    public override PresentationResult OnPresentation()
    //    {
    //        //$"123123 system == null = {testsystem == null}".ToLog();

    //        return base.OnPresentation();
    //    }
    //}
    //public sealed class Test123System : PresentationSystemEntity<Test123System>
    //{
    //    TestSystem testSystem;

    //    public override bool EnableBeforePresentation => false;
    //    public override bool EnableOnPresentation => true;
    //    public override bool EnableAfterPresentation => false;

    //    public override PresentationResult OnInitialize()
    //    {
    //        RequestSystem<TestSystem>((other) => testSystem = other);

    //        return base.OnInitialize();
    //    }

    //    public override PresentationResult OnPresentation()
    //    {
    //        //$"system == null = {testSystem == null}".ToLog();

    //        return base.OnPresentation();
    //    }
    //}
}
