using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Mono;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Render;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace Syadeu.Presentation
{
    internal sealed class GameObjectProxySystem : PresentationSystemEntity<GameObjectProxySystem>
    {
        private static readonly Vector3 INIT_POSITION = new Vector3(-9999, -9999, -9999);
        //private const int c_InitialMemorySize = 16384;
        private const int c_InitialMemorySize = 1024;

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
        private Cluster<ProxyTransformData> m_ClusterData;
#pragma warning disable IDE0090 // Use 'new(...)'
        private NativeQueue<int>
                m_RequestDestories,

                m_RequestProxyList,
                m_RemoveProxyList,
                m_VisibleList,
                m_InvisibleList;
        private NativeQueue<ClusterUpdateRequest>
                m_ClusterUpdates;
        private NativeQueue<ClusterIDRequest>
                m_ClusterIDRequests;
#pragma warning restore IDE0090 // Use 'new(...)'

        private SceneSystem m_SceneSystem;
        private RenderSystem m_RenderSystem;
        private EventSystem m_EventSystem;

        private bool m_LoadingLock = false;
        private bool m_Disposed = false;

        public bool Disposed => m_Disposed;

        #region Presentation Methods
        protected override PresentationResult OnInitialize()
        {
            if (!PoolContainer<PrefabRequester>.Initialized) PoolContainer<PrefabRequester>.Initialize(() => new PrefabRequester(), 10);

            m_RequestDestories = new NativeQueue<int>(Allocator.Persistent);

            m_RequestProxyList = new NativeQueue<int>(Allocator.Persistent);
            m_RemoveProxyList = new NativeQueue<int>(Allocator.Persistent);
            m_VisibleList = new NativeQueue<int>(Allocator.Persistent);
            m_InvisibleList = new NativeQueue<int>(Allocator.Persistent);

            m_ClusterUpdates = new NativeQueue<ClusterUpdateRequest>(Allocator.Persistent);
            m_ClusterIDRequests = new NativeQueue<ClusterIDRequest>(Allocator.Persistent);

            return base.OnInitialize();
        }
        protected override PresentationResult OnInitializeAsync()
        {
            m_ProxyData = new NativeProxyData(c_InitialMemorySize, Allocator.Persistent);
            m_ClusterData = new Cluster<ProxyTransformData>(c_InitialMemorySize);

            RequestSystem<SceneSystem>(Bind);
            RequestSystem<RenderSystem>(Bind);
            RequestSystem<EventSystem>(Bind);

            return base.OnInitializeAsync();
        }
        public override void OnDispose()
        {
            m_RequestDestories.Dispose();

            m_RequestProxyList.Dispose();
            m_RemoveProxyList.Dispose();
            m_VisibleList.Dispose();
            m_InvisibleList.Dispose();

            m_ClusterUpdates.Dispose();
            m_ClusterIDRequests.Dispose();

            m_ProxyData.For((tr) =>
            {
                OnDataObjectDestroy?.Invoke(tr);
            });
            m_ProxyData.Dispose();
            m_ClusterData.Dispose();

            if (m_SortedCluster.IsCreated) m_SortedCluster.Dispose();

            m_Disposed = true;
        }

        #region Binds
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Bind(SceneSystem other)
        {
            m_SceneSystem = other;
            m_SceneSystem.OnLoadingEnter += M_SceneSystem_OnLoadingEnter;
        }
        private void M_SceneSystem_OnLoadingEnter()
        {
            m_LoadingLock = true;
            CoreSystem.Logger.Log(Channel.Proxy, true,
                "Scene on loading enter lambda excute");

            m_RequestDestories.Clear();

            m_RequestProxyList.Clear();
            m_RemoveProxyList.Clear();
            m_VisibleList.Clear();
            m_InvisibleList.Clear();

            m_ProxyData.For(DestroyTransform);

            m_Instances.Clear();
            m_TerminatedProxies.Clear();

            m_ProxyData.Dispose();
            m_ClusterData.Dispose();
            m_ProxyData = new NativeProxyData(c_InitialMemorySize, Allocator.Persistent);
            m_ClusterData = new Cluster<ProxyTransformData>(c_InitialMemorySize);
            m_LoadingLock = false;

            void DestroyTransform(ProxyTransform tr)
            {
                OnDataObjectDestroy?.Invoke(tr);

                if (tr.hasProxy && !tr.hasProxyQueued)
                {
                    UnityEngine.Object.Destroy(tr.proxy.gameObject);
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Bind(RenderSystem other)
        {
            m_RenderSystem = other;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Bind(EventSystem other)
        {
            m_EventSystem = other;

            m_EventSystem.AddEvent<OnTransformChangedEvent>(OnTransformChanged);
        }
        #endregion

        unsafe private void OnTransformChanged(OnTransformChangedEvent ev)
        {
            if (ev.transform.isDestroyed) return;

            if (!ev.transform.Pointer->m_ClusterID.Equals(ClusterID.Requested))
            {
                //m_ClusterData.Update(in ev.transform.Pointer->m_ClusterID, ev.transform.position);
                m_ClusterUpdates.Enqueue(new ClusterUpdateRequest(ev.transform, ev.transform.Pointer->m_ClusterID, ev.transform.position));
            }

            if (!ev.transform.hasProxy || ev.transform.hasProxyQueued) return;

            RecycleableMonobehaviour proxy = ev.transform.proxy;
            proxy.transform.position = ev.transform.position;
            proxy.transform.rotation = ev.transform.rotation;
            proxy.transform.localScale = ev.transform.scale;
        }

        private NativeList<ClusterGroup<ProxyTransformData>> m_SortedCluster;
        protected override PresentationResult AfterPresentation()
        {
            const int c_ChunkSize = 100;

            if (m_LoadingLock) return base.AfterPresentation();

            #region Destroy
            int destroyCount = m_RequestDestories.Count;
            for (int i = 0; i < destroyCount; i++)
            {
                ProxyTransform tr = m_ProxyData[m_RequestDestories.Dequeue()];
                if (tr.isDestroyed)
                {
                    CoreSystem.Logger.LogError(Channel.Proxy,
                        $"Already destroyed");
                    continue;
                }

                OnDataObjectDestroy?.Invoke(tr);

                if (tr.hasProxy) RemoveProxy(tr);
                else if (tr.hasProxyQueued)
                {
                    "on destroy but proxy queued".ToLogError();
                }
                else
                {
                    "in".ToLog();
                }

                unsafe
                {
                    m_ClusterData.Remove(tr.Pointer->m_ClusterID);
                }
                m_ProxyData.Remove(tr);
            }
            #endregion

            #region Create / Remove Proxy
            int requestProxyCount = m_RequestProxyList.Count;
            for (int i = 0; i < requestProxyCount; i++)
            {
                //if (i != 0 && i % c_ChunkSize == 0) break;

                ProxyTransform tr = m_ProxyData[m_RequestProxyList.Dequeue()];

                if (tr.isDestroyed)
                {
                    CoreSystem.Logger.LogError(Channel.Proxy, $"1 destroyed transform");
                    continue;
                }
                else if (tr.hasProxy && !tr.hasProxyQueued)
                {
                    CoreSystem.Logger.LogError(Channel.Proxy, $"Already have proxy");
                    continue;
                }

                AddProxy(tr);
            }
            int removeProxyCount = m_RemoveProxyList.Count;
            for (int i = 0; i < removeProxyCount; i++)
            {
                //if (i != 0 && i % c_ChunkSize == 0) break;

                ProxyTransform tr = m_ProxyData[m_RemoveProxyList.Dequeue()];

                if (tr.isDestroyed)
                {
                    CoreSystem.Logger.LogError(Channel.Proxy, $"2 destroyed transform");
                    continue;
                }
                else if (!tr.hasProxy)
                {
                    CoreSystem.Logger.LogError(Channel.Proxy,
                        $"Does not have any proxy");
                    continue;
                }

                RemoveProxy(tr);
            }
            #endregion

            #region Visible / Invisible
            int visibleCount = m_VisibleList.Count;
            for (int i = 0; i < visibleCount; i++)
            {
                //if (i != 0 && i % c_ChunkSize == 0) break;

                ProxyTransform tr = m_ProxyData[m_VisibleList.Dequeue()];
                if (tr.isDestroyed) continue;

                tr.isVisible = true;
                OnDataObjectVisible?.Invoke(tr);
            }
            int invisibleCount = m_InvisibleList.Count;
            for (int i = 0; i < invisibleCount; i++)
            {
                //if (i != 0 && i % c_ChunkSize == 0) break;

                ProxyTransform tr = m_ProxyData[m_InvisibleList.Dequeue()];

                if (tr.isDestroyed) continue;

                tr.isVisible = false;
                OnDataObjectInvisible?.Invoke(tr);
            }
            #endregion

            int clusterIDRequestCount = m_ClusterIDRequests.Count;
            for (int i = 0; i < clusterIDRequestCount; i++)
            {
                var temp = m_ClusterIDRequests.Dequeue();
                var id = m_ClusterData.Add(temp.translation, temp.index);

                m_ProxyData[temp.index].Ref.m_ClusterID = id;
            }

            #region Jobs

            if (m_ClusterUpdates.Count > 0)
            {
                NativeArray<ClusterUpdateRequest> requests = m_ClusterUpdates.ToArray(Allocator.TempJob);
                m_ClusterUpdates.Clear();
                ClusterUpdateJob clusterUpdateJob = new ClusterUpdateJob
                {
                    m_ClusterData = m_ClusterData.AsParallelWriter(),
                    m_Requests = requests
                };
                ScheduleAt(JobPosition.On, clusterUpdateJob, requests.Length);
            }

            CameraFrustum frustum = m_RenderSystem.GetRawFrustum();

            if (m_SortedCluster.IsCreated)
            {
                m_SortedCluster.Clear();
                //m_SortedCluster.RemoveRangeSwapBackWithBeginEnd(0, m_SortedCluster.Length);
            }
            else m_SortedCluster = new NativeList<ClusterGroup<ProxyTransformData>>(Allocator.Persistent);

            ClusterJob clusterJob = new ClusterJob
            {
                m_ClusterData = m_ClusterData,
                m_Frustum = frustum,
                m_Output = m_SortedCluster
            };
            ScheduleAt(JobPosition.On, clusterJob);

            unsafe
            {
                NativeProxyData.UnsafeList list = *m_ProxyData.m_UnsafeList;

                ProxyJob proxyJob = new ProxyJob
                {
                    m_ActiveData = m_SortedCluster.AsDeferredJobArray(),
                    List = list,

                    m_Frustum = frustum,

                    m_Remove = m_RemoveProxyList.AsParallelWriter(),
                    m_Request = m_RequestProxyList.AsParallelWriter(),

                    m_Visible = m_VisibleList.AsParallelWriter(),
                    m_Invisible = m_InvisibleList.AsParallelWriter()
                };
                ScheduleAt(JobPosition.On, proxyJob, m_SortedCluster, 64);
            }

            #endregion

            return PresentationResult.Normal;
        }

        #region Jobs

        private struct ClusterUpdateJob : IJobParallelFor
        {
            [WriteOnly] public Cluster<ProxyTransformData>.ParallelWriter m_ClusterData;
            [ReadOnly, DeallocateOnJobCompletion] public NativeArray<ClusterUpdateRequest> m_Requests;

            public void Execute(int i)
            {
                if (m_Requests[i].transform.isDestroyQueued ||
                    m_Requests[i].transform.isDestroyed) return;

                m_Requests[i].transform.Ref.m_ClusterID = m_ClusterData.Update(m_Requests[i].id, m_Requests[i].translation);
            }
        }
        [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
        private struct ClusterJob : IJob
        {
            [ReadOnly] public Cluster<ProxyTransformData> m_ClusterData;
            [ReadOnly] public CameraFrustum m_Frustum;
            [WriteOnly] public NativeList<ClusterGroup<ProxyTransformData>> m_Output;

            public void Execute()
            {
                for (int i = 0; i < m_ClusterData.Length; i++)
                {
                    AABB box = new AABB(m_ClusterData[i].Translation, Cluster<ProxyTransformData>.c_ClusterRange);

                    if (m_Frustum.IntersectsBox(in box))
                    {
                        m_Output.Add(m_ClusterData[i]);
                    }
                }
            }
        }
        [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
        private struct ProxyJob : IJobParallelForDefer
        {
            [ReadOnly] public NativeArray<ClusterGroup<ProxyTransformData>> m_ActiveData;
            [ReadOnly] public NativeProxyData.UnsafeList List;

            [ReadOnly] public CameraFrustum m_Frustum;
            [WriteOnly]
            public NativeQueue<int>.ParallelWriter
                m_Remove,
                m_Request,

                m_Visible,
                m_Invisible;

            public void Execute(int i)
            {
                ClusterGroup<ProxyTransformData> clusterGroup = m_ActiveData[i];
                if (!clusterGroup.IsCreated) return;

                for (int j = 0; j < clusterGroup.Length; j++)
                {
                    if (!clusterGroup.HasElementAt(j)) continue;

                    if (clusterGroup[j] >= List.m_Length)
                    {
                        //$"?? {clusterGroup[j]} >= {List.m_Length}".ToLog();
                        throw new Exception();
                    }
                    ProxyTransformData data = List.ElementAt(clusterGroup[j]);

                    if (data.m_Prefab.Equals(PrefabReference.None)) continue;
                    if (!data.m_EnableCull)
                    {
                        if (data.m_ProxyIndex.Equals(-1) &&
                            !data.m_ProxyIndex.Equals(-2))
                        {
                            m_Request.Enqueue(data.m_Index);
                        }
                        continue;
                    }

                    if (m_Frustum.IntersectsBox(data.GetAABB(), 10))
                    {
                        if (data.m_ProxyIndex.Equals(-1) &&
                            !data.m_ProxyIndex.Equals(-2))
                        {
                            m_Request.Enqueue(data.m_Index);
                        }

                        if (!data.m_IsVisible)
                        {
                            m_Visible.Enqueue(data.m_Index);
                        }
                    }
                    else
                    {
                        if (!data.m_ProxyIndex.Equals(-1) &&
                            !data.m_ProxyIndex.Equals(-2))
                        {
                            m_Remove.Enqueue(data.m_Index);
                        }

                        if (data.m_IsVisible)
                        {
                            m_Invisible.Enqueue(data.m_Index);
                        }
                    }
                }

                //
            }
        }

        #endregion

        #endregion

        public ProxyTransform CreateNewPrefab(in PrefabReference prefab, in float3 pos, in quaternion rot, in float3 scale, in bool enableCull, in float3 center, in float3 size)
        {
            CoreSystem.Logger.ThreadBlock(nameof(CreateNewPrefab), ThreadInfo.Unity);

            CoreSystem.Logger.NotNull(m_RenderSystem, $"You've call this method too early or outside of PresentationSystem");

            ProxyTransform tr = m_ProxyData.Add(prefab, pos, rot, scale, enableCull, center, size);
            unsafe
            {
                m_ClusterIDRequests.Enqueue(new ClusterIDRequest(pos, tr.m_Index));
                tr.Pointer->m_ClusterID = ClusterID.Requested;
            }
            OnDataObjectCreated?.Invoke(tr);

            CoreSystem.Logger.Log(Channel.Proxy, true,
                $"ProxyTransform({prefab.GetObjectSetting().m_Name})" +
                $"has been created at {pos}");

            return tr;
        }
        public void Destroy(in ProxyTransform tr)
        {
            CoreSystem.Logger.ThreadBlock(nameof(Destroy), ThreadInfo.Unity);

            //OnDataObjectDestroy?.Invoke(tr);

            //if (tr.hasProxy && !tr.hasProxyQueued) RemoveProxy(tr);
            //else
            //{
            //    "in".ToLog();
            //}

            //unsafe
            //{
            //    //if (!tr.Pointer->m_ClusterID.Equals(ClusterID.Requested))
            //    {
            //        m_ClusterData.Remove(tr.Pointer->m_ClusterID);
            //    }
            //}
            //m_ProxyData.Remove(tr);

            unsafe
            {
                m_RequestDestories.Enqueue(tr.m_Index);
            }
            CoreSystem.Logger.Log(Channel.Proxy,
                $"Destroy called");
        }
        private struct ClusterIDRequest
        {
            public float3 translation;
            public int index;

            public ClusterIDRequest(float3 tr, int idx)
            {
                translation = tr;
                index = idx;
            }
        }
        private struct ClusterUpdateRequest
        {
            public ProxyTransform transform;
            public ClusterID id;
            public float3 translation;

            public ClusterUpdateRequest(ProxyTransform transform, ClusterID id, float3 tr)
            {
                this.transform = transform;
                this.id = id;
                translation = tr;
            }
        }

        #region Proxy Object Control

        private void AddProxy(ProxyTransform proxyTransform)
        {
            CoreSystem.Logger.ThreadBlock(nameof(GameObjectProxySystem.AddProxy), ThreadInfo.Unity);

            PrefabReference prefab = proxyTransform.prefab;

            if (!m_TerminatedProxies.TryGetValue(prefab, out Stack<RecycleableMonobehaviour> pool) ||
                    pool.Count == 0)
            {
                proxyTransform.SetProxy(-2);
                InstantiatePrefab(prefab, (other) =>
                {
                    if (proxyTransform.isDestroyed)
                    {
                        if (other.InitializeOnCall) other.Terminate();
                        other.transform.position = INIT_POSITION;

                        if (!m_TerminatedProxies.TryGetValue(prefab, out Stack<RecycleableMonobehaviour> pool))
                        {
                            pool = new Stack<RecycleableMonobehaviour>();
                            m_TerminatedProxies.Add(prefab, pool);
                        }
                        pool.Push(other);
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
                RecycleableMonobehaviour other = pool.Pop();
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

            if ((proxy.transform.position - (Vector3)proxyTransform.position).sqrMagnitude > .1f)
            {
                proxyTransform.position = proxy.transform.position;
                CoreSystem.Logger.LogWarning(Channel.Proxy,
                    "in-corrected translation found. Did you moved proxy transform directly?");
            }

            OnDataObjectProxyRemoved?.Invoke(proxyTransform, proxy);

            proxyTransform.SetProxy(ProxyTransform.ProxyNull);

            if (proxy.Activated) proxy.Terminate();

            if (!m_TerminatedProxies.TryGetValue(prefab, out Stack<RecycleableMonobehaviour> pool))
            {
                pool = new Stack<RecycleableMonobehaviour>();
                m_TerminatedProxies.Add(prefab, pool);
            }
            pool.Push(proxy);
            CoreSystem.Logger.Log(Channel.Proxy, true,
                    $"Prefab({prefab.GetObjectSetting().m_Name}) proxy removed.");
        }

        #endregion

        #region Instantiate Prefab From PrefabList

        /// <summary>
        /// 생성된 프록시 오브젝트들입니다. key 값은 <see cref="PrefabList.m_ObjectSettings"/> 인덱스(<see cref="PrefabReference"/>)이며 value 배열은 상태 구분없이(사용중이던 아니던) 실제 생성된 모노 객체입니다.
        /// </summary>
        internal readonly Dictionary<PrefabReference, List<RecycleableMonobehaviour>> m_Instances = new Dictionary<PrefabReference, List<RecycleableMonobehaviour>>();
        private readonly Dictionary<PrefabReference, Stack<RecycleableMonobehaviour>> m_TerminatedProxies = new Dictionary<PrefabReference, Stack<RecycleableMonobehaviour>>();

        private sealed class PrefabRequester
        {
            GameObjectProxySystem m_ProxySystem;
            SceneSystem SceneSystem => m_ProxySystem.m_SceneSystem;
            EventSystem EventSystem => m_ProxySystem.m_EventSystem;

            Scene m_RequestedScene;

            public void Setup(GameObjectProxySystem proxySystem, PrefabReference prefabIdx, Vector3 pos, Quaternion rot,
                Action<RecycleableMonobehaviour> onCompleted)
            {
                //var prefabInfo = PrefabList.Instance.ObjectSettings[prefabIdx];
                var prefabInfo = prefabIdx.GetObjectSetting();
                if (!proxySystem.m_TerminatedProxies.TryGetValue(prefabIdx, out var pool))
                {
                    pool = new Stack<RecycleableMonobehaviour>();
                    proxySystem.m_TerminatedProxies.Add(prefabIdx, pool);
                }

                if (pool.Count > 0)
                {
                    RecycleableMonobehaviour obj = pool.Pop();
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
                m_RequestedScene = SceneSystem.CurrentScene;

                Transform parent;
                if (prefabInfo.m_IsWorldUI)
                {
                    parent = PresentationSystem<WorldCanvasSystem>.System.Canvas.transform;
                }
                else
                {
                    if (CoreSystem.InstanceGroupTr == null) CoreSystem.InstanceGroupTr = new GameObject("InstanceSystemGroup").transform;

                    parent = CoreSystem.InstanceGroupTr;
                }

                AssetReference refObject = prefabInfo.m_RefPrefab;

                //var oper = prefabInfo.m_RefPrefab.InstantiateAsync(pos, rot, CoreSystem.InstanceGroupTr);
                var oper = prefabInfo.m_RefPrefab.InstantiateAsync(pos, rot, parent);
                oper.Completed += (other) =>
                {
                    Scene currentScene = SceneSystem.CurrentScene;
                    if (!currentScene.Equals(m_RequestedScene))
                    {
                        CoreSystem.Logger.LogWarning(Channel.Proxy, $"{other.Result.name} is returned because Scene has been changed");
                        refObject.ReleaseInstance(other.Result);
                        return;
                    }

                    if (prefabInfo.m_IsWorldUI)
                    {
                        other.Result.layer = 5;
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
                    recycleable.m_EventSystem = EventSystem;

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
            PoolContainer<PrefabRequester>.Dequeue().Setup(this, prefab, position, rotation, onCompleted);
        }

        #endregion
    }
}
