﻿// Copyright 2021 Seung Ha Kim
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
using Syadeu.Collections.Buffer;
using Syadeu.Collections.Proxy;
using Syadeu.Internal;
using Syadeu.Mono;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Render;
using Syadeu.Presentation.Render.LowLevel;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

namespace Syadeu.Presentation.Proxy
{
    /// <summary>
    /// ** 주의: 어떠한 상황에서든 이 시스템에 직접 접근하는 것은 권장되지 않습니다. **<br/>
    /// </summary>
    internal sealed class GameObjectProxySystem : PresentationSystemEntity<GameObjectProxySystem>,
        INotifySystemModule<ProxyMessageModule>
    {
        public static readonly Vector3 INIT_POSITION = new Vector3(-9999, -9999, -9999);
        private const int c_InitialMemorySize = 1024;

        const string 
            c_ProxyCreated = "ProxyTransform({0}) has been created at {1}";

        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;
        public override bool EnableTransformPresentation => true;

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
        private NativeList<ClusterUpdateRequest>
                m_TempSortedUpdateList;
        private NativeQueue<ClusterIDRequest>
                m_ClusterIDRequests;
        private NativeList<ClusterGroup<ProxyTransformData>> 
                m_SortedCluster;

        private Unity.Collections.NativeReference<int> m_ProxyClusterCounter;

        private NativeList<int> m_VisibleTransforms;
#pragma warning restore IDE0090 // Use 'new(...)'
        public Queue<int>
            m_OverrideRequestProxies = new Queue<int>();
        private readonly Dictionary<int, List<Transform>> m_ConnectedTransforms = new Dictionary<int, List<Transform>>();

        private static readonly Unity.Profiling.ProfilerMarker
            s_HandleOverrideProxyRequestsMarker = new Unity.Profiling.ProfilerMarker("Handle Override Proxy Requests"),
            s_HandleCreateProxiesMarker = new Unity.Profiling.ProfilerMarker("Handle Create Proxies"),
            s_HandleRemoveProxiesMarker = new Unity.Profiling.ProfilerMarker("Handle Remove Proxies"),
            s_HandleVisibleProxiesMarker = new Unity.Profiling.ProfilerMarker("Handle Visible Proxies"),
            s_HandleInvisibleProxiesMarker = new Unity.Profiling.ProfilerMarker("Handle Invisible Proxies"),
            s_HandleDestroyProxiesMarker = new Unity.Profiling.ProfilerMarker("Handle Destroy Proxies"),

            s_HandleApplyClusterIDMarker = new Unity.Profiling.ProfilerMarker("Handle Apply ClusterID"),

            s_HandleJobsMarker = new Unity.Profiling.ProfilerMarker("Handle Jobs"),
            s_HandleScheduleClusterUpdateMarker = new Unity.Profiling.ProfilerMarker("Handle Schedule Cluster Update"),
            s_HandleScheduleProxyUpdateMarker = new Unity.Profiling.ProfilerMarker("Handle Schedule Proxy Update");

        private NativeSlice<int> VisibleTransformIndices
        {
            get
            {
                return m_VisibleTransforms.AsArray().Slice(0, m_VisibleTransforms.Length);
            }
        }

        private SceneSystem m_SceneSystem;
        private RenderSystem m_RenderSystem;
        private EventSystem m_EventSystem;

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
            m_TempSortedUpdateList = new NativeList<ClusterUpdateRequest>(Allocator.Persistent);
            m_ClusterIDRequests = new NativeQueue<ClusterIDRequest>(Allocator.Persistent);

            m_SortedCluster = new NativeList<ClusterGroup<ProxyTransformData>>(1024, Allocator.Persistent);
            m_ProxyClusterCounter = new Unity.Collections.NativeReference<int>(0, AllocatorManager.Persistent);

            m_VisibleTransforms = new NativeList<int>(512, AllocatorManager.Persistent);

            return base.OnInitialize();
        }
        protected override PresentationResult OnInitializeAsync()
        {
            m_ProxyData = new NativeProxyData(c_InitialMemorySize, Allocator.Persistent);
            m_ClusterData = new Cluster<ProxyTransformData>(c_InitialMemorySize);

            RequestSystem<DefaultPresentationGroup, SceneSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, RenderSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, EventSystem>(Bind);

            return base.OnInitializeAsync();
        }
        protected override void OnDispose()
        {
            ReleaseAllPrefabs();

            m_RequestDestories.Dispose();

            m_RequestProxyList.Dispose();
            m_RemoveProxyList.Dispose();
            m_VisibleList.Dispose();
            m_InvisibleList.Dispose();

            m_ClusterUpdates.Dispose();
            m_TempSortedUpdateList.Dispose();
            m_ClusterIDRequests.Dispose();

            m_ProxyData.For((tr) =>
            {
                OnDataObjectDestroy?.Invoke(tr);
            });
            m_ProxyData.Dispose();
            m_ClusterData.Dispose();

            m_SortedCluster.Dispose();
            m_ProxyClusterCounter.Dispose();

            m_VisibleTransforms.Dispose();
        }

        #region Binds
        
        private void Bind(SceneSystem other)
        {
            m_SceneSystem = other;
            m_SceneSystem.OnLoadingEnter += M_SceneSystem_OnLoadingEnter;
        }
        private unsafe void M_SceneSystem_OnLoadingEnter()
        {
            CoreSystem.Logger.Log(LogChannel.Proxy, true,
                "Scene on loading enter lambda excute");

            m_ProxyData.For(RemoveProxy);

            m_RequestProxyList.Clear();
            m_RemoveProxyList.Clear();
            m_VisibleList.Clear();
            m_InvisibleList.Clear();

            ReleaseAllPrefabs();

            void RemoveProxy(ProxyTransform tr)
            {
                ProxyTransformData* data = m_ProxyData.List[tr.m_Index];

                if (data->m_ProxyIndex.Equals(ProxyTransform.ProxyNull) ||
                    data->m_ProxyIndex.Equals(ProxyTransform.ProxyQueued))
                {
                    return;
                }

                this.RemoveProxy(data);
            }
        }
        private void Bind(RenderSystem other)
        {
            m_RenderSystem = other;
        }
        private void Bind(EventSystem other)
        {
            m_EventSystem = other;

            m_EventSystem.AddEvent<OnTransformChangedEvent>(OnTransformChanged);
        }
        #endregion

        unsafe private void OnTransformChanged(OnTransformChangedEvent ev)
        {
            ProxyTransformData* data = m_ProxyData.List[ev.transform.m_Index];

            if (!data->m_IsOccupied ||
                data->m_Generation != data->m_Generation)
            {
                CoreSystem.Logger.LogError(LogChannel.Proxy,
                    $"Validation error. Target transform is not valid.");
                return;
            }

            if (!data->m_IsOccupied || data->m_DestroyQueued)
            {
                $"no update rtn {ev.transform.m_Index}".ToLog();
                return;
            }

            //UpdateProxyTransform(in data);

            //if (transform.isDestroyed || transform.isDestroyQueued) return;

            if (!data->clusterID.Equals(ClusterID.Requested))
            {
                m_ClusterUpdates.Enqueue(new ClusterUpdateRequest(ev.transform, data->clusterID, data->m_Translation));
            }

            if (!data->m_ProxyIndex.Equals(ProxyTransform.ProxyNull) &&
                !data->m_ProxyIndex.Equals(ProxyTransform.ProxyQueued))
            {
                int2 proxyIndex = data->m_ProxyIndex;

                if (!m_Instances.ContainsKey(proxyIndex.x))
                {
                    CoreSystem.Logger.LogError(LogChannel.Proxy,
                        $"Fatal error. {((PrefabReference)proxyIndex.x).GetObjectSetting()?.Name} is not in instance list 1.");

                    return;
                }
                else if (m_Instances[proxyIndex.x].Count <= proxyIndex.y)
                {
                    CoreSystem.Logger.LogError(LogChannel.Proxy,
                        $"Fatal error. {((PrefabReference)proxyIndex.x).GetObjectSetting()?.Name} is not in instance list 2.");

                    return;
                }

                RecycleableMonobehaviour proxy = m_Instances[proxyIndex.x][proxyIndex.y];
                Rigidbody rigidbody = proxy.GetComponent<Rigidbody>();

                Transform transform = proxy.transform;
                transform.position = data->m_Translation;
                transform.rotation = data->m_Rotation;
                transform.localScale = data->m_Scale;

                //if (rigidbody != null)
                //{
                //    rigidbody.position = data->m_Translation;
                //    rigidbody.rotation = data->m_Rotation;
                //}
            }

            UpdateConnectedTransforms(ev.transform);
        }
        unsafe private void UpdateProxyTransform(in ProxyTransformData* data)
        {
            if (!data->m_IsOccupied) return;

            if (data->clusterID.Equals(ClusterID.Requested))
            {
                //m_ClusterUpdates.Enqueue(new ClusterUpdateRequest(transform, transform.Pointer->clusterID, transform.position));
            }
        }

        unsafe protected override PresentationResult TransformPresentation()
        {
            //const int c_ChunkSize = 100;

            if (m_SceneSystem.IsSceneLoading) return base.TransformPresentation();

            CameraFrustum frustum = m_RenderSystem.GetRawFrustum();

            #region Override Proxy Requests

            using (s_HandleOverrideProxyRequestsMarker.Auto())
            {
                int overrideRequestProxies = m_OverrideRequestProxies.Count;
                for (int i = 0; i < overrideRequestProxies; i++)
                {
                    int index = m_OverrideRequestProxies.Dequeue();
                    ProxyTransform tr = m_ProxyData[index];
                    ProxyTransformData* data = m_ProxyData.List[tr.m_Index];

                    if (!data->m_IsOccupied || data->m_DestroyQueued)
                    {
                        continue;
                    }
                    else if (
                        !data->m_ProxyIndex.Equals(ProxyTransform.ProxyNull) ||
                        data->m_ProxyIndex.Equals(ProxyTransform.ProxyQueued))
                    {
                        CoreSystem.Logger.LogError(LogChannel.Proxy,
                            $"Already have proxy({data->m_Prefab.GetObjectSetting()?.Name}):{data->m_ProxyIndex}");
                        continue;
                    }

                    m_RequestProxyList.Enqueue(index);
                }
            }

            #endregion

            #region Create / Remove Proxy

            using (s_HandleCreateProxiesMarker.Auto())
            {
                int requestProxyCount = m_RequestProxyList.Count;
                for (int i = 0; i < requestProxyCount; i++)
                {
                    //if (i != 0 && i % c_ChunkSize == 0) break;

                    ProxyTransform tr = m_ProxyData[m_RequestProxyList.Dequeue()];
                    ProxyTransformData* data = m_ProxyData.List[tr.m_Index];

                    if (!data->m_IsOccupied || data->m_DestroyQueued)
                    {
                        CoreSystem.Logger.LogError(LogChannel.Proxy, $"1 destroyed transform");
                        continue;
                    }
                    else if (
                        !data->m_ProxyIndex.Equals(ProxyTransform.ProxyNull) ||
                        data->m_ProxyIndex.Equals(ProxyTransform.ProxyQueued) ||
                        data->m_ProxyIndex.Equals(-3))
                    {
                        CoreSystem.Logger.LogError(LogChannel.Proxy, 
                            $"Already have proxy({data->m_Prefab.GetObjectSetting()?.Name}):{data->m_ProxyIndex}");
                        continue;
                    }

                    AddProxy(data);
                }
            }
            
            using (s_HandleRemoveProxiesMarker.Auto())
            {
                int removeProxyCount = m_RemoveProxyList.Count;
                for (int i = 0; i < removeProxyCount; i++)
                {
                    //if (i != 0 && i % c_ChunkSize == 0) break;

                    ProxyTransform tr = m_ProxyData[m_RemoveProxyList.Dequeue()];
                    ProxyTransformData* data = m_ProxyData.List[tr.m_Index];

                    if (!data->m_IsOccupied || data->m_DestroyQueued)
                    {
                        CoreSystem.Logger.LogError(LogChannel.Proxy, $"2 destroyed transform");
                        continue;
                    }
                    else if (
                        data->m_ProxyIndex.Equals(ProxyTransform.ProxyNull) ||
                        data->m_ProxyIndex.Equals(ProxyTransform.ProxyQueued))
                    {
                        CoreSystem.Logger.LogError(LogChannel.Proxy,
                            $"Does not have any proxy");
                        continue;
                    }

                    RemoveProxy(data);
                }
            }

            #endregion

            #region Visible / Invisible

            using (s_HandleVisibleProxiesMarker.Auto())
            {
                int visibleCount = m_VisibleList.Count;
                for (int i = 0; i < visibleCount; i++)
                {
                    //if (i != 0 && i % c_ChunkSize == 0) break;

                    ProxyTransform tr = m_ProxyData[m_VisibleList.Dequeue()];
                    ref ProxyTransformData data = ref m_ProxyData.ElementAt(tr.m_Index);

                    if (!data.m_IsOccupied || data.m_DestroyQueued) continue;

                    data.m_IsVisible = true;
                    OnDataObjectVisible?.Invoke(tr);

                    m_VisibleTransforms.Add(data.m_Index);

                    int2 proxyIdx = data.m_ProxyIndex;
                    if (!proxyIdx.Equals(ProxyTransform.ProxyNull) &&
                        !proxyIdx.Equals(ProxyTransform.ProxyQueued) &&
                        !proxyIdx.Equals(ProxyTransform.ProxyGPUInstanced))
                    {
                        m_Instances[proxyIdx.x][proxyIdx.y].InternalOnVisible();
                    }
                }
            }
            
            using (s_HandleInvisibleProxiesMarker.Auto())
            {
                int invisibleCount = m_InvisibleList.Count;
                for (int i = 0; i < invisibleCount; i++)
                {
                    //if (i != 0 && i % c_ChunkSize == 0) break;

                    ProxyTransform tr = m_ProxyData[m_InvisibleList.Dequeue()];
                    ref ProxyTransformData data = ref m_ProxyData.ElementAt(tr.m_Index);

                    if (!data.m_IsOccupied || data.m_DestroyQueued) continue;

                    data.m_IsVisible = false;
                    OnDataObjectInvisible?.Invoke(tr);

                    m_VisibleTransforms.RemoveForSwapBack(data.m_Index);

                    int2 proxyIdx = data.m_ProxyIndex;
                    if (!proxyIdx.Equals(ProxyTransform.ProxyNull) &&
                        !proxyIdx.Equals(ProxyTransform.ProxyQueued) &&
                        !proxyIdx.Equals(ProxyTransform.ProxyGPUInstanced))
                    {
                        m_Instances[proxyIdx.x][proxyIdx.y].InternalOnInvisible();
                    }
                }
            }

            #endregion

            #region Destroy

            using (s_HandleDestroyProxiesMarker.Auto())
            {
                int destroyCount = m_RequestDestories.Count;
                for (int i = 0; i < destroyCount; i++)
                {
                    ProxyTransform tr = m_ProxyData[m_RequestDestories.Dequeue()];
                    if (tr.isDestroyed)
                    {
                        CoreSystem.Logger.LogError(LogChannel.Proxy,
                            $"Already destroyed");
                        continue;
                    }

                    InternalDestory(in tr, in frustum);
                }
            }

            #endregion

            #region Apply ClusterID Requests

            using (s_HandleApplyClusterIDMarker.Auto())
            {
                int clusterIDRequestCount = m_ClusterIDRequests.Count;
                for (int i = 0; i < clusterIDRequestCount; i++)
                {
                    var temp = m_ClusterIDRequests.Dequeue();
                    var id = m_ClusterData.Add(temp.translation, temp.index);

                    m_ProxyData[temp.index].Ref.clusterID = id;
                }
            }

            #endregion

            #region Jobs

            using (s_HandleJobsMarker.Auto())
            {
                bool updateCluster = false;
                using (s_HandleScheduleClusterUpdateMarker.Auto())
                {
                    updateCluster = m_ClusterData.HasEntries;

                    if (m_ClusterUpdates.Count > 0)
                    {
                        NativeArray<ClusterUpdateRequest> requests = m_ClusterUpdates.ToArray(Allocator.TempJob);
                        m_ClusterUpdates.Clear();
                        m_TempSortedUpdateList.Clear();

                        ClusterUpdateSortJob clusterUpdateSortJob = new ClusterUpdateSortJob
                        {
                            m_ClusterData = m_ClusterData,
                            m_Request = requests,
                            m_SortedRequests = m_TempSortedUpdateList
                        };
                        ScheduleAt(JobPosition.On, clusterUpdateSortJob);

                        ClusterUpdateJob clusterUpdateJob = new ClusterUpdateJob
                        {
                            m_ClusterData = m_ClusterData.AsParallelWriter(),
                            m_Requests = m_TempSortedUpdateList.AsDeferredJobArray()
                        };
                        ScheduleAt(JobPosition.On, clusterUpdateJob, m_TempSortedUpdateList);
                    }

                    if (updateCluster)
                    {
                        m_SortedCluster.Clear();
                        //var deferredSortedCluster = m_SortedCluster.AsDeferredJobArray();
                        ClusterJob clusterJob = new ClusterJob
                        {
                            m_ClusterData = m_ClusterData,
                            m_Frustum = frustum,
                            m_Output = m_SortedCluster.AsParallelWriter(),

                            m_Count = (int*)m_ProxyClusterCounter.GetUnsafePtrWithoutChecks()
                        };
                        ScheduleAt(JobPosition.On, clusterJob, m_ClusterData.Length);
                    }
                }

                if (updateCluster)
                {
                    using (s_HandleScheduleProxyUpdateMarker.Auto())
                    unsafe
                    {
                        ref NativeProxyData.UnsafeList list = ref m_ProxyData.List;

                        ProxyJob proxyJob = new ProxyJob
                        {
                            m_Count = (int*)m_ProxyClusterCounter.GetUnsafeReadOnlyPtr(),
                            m_ActiveData = m_SortedCluster.AsDeferredJobArray(),
                            List = list,

                            m_Frustum = frustum,

                            m_Remove = m_RemoveProxyList.AsParallelWriter(),
                            m_Request = m_RequestProxyList.AsParallelWriter(),

                            m_Visible = m_VisibleList.AsParallelWriter(),
                            m_Invisible = m_InvisibleList.AsParallelWriter()
                        };
                        ScheduleAt(JobPosition.On, proxyJob, m_SortedCluster, 64);

                        //UpdateChildDependencies updateChild = new UpdateChildDependencies
                        //{
                        //    List = list
                        //};
                        //ScheduleAt(JobPosition.On, updateChild, (int)list.m_Length, 64);
                    }
                }
            }

            #endregion

            return PresentationResult.Normal;
        }

        private unsafe void InternalDestroyChildHierarchy(in ProxyTransformData* parent, in CameraFrustum frustum)
        {
            for (int i = parent->m_ChildIndices.Length - 1; i >= 0; i--)
            {
                ProxyTransformData* child = m_ProxyData.List[parent->m_ChildIndices[i]];

                if (!child->m_IsOccupied || child->m_DestroyQueued)
                {
                    continue;
                }

                child->m_ParentIndex = -1;

                ProxyTransform childTr = m_ProxyData[parent->m_ChildIndices[i]];
                InternalDestory(in childTr, in frustum);

                parent->m_ChildIndices.RemoveAt(i);
            }
        }
        private unsafe void InternalDestory(in ProxyTransform tr, in CameraFrustum frustum)
        {
            ProxyTransformData* data = m_ProxyData.List[tr.m_Index];
            data->m_DestroyQueued = false;

            if (data->m_IsVisible)
            {
                data->m_IsVisible = false;
                OnDataObjectInvisible?.Invoke(tr);
                m_VisibleTransforms.RemoveForSwapBack(data->m_Index);
            }

            int2 proxyIdx = data->m_ProxyIndex;
            if (!proxyIdx.Equals(ProxyTransform.ProxyNull) &&
                !proxyIdx.Equals(ProxyTransform.ProxyQueued))
            {
                m_Instances[proxyIdx.x][proxyIdx.y].InternalOnInvisible();

                RecycleableMonobehaviour proxy = RemoveProxy(data);

                var intersection = frustum.IntersectsSphere(proxy.transform.position, proxy.transform.localScale.sqrMagnitude, 1);

                if ((intersection & IntersectionType.Intersects) == IntersectionType.Intersects ||
                    (intersection & IntersectionType.Contains) == IntersectionType.Contains)
                {
                    proxy.transform.position = INIT_POSITION;
                }
            }

            OnDataObjectDestroy?.Invoke(tr);

            ClusterID id = data->clusterID;
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

            InternalDestroyChildHierarchy(in data, in frustum);

            // has parent
            if (data->m_ParentIndex > 0)
            {
                m_ProxyData.List[data->m_ParentIndex]->m_ChildIndices.RemoveFor(data->m_Index);
                data->m_ParentIndex = -1;
            }

            m_ProxyData.Remove(tr);
            m_ConnectedTransforms.Remove(tr.m_Index);
        }

        #region Jobs

        [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
        private struct ClusterUpdateSortJob : IJob
        {
            private static readonly Unity.Profiling.ProfilerMarker s_Marker
                = new Unity.Profiling.ProfilerMarker("ClusterUpdateSort Job");

            public Cluster<ProxyTransformData> m_ClusterData;
            [DeallocateOnJobCompletion] public NativeArray<ClusterUpdateRequest> m_Request;
            public NativeList<ClusterUpdateRequest> m_SortedRequests;

            public void Execute()
            {
                s_Marker.Begin();

                NativeHashSet<ProxyTransform> m_Listed = new NativeHashSet<ProxyTransform>(m_Request.Length, Allocator.Temp);

                for (int i = m_Request.Length - 1; i >= 0; i--)
                {
                    if (m_Listed.Contains(m_Request[i].transform)) continue;

                    m_SortedRequests.Add(m_Request[i]);
                    m_Listed.Add(m_Request[i].transform);
                }

                s_Marker.End();
            }
        }
        [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
        private struct ClusterUpdateJob : IJobParallelForDefer
        {
            [WriteOnly] public Cluster<ProxyTransformData>.ParallelWriter m_ClusterData;
            [ReadOnly] public NativeArray<ClusterUpdateRequest> m_Requests;

            public void Execute(int i)
            {
                ProxyTransform tr = m_Requests[i].transform;

                if (tr.isDestroyQueued ||
                    tr.isDestroyed) return;

                ClusterID 
                    current = tr.Ref.clusterID,
                    updated = m_ClusterData.Update(m_Requests[i].id, m_Requests[i].translation);
                
                if (!current.Equals(updated))
                {
                    tr.Ref.clusterID = updated;
                }
            }
        }
        [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
        unsafe private struct ClusterJob : IJobParallelFor
        {
            [ReadOnly] public Cluster<ProxyTransformData> m_ClusterData;
            [ReadOnly] public CameraFrustum m_Frustum;
            [WriteOnly] public NativeList<ClusterGroup<ProxyTransformData>>.ParallelWriter m_Output;

            [NativeDisableUnsafePtrRestriction] public int* m_Count;

            public void Execute(int i)
            {
                if (m_Frustum.IntersectsBox(m_ClusterData[i].AABB))
                {
                    m_Output.AddNoResize(m_ClusterData[i]);
                    Interlocked.Increment(ref *m_Count);
                }
            }
        }
        [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
        unsafe private struct ProxyJob : IJobParallelForDefer
        {
            [NativeDisableUnsafePtrRestriction, ReadOnly] public int* m_Count;
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
                if (*m_Count <= i)
                {
                    UnityEngine.Debug.Log("max reach return");
                    return;
                }

                ClusterGroup<ProxyTransformData> clusterGroup = m_ActiveData[i];
                if (!clusterGroup.IsCreated)
                {
                    //UnityEngine.Debug.LogError($"invalid cluster group{i}({m_ActiveData[i].Translation.x}.{m_ActiveData[i].Translation.y}.{m_ActiveData[i].Translation.z}) in return");
                    return;
                }

                for (int j = 0; j < clusterGroup.Length; j++)
                {
                    if (!clusterGroup.HasElementAt(j)) continue;

                    if (clusterGroup[j] >= List.m_Length)
                    {
                        UnityEngine.Debug.LogError($"invalid cluster group{i}({m_ActiveData[i].Translation.x}.{m_ActiveData[i].Translation.y}.{m_ActiveData[i].Translation.z}) ?? {clusterGroup[j]} >= {List.m_Length}");
                        continue;
                    }
                    ref ProxyTransformData data = ref List.ElementAt(clusterGroup[j]);

                    if (data.m_GpuInstanced || (!data.m_EnableCull && !data.m_Prefab.Equals(PrefabReference.None)))
                    {
                        EnabledCullHandler(in data);

                        continue;
                    }

                    if (m_Frustum.IntersectsBox(data.GetAABB(), 10))
                    {
                        if (!data.m_Prefab.IsNone() &&
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
                        if (!data.m_Prefab.IsNone() &&
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

            private void EnabledCullHandler(in ProxyTransformData data)
            {
                if (!data.m_GpuInstanced)
                {
                    if (!data.m_Prefab.IsNone() &&
                        data.m_ProxyIndex.Equals(-1) &&
                        !data.m_ProxyIndex.Equals(-2))
                    {
                        m_Request.Enqueue(data.m_Index);
                    }
                }
                else
                {
                    if (m_Frustum.IntersectsBox(data.GetAABB(), 10))
                    {
                        if (!data.m_IsVisible)
                        {
                            m_Request.Enqueue(data.m_Index);
                            m_Visible.Enqueue(data.m_Index);
                        }
                    }
                    else
                    {
                        if (data.m_IsVisible)
                        {
                            m_Remove.Enqueue(data.m_Index);
                            m_Invisible.Enqueue(data.m_Index);
                        }
                    }
                }

                if (m_Frustum.IntersectsBox(data.GetAABB(), 10))
                {
                    if (!data.m_IsVisible)
                    {
                        m_Visible.Enqueue(data.m_Index);
                    }
                }
                else
                {
                    if (data.m_IsVisible)
                    {
                        m_Invisible.Enqueue(data.m_Index);
                    }
                }
            }
        }

        #endregion

        #endregion

        public struct VisibleEnumerator : IEnumerable<ProxyTransform>
        {
            private NativeProxyData m_ProxyData;
            private NativeSlice<int> m_VisibleTrIndices;

            internal VisibleEnumerator(NativeProxyData proxyData, NativeSlice<int> trIndices)
            {
                m_ProxyData = proxyData;
                m_VisibleTrIndices = trIndices;
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
            public IEnumerator<ProxyTransform> GetEnumerator()
            {
                for (int i = 0; i < m_VisibleTrIndices.Length; i++)
                {
                    yield return m_ProxyData.GetTransform(m_VisibleTrIndices[i]);
                }
            }
        }
        public VisibleEnumerator GetVisibleTransforms()
        {
            return new VisibleEnumerator(m_ProxyData, VisibleTransformIndices);
        }

        /// <summary>
        /// <see cref="Transform"/> 을 <see cref="ProxyTransform"/> 에 연결합니다.
        /// </summary>
        /// <remarks>
        /// 연결된 <paramref name="target"/> 은 <paramref name="transform"/> 의 좌표로 고정됩니다.
        /// </remarks>
        /// <param name="transform"></param>
        /// <param name="target"></param>
        public void ConnectTransform(in ProxyTransform transform, in Transform target)
        {
            CoreSystem.Logger.ThreadBlock(ThreadInfo.Unity);

            if (!m_ConnectedTransforms.TryGetValue(transform.m_Index, out var list))
            {
                list = new List<Transform>();
                m_ConnectedTransforms.Add(transform.m_Index, list);
            }
            list.Add(target);

            target.position = transform.position;
            target.rotation = transform.rotation;
            target.localScale = transform.localScale;
        }
        public void DisconnectTransform(in ProxyTransform transform, in Transform target)
        {
            CoreSystem.Logger.ThreadBlock(ThreadInfo.Unity);

            if (!m_ConnectedTransforms.TryGetValue(transform.m_Index, out var list))
            {
                return;
            }
            list.Remove(target);
        }
        public void UpdateConnectedTransforms(in ProxyTransform transform)
        {
            if (m_ConnectedTransforms.TryGetValue(transform.m_Index, out var connectedTrArr))
            {
                for (int i = 0; i < connectedTrArr.Count; i++)
                {
                    connectedTrArr[i].position = transform.position;
                    connectedTrArr[i].rotation = transform.rotation;
                    connectedTrArr[i].localScale = transform.localScale;
                }
            }
        }

        public ProxyTransform CreateTransform(in float3 pos, in quaternion rot, in float3 scale)
        {
            CoreSystem.Logger.ThreadBlock(ThreadInfo.Unity);

            ProxyTransform tr = m_ProxyData.Add(PrefabReference.None, pos, rot, scale, true, 0, 1, false);

            unsafe
            {
                m_ClusterIDRequests.Enqueue(new ClusterIDRequest(pos, tr.m_Index));
                tr.Pointer->clusterID = ClusterID.Requested;
            }
            OnDataObjectCreated?.Invoke(tr);

            CoreSystem.Logger.Log(LogChannel.Proxy, true,
                string.Format(c_ProxyCreated, "EMPTY", pos));

            return tr;
        }
        public void SetParent(in ProxyTransform parent, in ProxyTransform child)
        {
            unsafe
            {
                m_ProxyData.List[parent.m_Index]->m_ChildIndices.Add(child.m_Index);
                m_ProxyData.List[child.m_Index]->m_ParentIndex = parent.m_Index;
            }
        }
        public ProxyTransform CreateNewPrefab(in PrefabReference<GameObject> prefab, 
            in float3 pos, in quaternion rot, in float3 scale, in bool enableCull, 
            in float3 center, in float3 size,
            bool gpuInstanced)
        {
            const string c_ErrorToEarly = "You've call this method too early or outside of PresentationSystem";

            CoreSystem.Logger.ThreadBlock(nameof(CreateNewPrefab), ThreadInfo.Unity);

            CoreSystem.Logger.NotNull(m_RenderSystem, c_ErrorToEarly);

            ProxyTransform tr;
            if (prefab.IsNone())
            {
                tr = m_ProxyData.Add(PrefabReference.None, pos, rot, scale, enableCull, center, size, false);
            }
            else if (!prefab.IsValid())
            {
                CoreSystem.Logger.LogError(LogChannel.Proxy,
                    $"Trying to create an invalid prefab proxy. This is not allowed. Replaced to empty.");

                tr = m_ProxyData.Add(PrefabReference.None, pos, rot, scale, enableCull, center, size, gpuInstanced);
            }
            else tr = m_ProxyData.Add(prefab, pos, rot, scale, enableCull, center, size, gpuInstanced);

            unsafe
            {
                m_ClusterIDRequests.Enqueue(new ClusterIDRequest(pos, tr.m_Index));
                tr.Pointer->clusterID = ClusterID.Requested;
            }
            OnDataObjectCreated?.Invoke(tr);

            if (!enableCull && !prefab.IsNone())
            {
                m_OverrideRequestProxies.Enqueue(tr.m_Index);
            }

            CoreSystem.Logger.Log(LogChannel.Proxy, true,
                string.Format(c_ProxyCreated, (prefab.GetObjectSetting() != null ? prefab.GetObjectSetting().m_Name : "EMPTY"), pos));

            return tr;
        }
        public void Destroy(in ProxyTransform tr)
        {
            CoreSystem.Logger.ThreadBlock(nameof(Destroy), ThreadInfo.Unity);

            unsafe
            {
                ProxyTransformData* data = m_ProxyData.List[tr.m_Index];

                if (data->m_DestroyQueued)
                {
                    CoreSystem.Logger.LogError(LogChannel.Proxy, 
                        "Cannot destroy this proxy because it is already destroyed.");
                    return;
                }

                data->m_EnableCull = false;
                data->m_DestroyQueued = true;

                m_RequestDestories.Enqueue(tr.m_Index);
                CoreSystem.Logger.Log(LogChannel.Proxy,
                    $"Destroy({data->m_Prefab.GetObjectSetting()?.m_Name}) called");
            }
        }

        #region Proxy Object Control

        private Dictionary<int, InstancedModel[]> m_Models = new Dictionary<int, InstancedModel[]>();

        private unsafe void AddProxy(ProxyTransformData* data)
        {
            CoreSystem.Logger.ThreadBlock(nameof(GameObjectProxySystem.AddProxy), ThreadInfo.Unity);

            PrefabReference prefab = data->m_Prefab;

            if (data->m_GpuInstanced)
            {
                if (m_Models.TryGetValue(data->m_Index, out var arr))
                {
                    for (int i = 0; i < arr.Length; i++)
                    {
                        m_RenderSystem.GetModule<GPUInstancingModule>().RemoveModel(arr[i]);
                    }

                    m_Models.Remove(data->m_Index);
                }

                GameObject obj = (GameObject)prefab.LoadAsset();
                var renderers = obj.GetComponentsInChildren<Renderer>(false);
                InstancedModel[] models = new InstancedModel[renderers.Length];

                if (obj.GetComponentInChildren<Rigidbody>() != null)
                {
                    CoreSystem.Logger.LogError(LogChannel.Proxy,
                        $"Currently gpu instancing system is not support physics object. Proceed to non gpu instancing");

                    goto NON_GPUINSTANCED;
                }

                for (int i = 0; i < renderers.Length; i++)
                {
                    MeshFilter meshFilter = renderers[i].GetComponent<MeshFilter>();
                    Collider col = renderers[i].GetComponent<Collider>();
                    //bool addCollider = col != null;
                    int layer = 0;
                    if (col != null)
                    {
                        layer = col.gameObject.layer;
                    }

                    InstancedModel model = m_RenderSystem.GetModule<GPUInstancingModule>()
                        .AddModel(
                        m_ProxyData.GetTransform(data->m_Index),
                        meshFilter.sharedMesh, renderers[i].sharedMaterials
                        , col, layer
                        );
                    models[i] = model;
                }

                m_Models.Add(data->m_Index, models);
                data->m_ProxyIndex = ProxyTransform.ProxyGPUInstanced;

                return;
            }
            NON_GPUINSTANCED:
            if (!m_TerminatedProxies.TryGetValue(prefab, out Stack<RecycleableMonobehaviour> pool) ||
                    pool.Count == 0)
            {
                data->m_ProxyIndex = (ProxyTransform.ProxyQueued);
                InstantiatePrefab(prefab, (other) =>
                {
                    if (!data->m_IsOccupied || data->m_DestroyQueued)
                    {
                        if (other.InitializeOnCall) other.InternalTerminate();
                        other.transform.position = INIT_POSITION;

                        if (!m_TerminatedProxies.TryGetValue(prefab, out Stack<RecycleableMonobehaviour> pool))
                        {
                            pool = new Stack<RecycleableMonobehaviour>();
                            m_TerminatedProxies.Add(prefab, pool);
                        }
                        pool.Push(other);

                        CoreSystem.Logger.Log(LogChannel.Proxy, true,
                            $"Prefab({prefab.GetObjectSetting().Name}) has been created in different scene. Terminated.");
                        return;
                    }

                    data->m_ProxyIndex = new int2(prefab, other.m_Idx);

                    other.transform.position = data->m_Translation;
                    other.transform.rotation = data->m_Rotation;
                    other.transform.localScale = data->m_Scale;

                    OnDataObjectProxyCreated?.Invoke(m_ProxyData.GetTransform(data->m_Index), other);
                    CoreSystem.Logger.Log(LogChannel.Proxy, true,
                        $"Prefab({prefab.GetObjectSetting().Name}) proxy created");
                });

                CoreSystem.Logger.Log(LogChannel.Proxy, true,
                        $"Prefab({prefab.GetObjectSetting().Name}) proxy requested");
            }
            else
            {
                RecycleableMonobehaviour other = pool.Pop();
                data->m_ProxyIndex = new int2(prefab, other.m_Idx);

                other.transform.position = data->m_Translation;
                other.transform.rotation = data->m_Rotation;
                other.transform.localScale = data->m_Scale;

                if (other.InitializeOnCall) other.InternalInitialize();

                OnDataObjectProxyCreated?.Invoke(m_ProxyData.GetTransform(data->m_Index), other);
                CoreSystem.Logger.Log(LogChannel.Proxy, true,
                    $"Prefab({prefab.GetObjectSetting().Name}) proxy created, pool remains {pool.Count}");
            }
        }
        private unsafe RecycleableMonobehaviour RemoveProxy(ProxyTransformData* data)
        {
            if (data->m_ProxyIndex.Equals(ProxyTransform.ProxyNull))
            {
                throw new Exception();
            }

            PrefabReference prefab = data->m_Prefab;
            if (data->m_GpuInstanced)
            {
                if (m_Models.TryGetValue(data->m_Index, out var arr))
                {
                    for (int i = 0; i < arr.Length; i++)
                    {
                        m_RenderSystem.GetModule<GPUInstancingModule>().RemoveModel(arr[i]);
                    }

                    m_Models.Remove(data->m_Index);
                }

                data->m_ProxyIndex = (ProxyTransform.ProxyNull);
                return null;
            }

            int2 proxyIndex = data->m_ProxyIndex;
            RecycleableMonobehaviour proxy = m_Instances[proxyIndex.x][proxyIndex.y];

            if ((proxy.transform.position - (Vector3)data->m_Translation).sqrMagnitude > .1f)
            {
                data->m_Translation = proxy.transform.position;

                CoreSystem.Logger.LogError(LogChannel.Proxy,
                    "in-corrected translation found. Did you moved proxy transform directly?");
            }

            OnDataObjectProxyRemoved?.Invoke(m_ProxyData.GetTransform(data->m_Index), proxy);

            data->m_ProxyIndex = (ProxyTransform.ProxyNull);

            if (proxy.Activated) proxy.InternalTerminate();

            if (!m_TerminatedProxies.TryGetValue(prefab, out Stack<RecycleableMonobehaviour> pool))
            {
                pool = new Stack<RecycleableMonobehaviour>();
                m_TerminatedProxies.Add(prefab, pool);
            }
            pool.Push(proxy);
            CoreSystem.Logger.Log(LogChannel.Proxy, true,
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

            PrefabReference m_PrefabIdx;
            Scene m_RequestedScene;
            Action<RecycleableMonobehaviour> m_OnCompleted;

            private static Unity.Profiling.ProfilerMarker
                s_SetupMarker = new Unity.Profiling.ProfilerMarker("PrefabRequester: Setup"), 
                s_CreateMarker = new Unity.Profiling.ProfilerMarker("PrefabRequester: Create Prefab");

            public void Setup(GameObjectProxySystem proxySystem, 
                PrefabReference prefabIdx, Vector3 pos, Quaternion rot,
                Action<RecycleableMonobehaviour> onCompleted)
            {
                m_ProxySystem = proxySystem;
                using (s_SetupMarker.Auto())
                {
                    CoreSystem.Logger.True(prefabIdx.IsValid(), nameof(PrefabReference) + "not valid");
                    PrefabList.ObjectSetting prefabInfo = prefabIdx.GetObjectSetting();
                    if (prefabInfo == null)
                    {
                        CoreSystem.Logger.LogError(LogChannel.Proxy,
                            $"Cannot retrieve prefab setting index of {prefabIdx.Index}.");
                        return;
                    }

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
                        if (obj.InitializeOnCall) obj.InternalInitialize();
                        onCompleted?.Invoke(obj);

                        PoolContainer<PrefabRequester>.Enqueue(this);

                        return;
                    }

                    m_PrefabIdx = prefabIdx;
                    m_RequestedScene = SceneSystem.CurrentScene;
                    m_OnCompleted = onCompleted;

                    if (prefabInfo.m_IsRuntimeObject)
                    {
                        CreatePrefab(prefabInfo.m_Prefab);

                        return;
                    }

                    var oper = prefabInfo.InstantiateAsync(pos, rot, null);
                    oper.Completed += CreatePrefab;
                }
            }
            private void CreatePrefab(AsyncOperationHandle<GameObject> other)
            {
                Scene currentScene = SceneSystem.CurrentScene;
                //if (!currentScene.Equals(m_RequestedScene) || SceneSystem.IsSceneLoading)
                //{
                //    CoreSystem.Logger.LogError(Channel.Proxy, $"{other.Result.name} is returned because Scene has been changed");
                //    m_PrefabIdx.ReleaseInstance(other.Result);
                //    return;
                //}

                CreatePrefab(other.Result);
            }
            private void CreatePrefab(GameObject Result)
            {
#if DEBUG_MODE
                s_CreateMarker.Begin();
#endif
                Transform tr;
                if (Result.GetComponentInChildren<RectTransform>() != null)
                {
                    tr = PresentationSystem<DefaultPresentationGroup, WorldCanvasSystem>.System.Canvas.transform;
                }
                else
                {
                    tr = SceneSystem.SceneInstanceFolder;
                }
                Result.transform.SetParent(tr);

                if (m_PrefabIdx.GetObjectSetting().m_IsWorldUI)
                {
                    Result.layer = 5;
                }
                else
                {
                    Result.layer = EntityRaycastSystem.EntityLayer;
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
                if (recycleable.InitializeOnCall) recycleable.InternalInitialize();
                m_OnCompleted?.Invoke(recycleable);

                //if (m_StaticBatching)
                //{
                //    var meshRenderers = Result.GetComponentsInChildren<MeshRenderer>();

                //    StaticBatchingUtility.Combine(meshRenderers.Select((other) => other.gameObject).ToArray(), Result);
                //}

                m_ProxySystem = null;
                m_PrefabIdx = PrefabReference.Invalid;
                m_OnCompleted = null;
                PoolContainer<PrefabRequester>.Enqueue(this);
#if DEBUG_MODE
                s_CreateMarker.End();
#endif
            }
        }

        private void InstantiatePrefab(PrefabReference prefab, Action<RecycleableMonobehaviour> onCompleted)
            => InstantiatePrefab(prefab, INIT_POSITION, Quaternion.identity, onCompleted);
        private void InstantiatePrefab(PrefabReference prefab, 
            Vector3 position, Quaternion rotation,
            Action<RecycleableMonobehaviour> onCompleted)
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

            CoreSystem.Logger.Log(LogChannel.Proxy,
                "Release all proxy GameObjects");
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

    internal sealed class ProxyMessageModule : PresentationSystemModule<GameObjectProxySystem>
    {
        private Dictionary<ProxyTransform, Entry> m_Entries;
        private Dictionary<int, object> m_CachedObjects;
        private List<ProxyTransform> m_WaitTransforms;

        private ObjectPool<NativeQueue<MessageContext>> m_CtxQueuePool;

        private uint m_TickCounter;

        private static NativeQueue<MessageContext> QueueFactory()
        {
            return new NativeQueue<MessageContext>(AllocatorManager.Persistent);
        }
        private static void QueueReserve(NativeQueue<MessageContext> queue)
        {
            queue.Clear();
        }
        private static void QueueRelease(NativeQueue<MessageContext> queue)
        {
            queue.Dispose();
        }

        protected override void OnInitialize()
        {
            m_CtxQueuePool = new ObjectPool<NativeQueue<MessageContext>>(
                QueueFactory,
                null,
                QueueReserve,
                QueueRelease
                );
            m_Entries = new Dictionary<ProxyTransform, Entry>();
            m_CachedObjects = new Dictionary<int, object>();
            m_WaitTransforms = new List<ProxyTransform>();

            ProxyTransformExtensions.s_MessageModule = this;
        }
        protected override void OnDispose()
        {
            ProxyTransformExtensions.s_MessageModule = null;

            foreach (var item in m_Entries.Values)
            {
                m_CtxQueuePool.Reserve(item.queue);
            }
            m_Entries.Clear();
            m_WaitTransforms.Clear();

            m_CtxQueuePool.Dispose();
        }

        private struct Entry
        {
            public ProxyTransform transform;
            public NativeQueue<MessageContext> queue;

            public uint tick;
        }

        protected override void OnPresentation()
        {
            for (int i = m_WaitTransforms.Count - 1; i >= 0; i--)
            {
                ProxyTransform tr = m_WaitTransforms[i];
                Entry entry = m_Entries[tr];
                if (!tr.hasProxy)
                {
                    continue;
                }

                m_Entries.Remove(tr);

                int count = entry.queue.Count;
                RecycleableMonobehaviour proxy = tr.proxy;
                for (int h = 0; h < count; h++)
                {
                    MessageContext ctx = entry.queue.Dequeue();
                    if (m_CachedObjects.TryGetValue(ctx.UserData, out var obj))
                    {
                        m_CachedObjects.Remove(ctx.UserData);
                    }

                    proxy.ProcessMessageContext(ctx, obj);
                }

                m_CtxQueuePool.Reserve(entry.queue);
                m_WaitTransforms.RemoveAt(i);
            }

            m_TickCounter = unchecked(m_TickCounter + 1);
        }

        private int GetUniqueID(ProxyTransform tr, object obj, MessageContext ctx)
        {
            return unchecked(obj.GetHashCode() ^ tr.index ^ ctx.GetHashCode());
        }
        public void SendMessage(ProxyTransform transform, object obj, MessageContext ctx)
        {
            if (transform.hasProxy)
            {
                transform.proxy.ProcessMessageContext(ctx, obj);
                return;
            }

            if (obj != null)
            {
                int id = GetUniqueID(transform, obj, ctx);

                if (m_CachedObjects.ContainsKey(id))
                {
                    "Error".ToLogError();
                }

                m_CachedObjects.Add(id, obj);

                ctx.UserData = id;
            }

            if (!m_Entries.TryGetValue(transform, out Entry entry))
            {
                var queue = m_CtxQueuePool.Get();
                queue.Enqueue(ctx);

                entry = new Entry
                {
                    transform = transform,
                    queue = queue,

                    tick = m_TickCounter
                };

                m_Entries.Add(transform, entry);
                m_WaitTransforms.Add(transform);
                return;
            }

            entry.queue.Enqueue(ctx);
        }
    }

    public static class ProxyTransformExtensions
    {
        internal static ProxyMessageModule s_MessageModule;

        public static void SendMessage(this ProxyTransform t, MessageContext ctx)
        {
            s_MessageModule.SendMessage(t, null, ctx);
        }
        public static void SendMessage(this ProxyTransform t, string methodName)
        {
            s_MessageModule.SendMessage(t, null, new MessageContext(methodName));
        }
        public static void SendMessage(this ProxyTransform t, string methodName, SendMessageOptions options = SendMessageOptions.RequireReceiver)
        {
            s_MessageModule.SendMessage(t, null, new MessageContext(methodName, options));
        }
        public static void SendMessage(this ProxyTransform t, string methodName, object obj, SendMessageOptions options = SendMessageOptions.RequireReceiver)
        {
            s_MessageModule.SendMessage(t, obj, new MessageContext(methodName, options));
        }
    }
}
