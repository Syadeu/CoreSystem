﻿using Syadeu.Database;
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
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using AABB = Syadeu.Database.AABB;

namespace Syadeu.Presentation.Proxy
{
    /// <summary>
    /// ** 주의: 어떠한 상황에서든 이 시스템에 직접 접근하는 것은 권장되지 않습니다. **<br/>
    /// </summary>
    internal sealed class GameObjectProxySystem : PresentationSystemEntity<GameObjectProxySystem>
    {
        public static readonly Vector3 INIT_POSITION = new Vector3(-9999, -9999, -9999);
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
        public Queue<int>
            m_OverrideRequestProxies = new Queue<int>();

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
            ReleaseAllPrefabs();

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

            ReleaseAllPrefabs();

            m_ProxyData.Dispose();
            m_ClusterData.Dispose();
            m_ProxyData = new NativeProxyData(c_InitialMemorySize, Allocator.Persistent);
            m_ClusterData = new Cluster<ProxyTransformData>(c_InitialMemorySize);
            m_LoadingLock = false;

            void DestroyTransform(ProxyTransform tr)
            {
                OnDataObjectDestroy?.Invoke(tr);
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
            if (!(ev.transform is ProxyTransform transform)) return;
            
            if (transform.isDestroyed || transform.isDestroyQueued) return;

            if (!transform.Pointer->m_ClusterID.Equals(ClusterID.Requested))
            {
                //m_ClusterData.Update(in ev.transform.Pointer->m_ClusterID, ev.transform.position);
                m_ClusterUpdates.Enqueue(new ClusterUpdateRequest(transform, transform.Pointer->m_ClusterID, ev.transform.position));
            }

            if (!transform.hasProxy || transform.hasProxyQueued) return;

            RecycleableMonobehaviour proxy = transform.proxy;
            proxy.transform.position = ev.transform.position;
            proxy.transform.rotation = ev.transform.rotation;
            proxy.transform.localScale = ev.transform.scale;
        }

        private NativeList<ClusterGroup<ProxyTransformData>> m_SortedCluster;
        protected override PresentationResult AfterPresentation()
        {
            //const int c_ChunkSize = 100;

            if (m_LoadingLock) return base.AfterPresentation();

            CameraFrustum frustum = m_RenderSystem.GetRawFrustum();

            int overrideRequestProxies = m_OverrideRequestProxies.Count;
            for (int i = 0; i < overrideRequestProxies; i++)
            {
                int index = m_OverrideRequestProxies.Dequeue();
                ProxyTransform tr = m_ProxyData[index];
                if (tr.isDestroyed || tr.isDestroyQueued)
                {
                    continue;
                }

                m_RequestProxyList.Enqueue(index);
            }

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

                if (tr.hasProxy && !tr.hasProxyQueued)
                {
                    RecycleableMonobehaviour proxy = RemoveProxy(tr);

                    var intersection = frustum.IntersectsSphere(proxy.transform.position, proxy.transform.localScale.sqrMagnitude, 1);

                    if ((intersection & IntersectionType.Intersects) == IntersectionType.Intersects ||
                        (intersection & IntersectionType.Contains) == IntersectionType.Contains)
                    {
                        proxy.transform.position = INIT_POSITION;
                    }
                }
                if (tr.isVisible)
                {
                    tr.isVisible = false;
                    OnDataObjectInvisible?.Invoke(tr);
                }

                OnDataObjectDestroy?.Invoke(tr);

                unsafe
                {
                    ClusterID id = tr.Pointer->m_ClusterID;
                    if (id.Equals(ClusterID.Requested))
                    {
                        int tempCount = m_ClusterIDRequests.Count;
                        for (int a = 0; a < tempCount; a++)
                        {
                            var tempID = m_ClusterIDRequests.Dequeue();
                            if (tempID.index.Equals(tr.Pointer->m_Index))
                            {
                                break;
                            }
                            else m_ClusterIDRequests.Enqueue(tempID);
                        }
                    }
                    else m_ClusterData.Remove(id);
                }
                m_ProxyData.Remove(tr);
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

            if (m_SortedCluster.IsCreated)
            {
                //m_SortedCluster.Clear();
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
                NativeProxyData.UnsafeList list = m_ProxyData.List;

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
                ProxyTransform tr = m_Requests[i].transform;

                if (tr.isDestroyQueued ||
                    tr.isDestroyed) return;

                tr.Ref.m_ClusterID = m_ClusterData.Update(m_Requests[i].id, m_Requests[i].translation);
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
                int a = 0;
                for (int i = 0; i < m_ClusterData.Length; i++)
                {
                    //AABB box = new AABB(m_ClusterData[i].Translation, Cluster<ProxyTransformData>.c_ClusterRange);

                    if (m_Frustum.IntersectsBox(m_ClusterData[i].AABB))
                    {
                        if (a < m_Output.Length)
                        {
                            m_Output[a] = m_ClusterData[i];
                            a++;
                        }
                        else
                        {
                            m_Output.Add(m_ClusterData[i]);
                        }
                    }
                }

                for (int i = a; i < m_Output.Length; i++)
                {
                    m_Output[i] = ClusterGroup<ProxyTransformData>.Empty;
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
                    if (data.destroyed)
                    {
                        clusterGroup.RemoveAt(j);
                        continue;
                    }

                    if (!data.m_EnableCull && !data.m_Prefab.Equals(PrefabReference.None))
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
                        if (!data.m_Prefab.Equals(PrefabReference.None) &&
                            data.m_ProxyIndex.Equals(-1) &&
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
                        if (!data.m_Prefab.Equals(PrefabReference.None) &&
                            !data.m_ProxyIndex.Equals(-1) &&
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

        public ProxyTransform CreateNewPrefab(in PrefabReference<GameObject> prefab, 
            in float3 pos, in quaternion rot, in float3 scale, in bool enableCull, 
            in float3 center, in float3 size,
            bool gpuInstanced = false)
        {
            CoreSystem.Logger.ThreadBlock(nameof(CreateNewPrefab), ThreadInfo.Unity);

            CoreSystem.Logger.NotNull(m_RenderSystem, $"You've call this method too early or outside of PresentationSystem");

            ProxyTransform tr;
            if (prefab.IsNone())
            {
                tr = m_ProxyData.Add(PrefabReference.None, pos, rot, scale, enableCull, center, size, gpuInstanced);
            }
            else if (!prefab.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Proxy,
                    $"Trying to create an invalid prefab proxy. This is not allowed. Replaced to empty.");

                tr = m_ProxyData.Add(PrefabReference.None, pos, rot, scale, enableCull, center, size, gpuInstanced);
            }
            else tr = m_ProxyData.Add(prefab, pos, rot, scale, enableCull, center, size, gpuInstanced);

            unsafe
            {
                m_ClusterIDRequests.Enqueue(new ClusterIDRequest(pos, tr.m_Index));
                tr.Pointer->m_ClusterID = ClusterID.Requested;
            }
            OnDataObjectCreated?.Invoke(tr);

            if (!enableCull && !prefab.Equals(PrefabReference<GameObject>.None))
            {
                m_OverrideRequestProxies.Enqueue(tr.m_Index);
            }

            CoreSystem.Logger.Log(Channel.Proxy, true,
                $"ProxyTransform(" +
                $"{(prefab.GetObjectSetting() != null ? prefab.GetObjectSetting().m_Name : "EMPTY")})" +
                $"has been created at {pos}");

            return tr;
        }
        public void Destroy(in ProxyTransform tr)
        {
            CoreSystem.Logger.ThreadBlock(nameof(Destroy), ThreadInfo.Unity);

            unsafe
            {
                if ((*tr.m_Pointer)[tr.m_Index]->m_DestroyQueued)
                {
                    CoreSystem.Logger.LogError(Channel.Proxy, 
                        "Cannot destroy this proxy because it is already destroyed.");
                    return;
                }

                (*tr.m_Pointer)[tr.m_Index]->m_EnableCull = false;
                (*tr.m_Pointer)[tr.m_Index]->m_DestroyQueued = true;
            }

            m_RequestDestories.Enqueue(tr.m_Index);
            CoreSystem.Logger.Log(Channel.Proxy,
                $"Destroy called");
        }

        #region Proxy Object Control

        unsafe private class GPUInstancing
        {
            public PrefabReference<GameObject> prefab;

            public class Item
            {
                public Material[] Materials;
                public Mesh Mesh;
                public Bounds Bounds;

                public TRS LocalTRS;

                public ComputeBuffer ComputeBuffer;
            }

            public List<ProxyTransform> transforms;
            public List<Item> items;

            ComputeBuffer ComputeBuffer;

            public void Init(PrefabReference<GameObject> obj)
            {
                prefab = obj;

                if (prefab.Asset == null)
                {
                    AsyncOperationHandle<GameObject> handle = prefab.LoadAssetAsync();
                    handle.Completed += InitializeAsync;
                }
                else Initialize(prefab.Asset);
            }

            private void InitializeAsync(AsyncOperationHandle<GameObject> handle)
            {
                Initialize(handle.Result);
            }
            private void Initialize(GameObject obj)
            {
                Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

                items = new List<Item>();

                for (int i = 0; i < renderers.Length; i++)
                {
                    Mesh mesh;
                    if (renderers[i] is MeshRenderer meshRenderer)
                    {
                        mesh = UnityEngine.Object.Instantiate(meshRenderer.GetComponent<MeshFilter>().sharedMesh);
                    }
                    else
                    {
                        "error not support".ToLogError();
                        break;
                    }

                    Material[] 
                        localMats = renderers[i].sharedMaterials,
                        instancedMats = new Material[localMats.Length];
                    for (int a = 0; a < localMats.Length; a++)
                    {
                        instancedMats[i] = UnityEngine.Object.Instantiate(localMats[i]);
                    }


                    Item item = new Item()
                    {
                        Mesh = mesh,
                        Materials = instancedMats,
                        Bounds = renderers[i].bounds,
                        LocalTRS = new TRS(renderers[i].transform),

                        ComputeBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments)
                    };

                    items.Add(item);
                }
            }

            public void Draw()
            {
                for (int i = 0; i < transforms.Count; i++)
                {
                    TRS trs = items[i].LocalTRS.Project(new TRS(transforms[i]));


                }

                for (int i = 0; i < items.Count; i++)
                {
                    // Argument buffer used by DrawMeshInstancedIndirect.
                    uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
                    // Arguments for drawing mesh.
                    // 0 == number of triangle indices, 1 == population, others are only relevant if drawing submeshes.
                    args[0] = (uint)items[i].Mesh.GetIndexCount(0);
                    args[1] = (uint)transforms.Count;
                    args[2] = (uint)items[i].Mesh.GetIndexStart(0);
                    args[3] = (uint)items[i].Mesh.GetBaseVertex(0);

                    items[i].ComputeBuffer.SetData(args);

                    foreach (var material in items[i].Materials)
                    {
                        Graphics.DrawMeshInstancedIndirect(
                            mesh:           items[i].Mesh,
                            submeshIndex:   0,
                            material:       material,
                            bounds:         items[i].Bounds,
                            bufferWithArgs: items[i].ComputeBuffer
                            );
                    }
                }
            }
        }

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
        private RecycleableMonobehaviour RemoveProxy(ProxyTransform proxyTransform)
        {
            PrefabReference prefab = proxyTransform.prefab;
            RecycleableMonobehaviour proxy = proxyTransform.proxy;

            if ((proxy.transform.position - (Vector3)proxyTransform.position).sqrMagnitude > .1f)
            {
                unsafe
                {
                    proxyTransform.Pointer->m_Translation = proxy.transform.position;
                }
                CoreSystem.Logger.LogError(Channel.Proxy,
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
            return proxy;
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

            //AssetReference refObject;
            PrefabReference m_PrefabIdx;
            Scene m_RequestedScene;
            Action<RecycleableMonobehaviour> m_OnCompleted;

            public void Setup(GameObjectProxySystem proxySystem, PrefabReference prefabIdx, Vector3 pos, Quaternion rot,
                Action<RecycleableMonobehaviour> onCompleted)
            {
                CoreSystem.Logger.True(prefabIdx.IsValid(), nameof(PrefabReference) + "not valid");
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
                
                Transform parent;
                if (prefabInfo.m_IsWorldUI)
                {
                    parent = PresentationSystem<WorldCanvasSystem>.System.Canvas.transform;
                }
                else
                {
                    CoreSystem.Logger.NotNull(SceneSystem.SceneInstanceFolder);
                    parent = SceneSystem.SceneInstanceFolder;
                }

                //refObject = prefabInfo.m_RefPrefab;
                m_PrefabIdx = prefabIdx;
                m_RequestedScene = SceneSystem.CurrentScene;
                m_OnCompleted = onCompleted;

                if (prefabInfo.m_IsRuntimeObject)
                {
                    CreatePrefab(prefabInfo.m_Prefab, parent);
                    return;
                }
                var oper = prefabInfo.InstantiateAsync(pos, rot, parent);
                oper.Completed += CreatePrefab;
            }
            private void CreatePrefab(AsyncOperationHandle<GameObject> other)
            {
                Scene currentScene = SceneSystem.CurrentScene;
                if (!currentScene.Equals(m_RequestedScene))
                {
                    CoreSystem.Logger.LogWarning(Channel.Proxy, $"{other.Result.name} is returned because Scene has been changed");
                    m_PrefabIdx.ReleaseInstance(other.Result);
                    return;
                }

                CreatePrefab(other.Result);
            }
            private void CreatePrefab(GameObject Result, Transform parent = null)
            {
                if (parent != null)
                {
                    Result.transform.SetParent(parent);
                }

                if (m_PrefabIdx.GetObjectSetting().m_IsWorldUI)
                {
                    Result.layer = 5;
                }

                RecycleableMonobehaviour recycleable = Result.GetComponent<RecycleableMonobehaviour>();
                if (recycleable == null)
                {
                    recycleable = Result.AddComponent<ManagedRecycleObject>();
                }

                if (!m_ProxySystem.m_Instances.TryGetValue(m_PrefabIdx, out List<RecycleableMonobehaviour> instances))
                {
                    instances = new List<RecycleableMonobehaviour>();
                    m_ProxySystem.m_Instances.Add(m_PrefabIdx, instances);
                }

                recycleable.m_Idx = instances.Count;

                instances.Add(recycleable);

                recycleable.InternalOnCreated();
                if (recycleable.InitializeOnCall) recycleable.Initialize();
                m_OnCompleted?.Invoke(recycleable);

                m_ProxySystem = null;
                m_PrefabIdx = PrefabReference.Invalid;
                m_OnCompleted = null;
                PoolContainer<PrefabRequester>.Enqueue(this);
            }
        }

        private void InstantiatePrefab(PrefabReference prefab, Action<RecycleableMonobehaviour> onCompleted)
            => InstantiatePrefab(prefab, INIT_POSITION, Quaternion.identity, onCompleted);
        private void InstantiatePrefab(PrefabReference prefab, Vector3 position, Quaternion rotation, Action<RecycleableMonobehaviour> onCompleted)
        {
            PoolContainer<PrefabRequester>.Dequeue().Setup(this, prefab, position, rotation, onCompleted);
        }
        private void ReleaseAllPrefabs()
        {
            foreach (var item in m_Instances)
            {
                var prefab = item.Key.GetObjectSetting();
                for (int i = 0; i < item.Value.Count; i++)
                {
                    prefab.ReleaseInstance(item.Value[i].gameObject);
                }
            }
            m_Instances.Clear();
            m_TerminatedProxies.Clear();
        }

        #endregion

        #region Inner Classes

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

        #endregion
    }
}
