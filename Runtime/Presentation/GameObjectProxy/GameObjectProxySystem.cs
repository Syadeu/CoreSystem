﻿using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Mono;
using Syadeu.Presentation.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Profiling;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

namespace Syadeu.Presentation
{
    internal sealed class GameObjectProxySystem : PresentationSystemEntity<GameObjectProxySystem>
    {
        private static Vector3 INIT_POSITION = new Vector3(-9999, -9999, -9999);

        public override bool EnableBeforePresentation => true;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => true;

        public event Action<DataGameObject> OnDataObjectCreatedAsync;
        public event Action<DataGameObject> OnDataObjectDestroyAsync;
        public event Action<DataGameObject> OnDataObjectVisibleAsync;
        public event Action<DataGameObject> OnDataObjectInvisibleAsync;

        public event Action<DataGameObject, RecycleableMonobehaviour> OnDataObjectProxyCreated;
        public event Action<DataGameObject, RecycleableMonobehaviour> OnDataObjectProxyRemoved;

        internal NativeHashMap<Hash, int> m_MappedGameObjectIdxes = new NativeHashMap<Hash, int>(1000, Allocator.Persistent);
        internal NativeHashMap<Hash, int> m_MappedTransformIdxes = new NativeHashMap<Hash, int>(1000, Allocator.Persistent);
        internal NativeList<DataGameObject> m_MappedGameObjects = new NativeList<DataGameObject>(1000, Allocator.Persistent);
        internal NativeList<DataTransform> m_MappedTransforms = new NativeList<DataTransform>(1000, Allocator.Persistent);
        
        private readonly ConcurrentQueue<Hash> m_UpdateTransforms = new ConcurrentQueue<Hash>();
        private readonly ConcurrentQueue<Action> m_RequestedJobs = new ConcurrentQueue<Action>();
        private readonly ConcurrentQueue<Hash> m_RequestDestories = new ConcurrentQueue<Hash>();
        //private readonly ConcurrentQueue<IInternalDataComponent> m_RequestProxies = new ConcurrentQueue<IInternalDataComponent>();
        //private readonly ConcurrentQueue<IInternalDataComponent> m_RemoveProxies = new ConcurrentQueue<IInternalDataComponent>();

        private int m_VisibleCheckJobWorker;
        private BackgroundJob m_VisibleCheckJob;
        private readonly List<BackgroundJob> m_VisibleCheckJobs = new List<BackgroundJob>();

        NativeQueue<Hash>
                m_RequestProxyList = new NativeQueue<Hash>(Allocator.Persistent),
                m_RemoveProxyList = new NativeQueue<Hash>(Allocator.Persistent),
                m_VisibleList = new NativeQueue<Hash>(Allocator.Persistent),
                m_InvisibleList = new NativeQueue<Hash>(Allocator.Persistent);

        private readonly List<int> 
            m_RemovedTransformIdxes = new List<int>(),
            m_RemovedGameObjectIdxes = new List<int>();

        private SceneSystem m_SceneSystem;
        private Render.RenderSystem m_RenderSystem;

        private bool m_LoadingLock = false;
        private bool m_Disposed = false;

        public bool Disposed => m_Disposed;

        #region Presentation Methods
        protected override PresentationResult OnInitialize()
        {
            m_VisibleCheckJobWorker = CoreSystem.CreateNewBackgroundJobWorker(true);
            m_VisibleCheckJob = new BackgroundJob(ProxyVisibleCheckPararellJob);

            if (!PoolContainer<PrefabRequester>.Initialized) PoolContainer<PrefabRequester>.Initialize(() => new PrefabRequester(), 10);

            return base.OnInitialize();
        }
        protected override PresentationResult OnInitializeAsync()
        {
            AssemblyName aName = new AssemblyName("CoreSystem_Runtime");
            AssemblyBuilder ab = AppDomain.CurrentDomain.DefineDynamicAssembly(aName, AssemblyBuilderAccess.Run);
            m_ModuleBuilder = ab.DefineDynamicModule(aName.Name);

            RequestSystem<SceneSystem>((other) => m_SceneSystem = other);
            RequestSystem<Render.RenderSystem>((other) => m_RenderSystem = other);

            return base.OnInitializeAsync();
        }
        protected override PresentationResult OnStartPresentation()
        {
            m_SceneSystem.OnLoadingEnter += () =>
            {
                m_LoadingLock = true;
                CoreSystem.Logger.Log(Channel.Proxy, true,
                    "Scene on loading enter lambda excute");

                while (m_RequestDestories.Count > 0)
                {
                    m_RequestDestories.TryDequeue(out _);
                }
                m_RemovedGameObjectIdxes.Clear();

                #region Clear Data Transforms
                for (int i = 0; i < m_MappedTransforms.Length; i++)
                {
                    if (m_MappedTransforms[i].HasProxyObject)
                    {
                        int2 proxyIdx = m_MappedTransforms[i].m_ProxyIdx;

                        var prefabSetting = PrefabList.Instance.ObjectSettings[proxyIdx.x];
                        RecycleableMonobehaviour obj = m_Instances[proxyIdx.x][proxyIdx.y];

                        //prefabSetting.Pool.Enqueue(obj);
                        obj.Terminate();
                    }
                }
                m_MappedTransforms.Clear();
                m_MappedTransformIdxes.Clear();
                #endregion

                #region Clear Data GameObjects
                for (int i = 0; i < m_MappedGameObjects.Length; i++)
                {
                    OnDataObjectDestroyAsync?.Invoke(m_MappedGameObjects[i]);
                }
                m_MappedGameObjects.Clear();
                m_MappedGameObjectIdxes.Clear();
                #endregion

                #region Proxy Release

                m_RequestProxyList.Clear();
                m_RemoveProxyList.Clear();

                m_TerminatedProxies.Clear();

                //for (int i = 0; i < PrefabList.Instance.ObjectSettings.Count; i++)
                //{
                //    PrefabList.Instance.ObjectSettings[i].Pool.Clear();
                //}

                #endregion

                m_VisibleList.Clear();
                m_InvisibleList.Clear();

                m_Instances.Clear();

                m_LoadingLock = false;
            };
            return base.OnStartPresentation();
        }

        protected override PresentationResult BeforePresentation()
        {
            if (m_LoadingLock) return base.BeforePresentation();

            #region Object Proxy Work

            int requestProxyCount = m_RequestProxyList.Count;
            for (int i = 0; i < requestProxyCount; i++)
            {
                Hash trIdx = m_RequestProxyList.Dequeue();
                
                RequestProxy(trIdx);
            }

            int temp2 = m_RemoveProxyList.Count;
            for (int i = 0; i < temp2; i++)
            {
                Hash trIdx = m_RemoveProxyList.Dequeue();

                RecycleableMonobehaviour obj = DetechProxy(trIdx, out var prefab);
                ReleaseProxy(prefab, obj);
            }

            #endregion

            return base.BeforePresentation();
        }

        protected override PresentationResult AfterPresentation()
        {
            int jobCount = m_RequestedJobs.Count;
            for (int i = 0; i < jobCount; i++)
            {
                if (!m_RequestedJobs.TryDequeue(out Action job)) continue;
                job.Invoke();
            }

            int trUpdateCount = m_UpdateTransforms.Count;
            for (int i = 0; i < trUpdateCount; i++)
            {
                if (!m_UpdateTransforms.TryDequeue(out Hash trHash)) continue;

                unsafe
                {
                    ref DataTransform dataTr = ref *GetDataTransformPointer(trHash);
                    if (dataTr.ProxyRequested)
                    {
                        m_UpdateTransforms.Enqueue(trHash);
                        continue;
                    }
                    else if (!dataTr.HasProxyObject) continue;
                }
                UpdateDataTransform(trHash);
            }

            return base.AfterPresentation();
        }
        
        protected override PresentationResult AfterPresentationAsync()
        {
            #region Object Destory Work
            int temp3 = m_RequestDestories.Count;
            if (temp3 > 0)
            {
                if (!m_LoadingLock && m_VisibleCheckJob.IsDone)
                {
                    for (int i = 0; i < temp3; i++)
                    {
                        if (!m_RequestDestories.TryDequeue(out Hash objHash)) continue;

                        int objIdx = m_MappedGameObjectIdxes[objHash];
                        int trIdx = m_MappedTransformIdxes[m_MappedGameObjects[objIdx].m_Transform];

                        OnDataObjectDestroyAsync?.Invoke(m_MappedGameObjects[objIdx]);

                        if (m_MappedTransforms[trIdx].HasProxyObject)
                        {
                            //RemoveProxy(m_MappedGameObjects[objIdx].m_Transform, true);
                            RecycleableMonobehaviour obj = DetechProxy(m_MappedTransforms[trIdx].m_Idx, out var prefab);
                            CoreSystem.AddForegroundJob(() => ReleaseProxy(prefab, obj));
                        }

                        // 여기서 지우면 다른 오브젝트의 인덱스가 헷갈리니까 일단 위치저장
                        m_RemovedTransformIdxes.Add(trIdx);
                        m_MappedTransformIdxes.Remove(m_MappedGameObjects[objIdx].m_Transform);

                        ((IDisposable)m_MappedGameObjects[objIdx]).Dispose();

                        // 여기서 지우면 다른 오브젝트의 인덱스가 헷갈리니까 일단 위치저장
                        m_RemovedGameObjectIdxes.Add(objIdx);
                        m_MappedGameObjectIdxes.Remove(objHash);
                    }
                }
            }
            else
            {
                if (m_VisibleCheckJob.IsDone)
                {
                    m_VisibleCheckJob.Start(m_VisibleCheckJobWorker);
                }
            }

            // 데이터 재 인덱싱
            if (m_RemovedGameObjectIdxes.Count > 0)
            {
                List<int> removedObj = m_RemovedGameObjectIdxes.ToList();
                List<int> removedTr = m_RemovedTransformIdxes.ToList();

                for (int i = 0, j = 0; i < m_MappedGameObjects.Length; i++, j++)
                {
                    if (removedObj.Contains(j))
                    {
                        m_RemovedGameObjectIdxes.Remove(j);
                        m_MappedGameObjects.RemoveAt(i);
                        i--;
                        continue;
                    }

                    m_MappedGameObjectIdxes[m_MappedGameObjects[i].m_Idx] = i;
                }
                for (int i = 0, j = 0; i < m_MappedTransforms.Length; i++, j++)
                {
                    if (removedTr.Contains(j))
                    {
                        m_RemovedTransformIdxes.Remove(j);
                        m_MappedTransforms.RemoveAt(i);
                        i--;
                        continue;
                    }
                    m_MappedTransformIdxes[m_MappedTransforms[i].m_Idx] = i;
                }
            }
            #endregion

            return base.AfterPresentationAsync();
        }
        public override void Dispose()
        {
            m_MappedGameObjectIdxes.Dispose();
            m_MappedTransformIdxes.Dispose();
            m_MappedGameObjects.Dispose();
            m_MappedTransforms.Dispose();
            CoreSystem.RemoveBackgroundJobWorker(m_VisibleCheckJobWorker);

            m_RequestProxyList.Dispose();
            m_RemoveProxyList.Dispose();
            m_VisibleList.Dispose();
            m_InvisibleList.Dispose();

            m_Disposed = true;

            base.Dispose();
        }
        #endregion

        internal void DestoryDataObject(Hash objHash)
        {
            if (m_RequestDestories.Contains(objHash))
            {
                CoreSystem.Logger.LogError(Channel.Presentation, $"Already queued {objHash}");
                return;
            }
            m_RequestDestories.Enqueue(objHash);
        }
        public void RequestUpdateTransform(Hash trHash)
        {
            if (m_UpdateTransforms.Contains(trHash))
            {
                CoreSystem.Logger.LogWarning(Channel.Presentation, $"Ignore transform({trHash}) update request, already requested");
                return;
            }

            m_UpdateTransforms.Enqueue(trHash);
        }

        internal DataGameObject CreateEmpty(float3 translation, quaternion rotation, float3 scale)
        {
            Hash trHash = Hash.NewHash();
            Hash objHash = Hash.NewHash();

            DataTransform trData = new DataTransform()
            {
                m_GameObject = objHash,
                m_Idx = trHash,
                m_ProxyIdx = DataTransform.ProxyNull,
                m_PrefabIdx = -1,
                m_EnableCull = false,

                m_Position = translation,
                m_Rotation = rotation,
                m_LocalScale = scale
            };
            DataGameObject objData = new DataGameObject()
            {
                m_Idx = objHash,
                m_Transform = trHash
            };

            int objIdx = m_MappedGameObjects.Length;
            m_MappedGameObjects.Add(objData);
            m_MappedGameObjectIdxes.Add(objHash, objIdx);

            int trIdx = m_MappedTransforms.Length;
            m_MappedTransforms.Add(trData);
            m_MappedTransformIdxes.Add(trHash, trIdx);

            OnDataObjectCreatedAsync?.Invoke(objData);
            return objData;
        }
        internal DataGameObject CreateNewPrefab(PrefabReference prefab, Vector3 pos)
        {
            PrefabList.ObjectSetting objSetting;
            try
            {
                objSetting = prefab.GetObjectSetting();
            }
            catch (KeyNotFoundException)
            {
                CoreSystem.Logger.LogError(Channel.Entity, "Prefab is invalid. Create new prefab request has been ignored.");
                return default;
            }
            catch (Exception)
            {
                throw;
            }

            //Transform tr;
            //if (objSetting.m_RefPrefab.Asset == null)
            //{
            //    var asyncOper = objSetting.m_RefPrefab.LoadAssetAsync<GameObject>();
            //    asyncOper.Completed += (oper) =>
            //    {

            //    };
            //}
            //else tr = CoreSystem.GetTransform((GameObject)objSetting.m_RefPrefab.Asset);

            //DataTransform dataTr;
            //try
            //{
            //    dataTr = ToDataTransform(tr);
            //}
            //catch (NullReferenceException)
            //{
            //    CoreSystem.Logger.LogError(Channel.Entity, $"Prefab({objSetting.m_Name}) is null. Create new prefab request has been ignored.");
            //    return default;
            //}
            //catch (Exception)
            //{
            //    throw;
            //}

            return CreateNewPrefab(prefab, pos, quaternion.identity, Vector3.one, true);
        }
        internal DataGameObject CreateNewPrefab(PrefabReference prefab, 
            Vector3 pos, Quaternion rot, Vector3 localScale, bool enableCull)
        {
            CoreSystem.Logger.NotNull(m_RenderSystem, $"You've call this method too early or outside of PresentationSystem");

            Hash trHash = Hash.NewHash();
            Hash objHash = Hash.NewHash();

            int2 proxyIdx = DataTransform.ProxyNull;
            if (!enableCull || m_RenderSystem.IsInCameraScreen(pos))
            {
                proxyIdx = DataTransform.ProxyQueued;
            }

            ThreadSafe.Vector3 right = ThreadSafe.Vector3.Right;
            ThreadSafe.Vector3 up = ThreadSafe.Vector3.Up;
            ThreadSafe.Vector3 forward = ThreadSafe.Vector3.Forward;
            if (!rot.Equals(Quaternion.identity))
            {
                right = new ThreadSafe.Vector3(rot * Vector3.right);
                up = new ThreadSafe.Vector3(rot * Vector3.up);
                forward = new ThreadSafe.Vector3(rot * Vector3.right);
            }

            DataTransform trData = new DataTransform()
            {
                m_GameObject = objHash,
                m_Idx = trHash,
                m_ProxyIdx = proxyIdx,
                m_PrefabIdx = prefab,
                m_EnableCull = enableCull,

                m_Position = new ThreadSafe.Vector3(pos),
                m_Rotation = rot,
                m_LocalScale = new ThreadSafe.Vector3(localScale)
            };
            DataGameObject objData = new DataGameObject()
            {
                m_Idx = objHash,
                m_Transform = trHash
            };

            int objIdx = m_MappedGameObjects.Length;
            m_MappedGameObjects.Add(objData);
            m_MappedGameObjectIdxes.Add(objHash, objIdx);

            int trIdx = m_MappedTransforms.Length;
            m_MappedTransforms.Add(trData);
            m_MappedTransformIdxes.Add(trHash, trIdx);

            if (proxyIdx.Equals(DataTransform.ProxyQueued))
            {
                m_RequestProxyList.Enqueue(trHash);
            }

            OnDataObjectCreatedAsync?.Invoke(objData);
            return objData;
        }
        private DataTransform ToDataTransform(Transform tr)
        {
            Vector3
                pos = Vector3.zero, localScale = Vector3.zero;
            Quaternion rotation = Quaternion.identity;

            if (CoreSystem.IsThisMainthread())
            {
                pos = tr.position;
                localScale = tr.localScale;
                rotation = tr.rotation;
            }
            else
            {
                CoreSystem.AddForegroundJob(() =>
                {
                    pos = tr.position;
                    localScale = tr.localScale;
                    rotation = tr.rotation;
                }).Await();
            }
            return new DataTransform
            {
                m_Position = new ThreadSafe.Vector3(pos),
                m_LocalScale = new ThreadSafe.Vector3(localScale),
                m_Rotation = rotation
            };
        }

        #region Proxy Object Control
        
        unsafe private void RequestProxy(Hash trIdx)
        {
            DataTransform* p = (DataTransform*)m_MappedTransforms.GetUnsafePtr();
            ref DataTransform tr = ref *(p + m_MappedTransformIdxes[trIdx]);
            PrefabReference prefab = tr.m_PrefabIdx;

            if (!m_TerminatedProxies.TryGetValue(prefab, out Queue<RecycleableMonobehaviour> pool) ||
                    pool.Count == 0)
            {
                for (int a = 0; a < 10; a++)
                {
                    InstantiatePrefab(prefab, (obj) =>
                    {
                        obj.Terminate();
                        obj.transform.position = INIT_POSITION;

                        if (!m_TerminatedProxies.TryGetValue(prefab, out Queue<RecycleableMonobehaviour> pool))
                        {
                            pool = new Queue<RecycleableMonobehaviour>();
                            m_TerminatedProxies.Add(prefab, pool);
                        }
                        pool.Enqueue(obj);
                    });
                }

                m_RequestProxyList.Enqueue(trIdx);
            }
            else
            {
                RecycleableMonobehaviour other = pool.Dequeue();
                tr.m_ProxyIdx = new int2(prefab, other.m_Idx);

                other.transform.position = tr.m_Position;
                other.transform.rotation = tr.m_Rotation;
                other.transform.localScale = tr.m_LocalScale;

                if (other.InitializeOnCall) other.Initialize();

                OnDataObjectProxyCreated?.Invoke(tr.gameObject, other);
                CoreSystem.Logger.Log(Channel.Proxy, true,
                    $"DataGameobject({tr.m_GameObject}) proxy created, pool remains {pool.Count}");
            }
        }
        unsafe private RecycleableMonobehaviour DetechProxy(Hash trIdx, out PrefabReference prefab)
        {
            DataTransform* p = (DataTransform*)m_MappedTransforms.GetUnsafePtr();
            ref DataTransform tr = ref *(p + m_MappedTransformIdxes[trIdx]);

            int2 proxyIdx = tr.m_ProxyIdx;
            CoreSystem.Logger.False(proxyIdx.Equals(DataTransform.ProxyNull), $"proxy index null {proxyIdx}");

            RecycleableMonobehaviour obj = m_Instances[proxyIdx.x][proxyIdx.y];

            OnDataObjectProxyRemoved?.Invoke(tr.gameObject, obj);
            tr.m_ProxyIdx = DataTransform.ProxyNull;
            CoreSystem.Logger.Log(Channel.Proxy, true,
                $"DataGameobject({tr.m_GameObject}) proxy removed.");

            prefab = proxyIdx.x;
            return obj;
        }
        unsafe private void ReleaseProxy(PrefabReference prefab, RecycleableMonobehaviour obj)
        {
            //DataTransform* p = (DataTransform*)m_MappedTransforms.GetUnsafePtr();
            //ref DataTransform tr = ref *(p + m_MappedTransformIdxes[trIdx]);

            //if (!isDestroy)
            //{
            //    if ((obj.transform.position - tr.position).sqrMagnitude > 1)
            //    {
            //        CoreSystem.Logger.LogWarning(Channel.Proxy, 
            //            $"Detecting incorrect translation between DataTransform, Proxy at {prefab.GetObjectSetting().m_Name}. This will be slightly cared but highly suggested do not manipulate Proxy\'s own translation.");

            //        Transform monoTr = obj.transform;

            //        tr.m_Position = new ThreadSafe.Vector3(monoTr.position);
            //        tr.m_Rotation = monoTr.rotation;
            //    }
            //}

            obj.Terminate();
            obj.transform.position = INIT_POSITION;

            if (!m_TerminatedProxies.TryGetValue(prefab, out Queue<RecycleableMonobehaviour> pool))
            {
                pool = new Queue<RecycleableMonobehaviour>();
                m_TerminatedProxies.Add(prefab, pool);
            }
            pool.Enqueue(obj);
        }

        unsafe internal void DownloadDataTransform(Hash trHash)
        {
            ref DataTransform boxed = ref *GetDataTransformPointer(trHash);
            Transform oriTr = boxed.ProxyObject.transform;

            boxed.m_Position = new ThreadSafe.Vector3(oriTr.position);
            boxed.m_Rotation = oriTr.rotation;
            boxed.m_LocalScale = new ThreadSafe.Vector3(oriTr.localScale);
        }
        unsafe private void UpdateDataTransform(Hash trHash)
        {
            if (m_LoadingLock) return;

            ref DataTransform boxed = ref *GetDataTransformPointer(trHash);
            Transform oriTr = boxed.ProxyObject.transform;

            oriTr.position = boxed.m_Position;
            oriTr.rotation = boxed.m_Rotation;
            oriTr.localScale = boxed.m_LocalScale;
        }

        unsafe private void ProxyVisibleCheckPararellJob()
        {
            const int maxCountForeachJob = 10;

            if (m_LoadingLock) return;

            int listCount = m_MappedTransforms.Length;
            int div = listCount / maxCountForeachJob;

            DataTransform* trArrayP = (DataTransform*)m_MappedTransforms.GetUnsafePtr();

            for (int i = 0; i < div + 1; i++)
            {
                BackgroundJob job = PoolContainer<BackgroundJob>.Dequeue();
                int startIdx = i * maxCountForeachJob;
                m_VisibleCheckJobs.Add(job);
                int maxIdx = (i + 1) * maxCountForeachJob;
                if (maxIdx > listCount) maxIdx = listCount;

                job.Action = () =>
                {
                    for (int j = startIdx; j < maxIdx; j++)
                    {
                        if (m_LoadingLock) break;
                        ref DataTransform tr = ref *(trArrayP + j);

                        if (!tr.IsValid()) continue;
                        if (m_RenderSystem.IsInCameraScreen(tr.position))
                        {
                            if (tr.m_PrefabIdx >= 0 &&
                                !tr.ProxyRequested &&
                                !tr.HasProxyObject)
                            {
                                if (tr.m_EnableCull)
                                {
                                    tr.m_ProxyIdx = DataTransform.ProxyQueued;
                                    //PrefabReference prefab = tr.m_PrefabIdx;

                                    //RequestProxy(trArrayP + j);
                                    m_RequestProxyList.Enqueue(tr.m_Idx);
                                }
                            }

                            if (!tr.m_IsVisible)
                            {
                                tr.m_IsVisible = true;
                                OnDataObjectVisibleAsync?.Invoke(tr.gameObject);
                            }
                        }
                        else
                        {
                            if (tr.m_PrefabIdx >= 0 &&
                                tr.m_EnableCull &&
                                tr.HasProxyObject)
                            {
                                //if (m_RemoveProxies.Contains(tr))
                                //{
                                //    throw new Exception();
                                //}
                                //m_RemoveProxies.Enqueue(tr);
                                m_RemoveProxyList.Enqueue(tr.m_Idx);
                            }

                            if (tr.m_IsVisible)
                            {
                                tr.m_IsVisible = false;
                                OnDataObjectInvisibleAsync?.Invoke(tr.gameObject);
                            }
                        }
                    }
                    PoolContainer<BackgroundJob>.Enqueue(job);
                };
                job.Start();
            }

            for (int i = 0; i < m_VisibleCheckJobs.Count; i++)
            {
                m_VisibleCheckJobs[i].Await();
            }
            m_VisibleCheckJobs.Clear();
        }
        #endregion

        #region Data Pointer
        unsafe internal DataTransform* GetDataTransformPointer(Hash trHash)
        {
            int idx = m_MappedTransformIdxes[trHash];

            DataTransform* targetTr = ((DataTransform*)m_MappedTransforms.GetUnsafePtr()) + idx;
            return targetTr;
        }
        unsafe internal DataGameObject* GetDataGameObjectPointer(Hash objHash)
        {
            if (m_Disposed) return null;
            if (!m_MappedGameObjectIdxes.TryGetValue(objHash, out int idx))
            {
                CoreSystem.Logger.LogWarning(Channel.Proxy,
                    $"DataGameObject({objHash}) is already destroyed or not found. Request ignored.");
                return null;
            }
            //int idx = m_MappedGameObjectIdxes[objHash];

            DataGameObject* obj = ((DataGameObject*)m_MappedGameObjects.GetUnsafePtr()) + idx;
            return obj;
        }
        public DataTransform GetDataTransform(Hash hash)
        {
            unsafe
            {
                return *GetDataTransformPointer(hash);
            }
        }
        public DataGameObject GetDataGameObject(Hash hash)
        {
            unsafe
            {
                DataGameObject* p = GetDataGameObjectPointer(hash);
                return p == null ? DataGameObject.Null : *p;
            }
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
