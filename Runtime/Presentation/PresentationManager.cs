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
using UnityEngine.Assertions;

namespace Syadeu.Presentation
{
    [StaticManagerIntializeOnLoad]
    public sealed class PresentationManager : StaticDataManager<PresentationManager>
    {
        const string instance = "Instance";

        internal class Group
        {
            public Type m_Name;
            public Hash m_Hash;
            public readonly List<Type> m_RegisteredSystemTypes = new List<Type>();
            public bool m_IsStarted = false;

            public IPresentationSystemGroup m_SystemGroup;

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

            public Group(Type name, Hash hash)
            {
                m_Name = name;
                m_Hash = hash;

                m_WaitUntilInitializeCompleted = new WaitUntil(() => m_MainInitDone && m_BackgroundInitDone);
            }

            public bool HasSystem<T>(T system) where T : IPresentationSystem
                => m_Systems.FindFor((other) => other.Equals(system)) != null;
        }
        private Hash m_DefaultGroupHash = Hash.NewHash(typeof(DefaultPresentationGroup).Name);

        internal readonly Dictionary<Hash, Group> m_PresentationGroups = new Dictionary<Hash, Group>();
        internal readonly Dictionary<Type, Hash> m_RegisteredGroup = new Dictionary<Type, Hash>();
        internal readonly Dictionary<string, List<Hash>> m_DependenceSceneList = new Dictionary<string, List<Hash>>();

        public override void OnInitialize()
        {
            const string register = "Register";

            List<Type> registers = new List<Type>();
            registers.AddRange(CoreSystem.GetInternalTypes(
                (other) => !other.IsAbstract && other.GetInterfaces().FindFor(t => t.Equals(typeof(IPresentationRegister))) != null)
                );
            registers.AddRange(CoreSystem.GetMainAssemblyTypes(
                (other) => !other.IsAbstract && other.GetInterfaces().FindFor(t => t.Equals(typeof(IPresentationRegister))) != null));

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
        public static void RegisterSystem(Type groupName, SceneReference dependenceScene, params Type[] systems)
        {
            Hash groupHash = Hash.NewHash(groupName.Name);
            if (!Instance.m_PresentationGroups.TryGetValue(groupHash, out Group group))
            {
                group = new Group(groupName, groupHash);
                Instance.m_PresentationGroups.Add(groupHash, group);

                Type t = typeof(PresentationSystemGroup<>).MakeGenericType(groupName);
                $"{t.Name}: {t.GenericTypeArguments[0]}".ToLog();
                PropertyInfo insProperty = typeof(PresentationSystemGroup<>).MakeGenericType(groupName).GetProperty(instance, BindingFlags.NonPublic | BindingFlags.Static);
                Assert.IsNotNull(insProperty);
                $"{insProperty.Name}".ToLog();
                Assert.IsNotNull(insProperty.GetValue(null, null));
                group.m_SystemGroup = (IPresentationSystemGroup)insProperty.GetValue(null, null);
            }

            if (dependenceScene != null)
            {
                if (!Instance.m_DependenceSceneList.TryGetValue(dependenceScene, out List<Hash> list))
                {
                    list = new List<Hash>();
                    Instance.m_DependenceSceneList.Add(dependenceScene, list);
                }

                if (list.Contains(groupHash)) throw new Exception();
                list.Add(groupHash);
            }

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
                if (group.m_RegisteredSystemTypes.Contains(systems[i]))
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Presentation,
                        $"{systems[i].Name}은 이미 등록된 시스템입니다.");
                }
                group.m_RegisteredSystemTypes.Add(systems[i]);
                Instance.m_RegisteredGroup.Add(systems[i], groupHash);
                $"System ({groupName}): {systems[i].GetType().Name} Registered".ToLog();
            }
        }
        private void StartPresentation()
        {
            StartPresentation(m_DefaultGroupHash);
        }
        internal void StartPresentation(Hash groupHash)
        {
            Group group = m_PresentationGroups[groupHash];
            if (group.m_IsStarted) throw new CoreSystemException(CoreSystemExceptionFlag.Presentation,
                    $"{group.m_Name.Name} 은 이미 시작된 시스템 그룹입니다.");

            for (int i = 0; i < group.m_RegisteredSystemTypes.Count; i++)
            {
                Type t = group.m_RegisteredSystemTypes[i];
                ConstructorInfo ctor = t.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, CallingConventions.HasThis, new Type[0], null);
                object ins;
                if (ctor != null)
                {
                    ins = ctor.Invoke(null);
                }
                else ins = Activator.CreateInstance(t);

                IPresentationSystem system = (IPresentationSystem)ins;
                group.m_Systems.Add(system);

                group.m_Initializers.Add((IInitPresentation)ins);
                if (system.EnableBeforePresentation) group.m_BeforePresentations.Add(system);
                if (system.EnableOnPresentation) group.m_OnPresentations.Add(system);
                if (system.EnableAfterPresentation) group.m_AfterPresentations.Add(system);

                $"System ({group.m_Name.Name}): {system.GetType().Name} Start".ToLog();
            }

            group.MainPresentation = Instance.StartUnityUpdate(Presentation(group));
            group.BackgroundPresentation = Instance.StartBackgroundUpdate(PresentationAsync(group));
            group.m_IsStarted = true;

            $"{group.m_Name.Name} group is started".ToLog();
        }
        internal void StopPresentation(Hash groupHash)
        {
            Group group = m_PresentationGroups[groupHash];
            if (!group.m_IsStarted) throw new CoreSystemException(CoreSystemExceptionFlag.Presentation,
                    $"{group.m_Name.Name} 은 이미 정지된 시스템 그룹입니다.");

            Instance.StopUnityUpdate(group.MainPresentation);
            Instance.StopUnityUpdate(group.BackgroundPresentation);
            for (int i = 0; i < group.m_Systems.Count; i++)
            {
                group.m_Systems[i].Dispose();
            }
            group.m_Systems.Clear();
            group.m_BeforePresentations.Clear();
            group.m_OnPresentations.Clear();
            group.m_AfterPresentations.Clear();

            $"{group.m_Name.Name} group is stopped".ToLog();
        }

        internal static void RegisterRequestSystem<T, TA>(Action<TA> setter) 
            where T : class, IPresentationSystem
            where TA : class, IPresentationSystem
        {
            if (!Instance.m_RegisteredGroup.TryGetValue(typeof(T), out Hash groupHash) ||
                !Instance.m_PresentationGroups.TryGetValue(groupHash, out Group group))
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Presentation,
                    $"시스템 {typeof(T).Name} 은 등록되지 않았습니다.");
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

        private static IEnumerator Presentation(Group group)
        {
            for (int i = 0; i < group.m_Initializers.Count; i++)
            {
                group.m_Initializers[i].OnInitialize();
            }
            group.m_MainthreadSignal = true;
            

            //$"main pre in".ToLog();

            for (int i = 0; i < group.m_Systems.Count; i++)
            {
                while (!group.m_Systems[i].IsStartable)
                {
                    yield return null;
                }
            }

            yield return new WaitUntil(() => group.m_BackgroundthreadSignal);
            for (int i = 0; i < group.m_Initializers.Count; i++)
            {
                group.m_Initializers[i].OnStartPresentation();
            }
            
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
        private static IEnumerator PresentationAsync(Group group)
        {
            for (int i = 0; i < group.m_Initializers.Count; i++)
            {
                group.m_Initializers[i].OnInitializeAsync();
            }
            "1".ToLog();

            yield return new WaitUntil(() => group.m_MainthreadSignal);
            int requestSystemCount = group.m_RequestSystemDelegates.Count;
            for (int i = 0; i < requestSystemCount; i++)
            {
                $"asd : {i} = {requestSystemCount}".ToLog();
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
            "2".ToLog();

            group.m_BackgroundthreadSignal = true;
            group.m_BackgroundInitDone = true;

            //"3".ToLog();
            yield return group.m_WaitUntilInitializeCompleted;
            //"4".ToLog();
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

                //"running".ToLog();
                yield return null;
            }
        }
    }
}
