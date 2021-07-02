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
using System.Reflection;
using UnityEngine;

namespace Syadeu.Presentation
{
    [StaticManagerIntializeOnLoad]
    public sealed class PresentationManager : StaticDataManager<PresentationManager>
    {
        internal class PresentationGroup
        {
            public string m_Name;
            public Hash m_Hash;

            public readonly List<IPresentationSystem> m_Systems = new List<IPresentationSystem>();
            public readonly List<IInitPresentation> m_Initializers = new List<IInitPresentation>();
            public readonly List<IBeforePresentation> m_BeforePresentations = new List<IBeforePresentation>();
            public readonly List<IOnPresentation> m_OnPresentations = new List<IOnPresentation>();
            public readonly List<IAfterPresentation> m_AfterPresentations = new List<IAfterPresentation>();

            public readonly ConcurrentQueue<Action> m_RequestSystemDelegates = new ConcurrentQueue<Action>();

            public CoreRoutine MainPresentation;
            public CoreRoutine BackgroundPresentation;

            public bool m_MainthreadSignal = false;
            public bool m_BackgroundthreadSignal = false;
            
            public bool m_MainInitDone = false;
            public bool m_BackgroundInitDone = false;
            public WaitUntil m_WaitUntilInitializeCompleted;

            public PresentationGroup(string name, Hash hash)
            {
                m_Name = name;
                m_Hash = hash;

                m_WaitUntilInitializeCompleted = new WaitUntil(() => m_MainInitDone && m_BackgroundInitDone);
            }

            public bool HasSystem<T>(T system) where T : IPresentationSystem
                => m_Systems.FindFor((other) => other.Equals(system)) != null;
        }
        private Hash m_DefaultGroupHash = Hash.NewHash("DefaultSystemGroup");

        internal readonly Dictionary<Hash, PresentationGroup> m_PresentationGroups = new Dictionary<Hash, PresentationGroup>();
        internal readonly Dictionary<Type, Hash> m_RegisteredGroup = new Dictionary<Type, Hash>();

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

            StartPresentation();
        }

        public static void RegisterSystem<T>(params T[] systems) where T : IPresentationSystem => RegisterSystem("DefaultSystemGroup", systems);
        public static void RegisterSystem<T>(string groupName, params T[] systems) where T : IPresentationSystem
        {
            Hash groupHash = Hash.NewHash(groupName);
            if (!Instance.m_PresentationGroups.TryGetValue(groupHash, out PresentationGroup group))
            {
                group = new PresentationGroup(groupName, groupHash);
                Instance.m_PresentationGroups.Add(groupHash, group);
            }

            for (int i = 0; i < systems.Length; i++)
            {
                if (group.m_Systems.Contains(systems[i]))
                {
                    throw new Exception();
                }

                group.m_Systems.Add(systems[i]);

                group.m_Initializers.Add(systems[i]);
                if (systems[i].EnableBeforePresentation) group.m_BeforePresentations.Add(systems[i]);
                if (systems[i].EnableOnPresentation) group.m_OnPresentations.Add(systems[i]);
                if (systems[i].EnableAfterPresentation) group.m_AfterPresentations.Add(systems[i]);

                Instance.m_RegisteredGroup.Add(systems[i].GetType(), groupHash);
                $"System ({groupName}): {systems[i].GetType().Name} Registered".ToLog();
            }
        }
        private void StartPresentation()
        {
            m_PresentationGroups[m_DefaultGroupHash].MainPresentation 
                = Instance.StartUnityUpdate(Presentation(m_PresentationGroups[m_DefaultGroupHash]));
            m_PresentationGroups[m_DefaultGroupHash].BackgroundPresentation 
                = Instance.StartBackgroundUpdate(PresentationAsync(m_PresentationGroups[m_DefaultGroupHash]));
        }

        internal static void RegisterRequestSystem<T, TA>(Action<TA> setter) where TA : class, IPresentationSystem
        {
            if (!Instance.m_RegisteredGroup.TryGetValue(typeof(T), out Hash groupHash))
            {
                throw new Exception();
            }
            if (!Instance.m_PresentationGroups.TryGetValue(groupHash, out PresentationGroup group))
            {
                throw new Exception();
            }

            group.m_RequestSystemDelegates.Enqueue(() =>
            {
                TA system = PresentationSystem<TA>.GetSystem();
                if (system == null)
                {
                    $"Requested system ({typeof(TA).Name}) not found".ToLogError();
                }
                else $"Requested system ({typeof(TA).Name}) found".ToLog();

                setter.Invoke(system);
            });
            "request in".ToLog();
        }

        private static IEnumerator Presentation(PresentationGroup group)
        {
            for (int i = 0; i < group.m_Initializers.Count; i++)
            {
                group.m_Initializers[i].OnInitialize();
            }
            yield return new WaitUntil(() => group.m_BackgroundthreadSignal);

            //$"main pre in".ToLog();

            for (int i = 0; i < group.m_Systems.Count; i++)
            {
                while (!group.m_Systems[i].IsStartable)
                {
                    yield return null;
                }
            }

            for (int i = 0; i < group.m_Initializers.Count; i++)
            {
                group.m_Initializers[i].OnStartPresentation();
            }
            group.m_MainthreadSignal = true;
            //group.m_IsPresentationStarted = true;
            //OnPresentationStarted?.Invoke();
            group.m_MainInitDone = true;

            yield return group.m_WaitUntilInitializeCompleted;
            $"Presentation started".ToLog();
            while (true)
            {
                for (int i = 0; i < group.m_BeforePresentations.Count; i++)
                {
                    group.m_BeforePresentations[i].BeforePresentation();
                }
                for (int i = 0; i < group.m_OnPresentations.Count; i++)
                {
                    group.m_OnPresentations[i].OnPresentation();
                }
                for (int i = 0; i < group.m_AfterPresentations.Count; i++)
                {
                    group.m_AfterPresentations[i].AfterPresentation();
                }

                yield return null;
            }
        }
        private static IEnumerator PresentationAsync(PresentationGroup group)
        {
            for (int i = 0; i < group.m_Initializers.Count; i++)
            {
                group.m_Initializers[i].OnInitializeAsync();
            }

            group.m_BackgroundthreadSignal = true;
            int requestSystemCount = group.m_RequestSystemDelegates.Count;
            for (int i = 0; i < requestSystemCount; i++)
            {
                if (!group.m_RequestSystemDelegates.TryDequeue(out Action action)) continue;
                action.Invoke();
            }

            yield return new WaitUntil(() => group.m_MainthreadSignal);
            group.m_BackgroundInitDone = true;


            yield return group.m_WaitUntilInitializeCompleted;
            while (true)
            {
                for (int i = 0; i < group.m_BeforePresentations.Count; i++)
                {
                    group.m_BeforePresentations[i].BeforePresentationAsync();
                }
                for (int i = 0; i < group.m_OnPresentations.Count; i++)
                {
                    group.m_OnPresentations[i].OnPresentationAsync();
                }
                for (int i = 0; i < group.m_AfterPresentations.Count; i++)
                {
                    group.m_AfterPresentations[i].AfterPresentationAsync();
                }

                yield return null;
            }
        }
    }
}
