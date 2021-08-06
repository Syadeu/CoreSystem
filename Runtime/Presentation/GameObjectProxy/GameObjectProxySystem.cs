using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Mono;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace Syadeu.Presentation
{
    internal sealed class GameObjectProxySystem : PresentationSystemEntity<GameObjectProxySystem>
    {
        private static readonly Vector3 INIT_POSITION = new Vector3(-9999, -9999, -9999);

        public override bool EnableBeforePresentation => true;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => true;

        public event Action<ProxyTransform> OnDataObjectCreated;
        public event Action<ProxyTransform> OnDataObjectDestroy;
        public event Action<ProxyTransform> OnDataObjectVisible;
        public event Action<ProxyTransform> OnDataObjectInvisible;

        public event Action<ProxyTransform, RecycleableMonobehaviour> OnDataObjectProxyCreated;
        public event Action<ProxyTransform, RecycleableMonobehaviour> OnDataObjectProxyRemoved;

        private NativeProxyData m_ProxyData;
#pragma warning disable IDE0090 // Use 'new(...)'
        private NativeQueue<ProxyTransform>
                m_RequestDestories = new NativeQueue<ProxyTransform>(Allocator.Persistent),
                m_RequestUpdates = new NativeQueue<ProxyTransform>(Allocator.Persistent),

                m_RequestProxyList = new NativeQueue<ProxyTransform>(Allocator.Persistent),
                m_RemoveProxyList = new NativeQueue<ProxyTransform>(Allocator.Persistent),
                m_VisibleList = new NativeQueue<ProxyTransform>(Allocator.Persistent),
                m_InvisibleList = new NativeQueue<ProxyTransform>(Allocator.Persistent);
#pragma warning restore IDE0090 // Use 'new(...)'
        private ParallelLoopResult m_VisibleJob;

        private SceneSystem m_SceneSystem;
        private Render.RenderSystem m_RenderSystem;

        private bool m_LoadingLock = false;
        private bool m_Disposed = false;

        public bool Disposed => m_Disposed;

        #region Presentation Methods
        protected override PresentationResult OnInitialize()
        {
            //m_VisibleCheckJobWorker = CoreSystem.CreateNewBackgroundJobWorker(true);
            //m_VisibleCheckJob = new BackgroundJob(ProxyVisibleCheckPararellJob);

            if (!PoolContainer<PrefabRequester>.Initialized) PoolContainer<PrefabRequester>.Initialize(() => new PrefabRequester(), 10);

            //ConsoleWindow.CreateCommand((cmd) =>
            //{
            //    for (int i = 0; i < m_MappedGameObjects.Length; i++)
            //    {
            //        DestoryDataObject(m_MappedGameObjects[i].m_Idx);
            //    }
            //}, "destroy", "all");

            return base.OnInitialize();
        }
        protected override PresentationResult OnInitializeAsync()
        {
            AssemblyName aName = new AssemblyName("CoreSystem_Runtime");
            AssemblyBuilder ab = AppDomain.CurrentDomain.DefineDynamicAssembly(aName, AssemblyBuilderAccess.Run);
            m_ModuleBuilder = ab.DefineDynamicModule(aName.Name);

            RequestSystem<SceneSystem>((other) => m_SceneSystem = other);
            RequestSystem<Render.RenderSystem>((other) => m_RenderSystem = other);

            m_ProxyData = new NativeProxyData(1024, Allocator.Persistent);
            EventDescriptor<ProxyTransform>.AddEvent(ProxyTransform.s_TranslationChanged, OnProxyTransformTranslationChanged);
            EventDescriptor<ProxyTransform>.AddEvent(ProxyTransform.s_RotationChanged, OnProxyTransformRotationChanged);
            EventDescriptor<ProxyTransform>.AddEvent(ProxyTransform.s_ScaleChanged, OnProxyTransformScaleChanged);
            EventDescriptor<ProxyTransform>.AddEvent(ProxyTransform.s_RequestProxy, OnProxyTransformProxyRequested);
            EventDescriptor<ProxyTransform>.AddEvent(ProxyTransform.s_RemoveProxy, OnProxyTransformProxyRemove);

            m_VisibleJob = m_ProxyData.ParallelFor((other) =>
            {
            });

            return base.OnInitializeAsync();
        }
        private void OnProxyTransformTranslationChanged(ProxyTransform data)
        {
            if (data.hasProxy && !data.hasProxyQueued)
            {
                m_RequestUpdates.Enqueue(data);
            }
        }
        private void OnProxyTransformRotationChanged(ProxyTransform data)
        {
            if (!data.hasProxy || data.hasProxyQueued) return;

            m_RequestUpdates.Enqueue(data);
        }
        private void OnProxyTransformScaleChanged(ProxyTransform data)
        {
            if (!data.hasProxy || data.hasProxyQueued) return;

            m_RequestUpdates.Enqueue(data);
        }
        private void OnProxyTransformProxyRequested(ProxyTransform data)
        {
            CoreSystem.Logger.Log(Channel.Proxy,
                $"Proxy requested at {data.index}, {data.prefab.GetObjectSetting().m_Name}");

            m_RequestProxyList.Enqueue(data);
        }
        private void OnProxyTransformProxyRemove(ProxyTransform data)
        {
            CoreSystem.Logger.Log(Channel.Proxy,
                $"Proxy removed at {data.index}, {data.prefab.GetObjectSetting().m_Name}");
            
            m_RemoveProxyList.Enqueue(data);
        }

        protected override PresentationResult OnStartPresentation()
        {
            m_SceneSystem.OnLoadingEnter += () =>
            {
                m_LoadingLock = true;
                CoreSystem.Logger.Log(Channel.Proxy, true,
                    "Scene on loading enter lambda excute");

                m_RequestDestories.Clear();
                m_RequestUpdates.Clear();

                m_RequestProxyList.Clear();
                m_RemoveProxyList.Clear();
                m_VisibleList.Clear();
                m_InvisibleList.Clear();

                m_Instances.Clear();
                m_TerminatedProxies.Clear();

                m_ProxyData.For((tr) =>
                {
                    OnDataObjectDestroy?.Invoke(tr);
                });

                m_ProxyData.Clear();
                m_LoadingLock = false;
            };

            return base.OnStartPresentation();
        }

        protected override PresentationResult AfterPresentation()
        {
            const int c_ChunkSize = 100;

            if (m_LoadingLock) return base.AfterPresentation();

            #region Update Proxy
            int requestUpdateCount = m_RequestUpdates.Count;
            for (int i = 0; i < requestUpdateCount; i++)
            {
                ProxyTransform tr = m_RequestUpdates.Dequeue();
                //"in0".ToLog();
                if (tr.isDestroyed || !tr.hasProxy)
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Proxy,
                        "Destroyed or does not have any proxy");
                }
                if (tr.hasProxyQueued)
                {
                    m_RequestUpdates.Enqueue(tr);
                    continue;
                }

                RecycleableMonobehaviour proxy = tr.proxy;
                proxy.transform.position = tr.position;
                proxy.transform.rotation = tr.rotation;
                proxy.transform.localScale = tr.scale;

                if (i != 0 && i % c_ChunkSize == 0) break;
            }
            #endregion

            #region Create / Remove Proxy
            int requestProxyCount = m_RequestProxyList.Count;
            for (int i = 0; i < requestProxyCount; i++)
            {
                ProxyTransform tr = m_RequestProxyList.Dequeue();
                //"in1".ToLog();
                if (tr.isDestroyed) continue;
                //{
                //    throw new CoreSystemException(CoreSystemExceptionFlag.Proxy,
                //        "Destroyed");
                //}
                if (tr.hasProxy && !tr.hasProxyQueued)
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Proxy,
                        $"Already have proxy, {tr.isDestroyed}:{tr.hasProxy}:{tr.hasProxyQueued}");
                }

                AddProxy(tr);
                if (i != 0 && i % c_ChunkSize == 0) break;
            }
            int removeProxyCount = m_RemoveProxyList.Count;
            for (int i = 0; i < removeProxyCount; i++)
            {
                ProxyTransform tr = m_RemoveProxyList.Dequeue();
                //"in2".ToLog();
                if (tr.isDestroyed || !tr.hasProxy)
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Proxy,
                        "Destroyed or does not have any proxy");
                }

                RemoveProxy(tr);
                if (i != 0 && i % c_ChunkSize == 0) break;
            }
            #endregion

            #region Visible / Invisible
            int visibleCount = m_VisibleList.Count;
            for (int i = 0; i < visibleCount; i++)
            {
                ProxyTransform tr = m_VisibleList.Dequeue();
                if (tr.isDestroyed)
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Proxy,
                        "Destroyed");
                }

                tr.isVisible = true;
                OnDataObjectVisible?.Invoke(tr);

                if (i != 0 && i % c_ChunkSize == 0) break;
            }
            int invisibleCount = m_InvisibleList.Count;
            for (int i = 0; i < invisibleCount; i++)
            {
                ProxyTransform tr = m_InvisibleList.Dequeue();
                if (tr.isDestroyed)
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Proxy,
                        "Destroyed");
                }

                tr.isVisible = false;
                OnDataObjectInvisible?.Invoke(tr);

                if (i != 0 && i % c_ChunkSize == 0) break;
            }
            #endregion

            #region Destroy
            int destroyCount = m_RequestDestories.Count;
            for (int i = 0; i < destroyCount; i++)
            {
                ProxyTransform tr = m_RequestDestories.Dequeue();
                if (tr.isDestroyed)
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Proxy,
                        "Already destroyed");
                }

                OnDataObjectDestroy?.Invoke(tr);

                if (tr.hasProxy && !tr.hasProxyQueued) RemoveProxy(tr);
                m_ProxyData.Remove(tr);

                if (i != 0 && i % c_ChunkSize == 0) break;
            }
            #endregion

            return PresentationResult.Normal;
        }
        protected override PresentationResult AfterPresentationAsync()
        {
            if (m_VisibleJob.IsCompleted)
            {
                m_VisibleJob = m_ProxyData.ParallelFor((other) =>
                {
                    // aabb의 꼭지점 중 단 하나라도 화면 내 존재하면 화면에 비추는 것으로 간주함.
                    if (m_RenderSystem.IsInCameraScreen(other.aabb.vertices))
                    {
                        if (other.enableCull && !other.hasProxy && !other.hasProxyQueued)
                        {
                            other.RequestProxy();
                        }

                        if (!other.isVisible) m_VisibleList.Enqueue(other);
                    }
                    else
                    {
                        if (other.hasProxy && !other.hasProxyQueued)
                        {
                            other.RemoveProxy();
                        }

                        if (other.isVisible) m_InvisibleList.Enqueue(other);
                    }
                });
            }
            return base.AfterPresentationAsync();
        }

        public override void Dispose()
        {
            m_RequestDestories.Dispose();
            m_RequestUpdates.Dispose();

            m_RequestProxyList.Dispose();
            m_RemoveProxyList.Dispose();
            m_VisibleList.Dispose();
            m_InvisibleList.Dispose();

            m_ProxyData.For((tr) =>
            {
                OnDataObjectDestroy?.Invoke(tr);
            });
            m_ProxyData.Dispose();

            m_Disposed = true;

            base.Dispose();
        }
        #endregion

        public ProxyTransform CreateNewPrefab(PrefabReference prefab, float3 pos, quaternion rot, float3 scale, bool enableCull, float3 center, float3 size)
        {
            CoreSystem.Logger.NotNull(m_RenderSystem, $"You've call this method too early or outside of PresentationSystem");

            ProxyTransform tr = m_ProxyData.Add(prefab, pos, rot, scale, enableCull, center, size);
            OnDataObjectCreated?.Invoke(tr);

            CoreSystem.Logger.Log(Channel.Proxy, true,
                $"ProxyTransform({prefab.GetObjectSetting().m_Name}) has been created at {pos}");
            return tr;
        }
        public void Destroy(ProxyTransform proxyTransform)
        {
            m_RequestDestories.Enqueue(proxyTransform);
            CoreSystem.Logger.Log(Channel.Proxy,
                $"Destroy called at {proxyTransform.index}");
        }

        #region Proxy Object Control

        private void AddProxy(ProxyTransform proxyTransform)
        {
            CoreSystem.Logger.ThreadBlock(ThreadInfo.Unity);

            PrefabReference prefab = proxyTransform.prefab;

            if (!m_TerminatedProxies.TryGetValue(prefab, out Queue<RecycleableMonobehaviour> pool) ||
                    pool.Count == 0)
            {
                InstantiatePrefab(prefab, (other) =>
                {
                    if (proxyTransform.isDestroyed)
                    {
                        if (other.InitializeOnCall) other.Terminate();
                        other.transform.position = INIT_POSITION;

                        if (!m_TerminatedProxies.TryGetValue(prefab, out Queue<RecycleableMonobehaviour> pool))
                        {
                            pool = new Queue<RecycleableMonobehaviour>();
                            m_TerminatedProxies.Add(prefab, pool);
                        }
                        pool.Enqueue(other);
                        return;
                    }

                    proxyTransform.SetProxy(new int2(prefab, other.m_Idx));

                    other.transform.position = proxyTransform.position;
                    other.transform.rotation = proxyTransform.rotation;
                    other.transform.localScale = proxyTransform.scale;

                    OnDataObjectProxyCreated?.Invoke(proxyTransform, other);
                    CoreSystem.Logger.Log(Channel.Proxy, true,
                        $"Prefab({proxyTransform.prefab.GetObjectSetting().m_Name}) proxy created");
                });
            }
            else
            {
                RecycleableMonobehaviour other = pool.Dequeue();
                proxyTransform.SetProxy(new int2(prefab, other.m_Idx));

                other.transform.position = proxyTransform.position;
                other.transform.rotation = proxyTransform.rotation;
                other.transform.localScale = proxyTransform.scale;

                if (other.InitializeOnCall) other.Initialize();

                OnDataObjectProxyCreated?.Invoke(proxyTransform, other);
                CoreSystem.Logger.Log(Channel.Proxy, true,
                    $"Prefab({proxyTransform.prefab.GetObjectSetting().m_Name}) proxy created, pool remains {pool.Count}");
            }
        }
        private void RemoveProxy(ProxyTransform proxyTransform)
        {
            PrefabReference prefab = proxyTransform.prefab;
            RecycleableMonobehaviour proxy = proxyTransform.proxy;

            OnDataObjectProxyRemoved?.Invoke(proxyTransform, proxy);

            proxyTransform.SetProxy(ProxyTransform.ProxyNull);

            if (proxy.Activated) proxy.Terminate();

            if (!m_TerminatedProxies.TryGetValue(prefab, out Queue<RecycleableMonobehaviour> pool))
            {
                pool = new Queue<RecycleableMonobehaviour>();
                m_TerminatedProxies.Add(prefab, pool);
            }
            pool.Enqueue(proxy);
            CoreSystem.Logger.Log(Channel.Proxy, true,
                    $"Prefab({prefab.GetObjectSetting().m_Name}) proxy removed.");
        }

        #endregion

        #region Experimental

        private ModuleBuilder m_ModuleBuilder;
        private readonly Dictionary<Type, GenericType> m_GenericTypes = new Dictionary<Type, GenericType>();

        /// <summary>
        /// 사용한 오브젝트를 재사용할 수 있도록 해당 오브젝트 풀로 반환합니다.<br/>
        /// 해당 타입이 <seealso cref="ITerminate"/>를 참조하면 <seealso cref="ITerminate.Terminate"/>를 호출합니다.
        /// </summary>
        public void ReturnGenericTypeObject<T>(MonoBehaviour<T> obj)
        {
            if (!m_GenericTypes.TryGetValue(TypeHelper.TypeOf<T>.Type, out GenericType runtimeType))
            {
                runtimeType = new GenericType
                {
                    TargetType = TypeHelper.TypeOf<T>.Type,
                    BakedType = obj.GetType()
                };
                m_GenericTypes.Add(TypeHelper.TypeOf<T>.Type, runtimeType);
            }
            if (obj is ITerminate terminate) terminate.Terminate();
            runtimeType.ObjectPool.Enqueue(obj);
        }
        /// <inheritdoc cref="ReturnGenericTypeObject{T}(MonoBehaviour{T})"/>
        public void ReturnObject<T>(T obj) where T : MonoBehaviour
        {
            if (!m_CompliedTypes.TryGetValue(TypeHelper.TypeOf<T>.Type, out CompliedType compliedType))
            {
                compliedType = new CompliedType
                {
                    Type = TypeHelper.TypeOf<T>.Type,
                };
                m_CompliedTypes.Add(TypeHelper.TypeOf<T>.Type, compliedType);
            }
            if (obj is ITerminate terminate) terminate.Terminate();
            compliedType.ObjectPool.Enqueue(obj);
        }
        /// <summary>
        /// 해당 <typeparamref name="T"/>의 값을 가지는 타입을 직접 만들어서 반환하거나 미사용 오브젝트를 반환합니다.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public MonoBehaviour<T> GetOrCreateGenericTypeObject<T>()
        {
            if (!m_GenericTypes.TryGetValue(TypeHelper.TypeOf<T>.Type, out GenericType runtimeType) ||
                runtimeType.ObjectPool.Count == 0)
            {
                return CreateGenericTypeObject<T>();
            }
            return runtimeType.ObjectPool.Dequeue() as MonoBehaviour<T>;
        }
        public Type GetGenericType<T>()
        {
            if (!m_GenericTypes.TryGetValue(TypeHelper.TypeOf<T>.Type, out GenericType runtimeType))
            {
                Type baked = MakeGenericTypeMonobehaviour<T>();
                runtimeType = new GenericType
                {
                    TargetType = TypeHelper.TypeOf<T>.Type,
                    BakedType = baked
                };
                m_GenericTypes.Add(TypeHelper.TypeOf<T>.Type, runtimeType);
            }
            return runtimeType.BakedType;
        }
        /// <summary>
        /// 단일 스크립트를 지닌 오브젝트를 생성하여 반환하거나 미사용 오브젝트를 반환합니다.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetOrCreateObject<T>() where T : MonoBehaviour
        {
            if (!m_CompliedTypes.TryGetValue(TypeHelper.TypeOf<T>.Type, out CompliedType compliedType) ||
                compliedType.ObjectPool.Count == 0)
            {
                return CreateCompliedTypeObject<T>();
            }
            return compliedType.ObjectPool.Dequeue() as T;
        }

        #region Runtime Generic MonoBehaviour Maker
        private class GenericType
        {
            public Type TargetType;
            public Type BakedType;

            public int CreatedCount = 0;
            public Queue<MonoBehaviour> ObjectPool = new Queue<MonoBehaviour>();
        }
        /// <summary>
        /// 런타임에서 요구하는 <typeparamref name="T"/>의 값의 <see cref="MonoBehaviour"/> 타입을 만들어 반환합니다.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private Type MakeGenericTypeMonobehaviour<T>()
        {
            const string newName = "{0}Proxy";

            Type testMono = typeof(MonoBehaviour<>).MakeGenericType(TypeHelper.TypeOf<T>.Type);
            TypeBuilder tb = m_ModuleBuilder.DefineType(
                string.Format(newName, TypeHelper.TypeOf<T>.Name), TypeAttributes.Public, testMono);

            return tb.CreateType();
        }
        private MonoBehaviour<T> CreateGenericTypeObject<T>()
        {
            if (TypeHelper.TypeOf<T>.IsAbstract) throw new Exception();

            GameObject obj = new GameObject(TypeHelper.TypeOf<T>.Name);
            if (!m_GenericTypes.TryGetValue(TypeHelper.TypeOf<T>.Type, out GenericType runtimeType))
            {
                Type baked = MakeGenericTypeMonobehaviour<T>();
                runtimeType = new GenericType
                {
                    TargetType = TypeHelper.TypeOf<T>.Type,
                    BakedType = baked
                };
                m_GenericTypes.Add(TypeHelper.TypeOf<T>.Type, runtimeType);
            }
            obj.name += runtimeType.CreatedCount;
            runtimeType.CreatedCount++;
            return obj.AddComponent(runtimeType.BakedType) as MonoBehaviour<T>;
        }
        #endregion

        #region Complied MonoBehaviour Maker
        private readonly Dictionary<Type, CompliedType> m_CompliedTypes = new Dictionary<Type, CompliedType>();
        private class CompliedType
        {
            public Type Type;
            public GameObject Prefab;

            public int CreatedCount = 0;
            public Queue<MonoBehaviour> ObjectPool = new Queue<MonoBehaviour>();
        }
        private T CreateCompliedTypeObject<T>() where T : MonoBehaviour
        {
            if (TypeHelper.TypeOf<T>.IsAbstract) throw new Exception();

            GameObject obj = new GameObject(TypeHelper.TypeOf<T>.Name);
            if (!m_CompliedTypes.TryGetValue(TypeHelper.TypeOf<T>.Type, out CompliedType compliedType))
            {
                compliedType = new CompliedType
                {
                    Type = TypeHelper.TypeOf<T>.Type,
                };
                m_CompliedTypes.Add(compliedType.Type, compliedType);
            }
            obj.name += compliedType.CreatedCount;
            compliedType.CreatedCount++;
            return obj.AddComponent<T>();
        }
        #endregion

        #endregion

        #region Instantiate Prefab From PrefabList

        /// <summary>
        /// 생성된 프록시 오브젝트들입니다. key 값은 <see cref="PrefabList.m_ObjectSettings"/> 인덱스(<see cref="PrefabReference"/>)이며 value 배열은 상태 구분없이(사용중이던 아니던) 실제 생성된 모노 객체입니다.
        /// </summary>
        internal readonly Dictionary<PrefabReference, List<RecycleableMonobehaviour>> m_Instances = new Dictionary<PrefabReference, List<RecycleableMonobehaviour>>();
        private readonly Dictionary<PrefabReference, Queue<RecycleableMonobehaviour>> m_TerminatedProxies = new Dictionary<PrefabReference, Queue<RecycleableMonobehaviour>>();

        private sealed class PrefabRequester
        {
            GameObjectProxySystem m_ProxySystem;
            SceneSystem m_SceneSystem;
            Scene m_RequestedScene;

            public void Setup(GameObjectProxySystem proxySystem, SceneSystem sceneSystem, PrefabReference prefabIdx, Vector3 pos, Quaternion rot,
                Action<RecycleableMonobehaviour> onCompleted)
            {
                //var prefabInfo = PrefabList.Instance.ObjectSettings[prefabIdx];
                var prefabInfo = prefabIdx.GetObjectSetting();
                if (!proxySystem.m_TerminatedProxies.TryGetValue(prefabIdx, out var pool))
                {
                    pool = new Queue<RecycleableMonobehaviour>();
                    proxySystem.m_TerminatedProxies.Add(prefabIdx, pool);
                }

                if (pool.Count > 0)
                {
                    RecycleableMonobehaviour obj = pool.Dequeue();
                    obj.transform.position = pos;
                    obj.transform.rotation = rot;

                    if (!m_ProxySystem.m_Instances.TryGetValue(prefabIdx, out List<RecycleableMonobehaviour> instances))
                    {
                        instances = new List<RecycleableMonobehaviour>();
                        m_ProxySystem.m_Instances.Add(prefabIdx, instances);
                    }

                    obj.m_Idx = instances.Count;
                    instances.Add(obj);

                    obj.InternalOnCreated();
                    if (obj.InitializeOnCall) obj.Initialize();
                    onCompleted?.Invoke(obj);

                    PoolContainer<PrefabRequester>.Enqueue(this);
                    return;
                }

                m_ProxySystem = proxySystem;
                m_SceneSystem = sceneSystem;
                m_RequestedScene = m_SceneSystem.CurrentScene;

                if (CoreSystem.InstanceGroupTr == null) CoreSystem.InstanceGroupTr = new GameObject("InstanceSystemGroup").transform;

                AssetReference refObject = prefabInfo.m_RefPrefab;

                var oper = prefabInfo.m_RefPrefab.InstantiateAsync(pos, rot, CoreSystem.InstanceGroupTr);
                oper.Completed += (other) =>
                {
                    Scene currentScene = m_SceneSystem.CurrentScene;
                    if (!currentScene.Equals(m_RequestedScene))
                    {
                        CoreSystem.Logger.LogWarning(Channel.Proxy, $"{other.Result.name} is returned because Scene has been changed");
                        refObject.ReleaseInstance(other.Result);
                        return;
                    }

                    RecycleableMonobehaviour recycleable = other.Result.GetComponent<RecycleableMonobehaviour>();
                    if (recycleable == null)
                    {
                        recycleable = other.Result.AddComponent<ManagedRecycleObject>();
                    }

                    if (!m_ProxySystem.m_Instances.TryGetValue(prefabIdx, out List<RecycleableMonobehaviour> instances))
                    {
                        instances = new List<RecycleableMonobehaviour>();
                        m_ProxySystem.m_Instances.Add(prefabIdx, instances);
                    }

                    recycleable.m_Idx = instances.Count;
                    instances.Add(recycleable);

                    recycleable.InternalOnCreated();
                    if (recycleable.InitializeOnCall) recycleable.Initialize();
                    onCompleted?.Invoke(recycleable);

                    PoolContainer<PrefabRequester>.Enqueue(this);
                };
            }
        }

        private void InstantiatePrefab(PrefabReference prefab, Action<RecycleableMonobehaviour> onCompleted)
            => InstantiatePrefab(prefab, INIT_POSITION, Quaternion.identity, onCompleted);
        private void InstantiatePrefab(PrefabReference prefab, Vector3 position, Quaternion rotation, Action<RecycleableMonobehaviour> onCompleted)
        {
            PoolContainer<PrefabRequester>.Dequeue().Setup(this, m_SceneSystem, prefab, position, rotation, onCompleted);
        }

        #endregion
    }
}
