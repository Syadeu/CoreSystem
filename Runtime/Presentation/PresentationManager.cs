using Syadeu.Database;
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
using UnityEngine;
using UnityEngine.Assertions;

namespace Syadeu.Presentation
{
    [StaticManagerIntializeOnLoad]
    public sealed class PresentationManager : StaticDataManager<PresentationManager>
    {
        const string c_Instance = "Instance";

        internal class Group
        {
            public Type m_Name;
            public Hash m_Hash;
            public readonly List<Type> m_RegisteredSystemTypes = new List<Type>();
            public bool m_IsStarted = false;

            public IPresentationSystemGroup m_SystemGroup;

            public readonly List<PresentationSystemEntity> m_Systems = new List<PresentationSystemEntity>();
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

            public Action PublicSystemStructDisposer;

            public Group(Type name, Hash hash)
            {
                m_Name = name;
                m_Hash = hash;

                m_WaitUntilInitializeCompleted = new WaitUntil(() => m_MainInitDone && m_BackgroundInitDone);
            }

            public bool HasSystem<T>(T system) where T : PresentationSystemEntity
                => m_Systems.FindFor((other) => other.Equals(system)) != null;

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

                PublicSystemStructDisposer?.Invoke();
                PublicSystemStructDisposer = null;

                m_MainthreadSignal = false;
                m_BackgroundthreadSignal = false;

                m_MainInitDone = false;
                m_BackgroundInitDone = false;

                PublicSystemStructDisposer?.Invoke();
                PublicSystemStructDisposer = null;
            }
        }
        private readonly Hash m_DefaultGroupHash = Hash.NewHash(TypeHelper.TypeOf<DefaultPresentationGroup>.Name);

        internal readonly Dictionary<Hash, Group> m_PresentationGroups = new Dictionary<Hash, Group>();
        internal readonly Dictionary<Type, Hash> m_RegisteredGroup = new Dictionary<Type, Hash>();
        internal readonly Dictionary<string, List<Hash>> m_DependenceSceneList = new Dictionary<string, List<Hash>>();

        public override void OnInitialize()
        {
            const string register = "Register";

            List<Type> registers = new List<Type>();
            //registers.AddRange(CoreSystem.GetInternalTypes(
            //    (other) => !other.IsAbstract && other.GetInterfaces().FindFor(t => t.Equals(typeof(IPresentationRegister))) != null)
            //    );
            //registers.AddRange(CoreSystem.GetMainAssemblyTypes(
            //    (other) => !other.IsAbstract && other.GetInterfaces().FindFor(t => t.Equals(typeof(IPresentationRegister))) != null));

            //registers.AddRange(AppDomain
            //    .CurrentDomain.GetAssemblies()
            //    .Where(a => !a.IsDynamic)
            //    .SelectMany(a => a.GetTypes())
            //    .Where(t => !t.IsAbstract && t.GetInterfaces().FindFor(ta => ta.Equals(typeof(IPresentationRegister))) != null)
                //);
            registers.AddRange(TypeHelper.GetTypes(t => !t.IsAbstract && t.GetInterfaces().FindFor(ta => ta.Equals(TypeHelper.TypeOf<IPresentationRegister>.Type)) != null));

            //Assembly.g

            MethodInfo registerMethod = TypeHelper.TypeOf<IPresentationRegister>.Type.GetMethod(register);

            for (int i = 0; i < registers.Count; i++)
            {
                object ins;
                PropertyInfo instanceProperty = registers[i].GetProperty(c_Instance, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (instanceProperty != null)
                {
                    ins = instanceProperty.GetGetMethod().Invoke(null, null);
                }
                else ins = Activator.CreateInstance(registers[i]);

                registerMethod.Invoke(ins, null);
            }

            StartPresentation(m_DefaultGroupHash);
        }

        #region Internals
        internal static void RegisterSystem(Type groupName, SceneReference dependenceScene, params Type[] systems)
        {
            if (dependenceScene != null)
            {
                CoreSystem.Log(Channel.Presentation, $"Registration start ({groupName.Name.Split('.').Last()}), number of {systems.Length}, has dependece scene ({dependenceScene.ScenePath})");
            }
            else CoreSystem.Log(Channel.Presentation, $"Registration start ({groupName.Name.Split('.').Last()}), number of {systems.Length}");

            Hash groupHash = Hash.NewHash(groupName.Name);
            if (!Instance.m_PresentationGroups.TryGetValue(groupHash, out Group group))
            {
                group = new Group(groupName, groupHash);
                Instance.m_PresentationGroups.Add(groupHash, group);

                //Type t = typeof(PresentationSystemGroup<>).MakeGenericType(groupName);
                //$"{t.Name}: {t.GenericTypeArguments[0]}".ToLog();
                PropertyInfo insProperty = typeof(PresentationSystemGroup<>).MakeGenericType(groupName).GetProperty(c_Instance, BindingFlags.NonPublic | BindingFlags.Static);
                
                CoreSystem.NotNull(insProperty);

                //$"{insProperty.Name}".ToLog();
                //Assert.IsNotNull(insProperty.GetValue(null, null));
                group.m_SystemGroup = (IPresentationSystemGroup)insProperty.GetValue(null, null);
                CoreSystem.NotNull(group.m_SystemGroup);
            }

            if (dependenceScene != null)
            {
                if (!Instance.m_DependenceSceneList.TryGetValue(dependenceScene, out List<Hash> list))
                {
                    list = new List<Hash>();
                    Instance.m_DependenceSceneList.Add(dependenceScene, list);
                }

                if (list.Contains(groupHash)) CoreSystem.LogError(Channel.Presentation, $"{groupName.Name.Split('.').Last()} 은 이미 해당 씬({dependenceScene.ScenePath})에 종속되었습니다. 중복 추가는 허용하지 않습니다.");
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
                CoreSystem.Log(Channel.Presentation, $"System ({groupName.Name.Split('.').Last()}): {systems[i].Name} Registered");
            }

            CoreSystem.Log(Channel.Presentation, $"Registration Ended ({groupName.Name.Split('.').Last()}), number of {systems.Length}");
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

                PresentationSystemEntity system = (PresentationSystemEntity)ins;
                group.m_Systems.Add(system);

                group.m_Initializers.Add((IInitPresentation)ins);
                if (system.EnableBeforePresentation) group.m_BeforePresentations.Add(system);
                if (system.EnableOnPresentation) group.m_OnPresentations.Add(system);
                if (system.EnableAfterPresentation) group.m_AfterPresentations.Add(system);

                //$"System ({group.m_Name.Name}): {system.GetType().Name} Start".ToLog();
            }

            group.MainPresentation = Instance.StartUnityUpdate(Presentation(group));
            group.BackgroundPresentation = Instance.StartBackgroundUpdate(PresentationAsync(group));
            group.m_IsStarted = true;

            CoreSystem.Log(Channel.Presentation, $"{group.m_Name.Name} group is started");
        }
        internal void StopPresentation(Hash groupHash)
        {
            Group group = m_PresentationGroups[groupHash];
            if (!group.m_IsStarted) throw new CoreSystemException(CoreSystemExceptionFlag.Presentation,
                    $"{group.m_Name.Name} 은 이미 정지된 시스템 그룹입니다.");

            Instance.StopUnityUpdate(group.MainPresentation);
            Instance.StopUnityUpdate(group.BackgroundPresentation);

            group.Reset();

            CoreSystem.Log(Channel.Presentation, $"{group.m_Name.Name} group is stopped");
        }

        internal static void RegisterRequestSystem<T, TA>(Action<TA> setter) 
            where T : PresentationSystemEntity
            where TA : PresentationSystemEntity
        {
            if (!Instance.m_RegisteredGroup.TryGetValue(typeof(T), out Hash groupHash) ||
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
                    CoreSystem.LogError(Channel.Presentation, $"Requested system ({TypeHelper.TypeOf<TA>.Name}) not found");
                }
                else CoreSystem.Log(Channel.Presentation, $"Requested system ({TypeHelper.TypeOf<TA>.Name}) found");

                setter.Invoke(system);
            });
            //"request in".ToLog();
        }
        #endregion

        #region Presentation Group Method
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

            DateTime dateTime = DateTime.Now;
            yield return new WaitUntil(() =>
            {
                ////if (dateTime.Ticks + 1000 < DateTime.Now.Ticks)
                //{
                //    "waitting m_BackgroundthreadSignal".ToLog();
                //}
                return group.m_BackgroundthreadSignal;
            });
            for (int i = 0; i < group.m_Initializers.Count; i++)
            {
                group.m_Initializers[i].OnStartPresentation();
            }
            
            //group.m_IsPresentationStarted = true;
            //OnPresentationStarted?.Invoke();
            group.m_MainInitDone = true;

            yield return group.m_WaitUntilInitializeCompleted;
            CoreSystem.Log(Channel.Presentation, $"Presentation group ({group.m_Name.Name}) started");

            PresentationResult result;
            while (true)
            {
                for (int i = 0; i < group.m_BeforePresentations.Count; i++)
                {
                    result = group.m_BeforePresentations[i].BeforePresentation();
                    if (result.m_Result != ResultFlag.Normal)
                    {
                        ConsoleWindow.Log(result.m_Message, result.m_Result);
                    }
                }
                for (int i = 0; i < group.m_OnPresentations.Count; i++)
                {
                    result = group.m_OnPresentations[i].OnPresentation();
                    if (result.m_Result != ResultFlag.Normal)
                    {
                        ConsoleWindow.Log(result.m_Message, result.m_Result);
                    }
                }
                for (int i = 0; i < group.m_AfterPresentations.Count; i++)
                {
                    result = group.m_AfterPresentations[i].AfterPresentation();
                    if (result.m_Result != ResultFlag.Normal)
                    {
                        ConsoleWindow.Log(result.m_Message, result.m_Result);
                    }
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
            //"1".ToLog();

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
            //"2".ToLog();

            group.m_BackgroundthreadSignal = true;
            group.m_BackgroundInitDone = true;

            //"3".ToLog();
            yield return group.m_WaitUntilInitializeCompleted;
            //"4".ToLog();

            PresentationResult result;
            while (true)
            {
                for (int i = 0; i < group.m_BeforePresentations.Count; i++)
                {
                    result = group.m_BeforePresentations[i].BeforePresentationAsync();
                    if (result.m_Result != ResultFlag.Normal)
                    {
                        ConsoleWindow.Log(result.m_Message, result.m_Result);
                    }
                }
                for (int i = 0; i < group.m_OnPresentations.Count; i++)
                {
                    result = group.m_OnPresentations[i].OnPresentationAsync();
                    if (result.m_Result != ResultFlag.Normal)
                    {
                        ConsoleWindow.Log(result.m_Message, result.m_Result);
                    }
                }
                for (int i = 0; i < group.m_AfterPresentations.Count; i++)
                {
                    result = group.m_AfterPresentations[i].AfterPresentationAsync();
                    if (result.m_Result != ResultFlag.Normal)
                    {
                        ConsoleWindow.Log(result.m_Message, result.m_Result);
                    }
                }

                //"running".ToLog();
                yield return null;
            }
        }
        #endregion
    }
}
