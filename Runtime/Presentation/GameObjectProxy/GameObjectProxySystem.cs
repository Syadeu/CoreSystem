using Syadeu.Database;
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
    public sealed class GameObjectProxySystem : PresentationSystemEntity<GameObjectProxySystem>
    {
        private static Vector3 INIT_POSITION = new Vector3(-9999, -9999, -9999);

        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => true;

        public event Action<DataGameObject> OnDataObjectDestoryAsync;
        public event Action<DataGameObject> OnDataObjectVisibleAsync;
        public event Action<DataGameObject> OnDataObjectInvisibleAsync;

        public event Action<DataGameObject> OnDataObjectProxyCreated;
        public event Action<DataGameObject> OnDataObjectProxyRemoved;

        internal NativeHashMap<Hash, int> m_MappedGameObjectIdxes = new NativeHashMap<Hash, int>(1000, Allocator.Persistent);
        internal NativeHashMap<Hash, int> m_MappedTransformIdxes = new NativeHashMap<Hash, int>(1000, Allocator.Persistent);
        internal NativeList<DataGameObject> m_MappedGameObjects = new NativeList<DataGameObject>(1000, Allocator.Persistent);
        internal NativeList<DataTransform> m_MappedTransforms = new NativeList<DataTransform>(1000, Allocator.Persistent);
        
        internal readonly Dictionary<Hash, List<DataComponentEntity>> m_ComponentList = new Dictionary<Hash, List<DataComponentEntity>>();

        private readonly ConcurrentQueue<Hash> m_UpdateTransforms = new ConcurrentQueue<Hash>();
        private readonly ConcurrentQueue<Action> m_RequestedJobs = new ConcurrentQueue<Action>();
        private readonly ConcurrentQueue<Hash> m_RequestDestories = new ConcurrentQueue<Hash>();
        private readonly ConcurrentQueue<IInternalDataComponent> m_RequestProxies = new ConcurrentQueue<IInternalDataComponent>();
        private readonly ConcurrentQueue<IInternalDataComponent> m_RemoveProxies = new ConcurrentQueue<IInternalDataComponent>();

        private int m_VisibleCheckJobWorker;
        private BackgroundJob m_VisibleCheckJob;
        private readonly List<BackgroundJob> m_VisibleCheckJobs = new List<BackgroundJob>();

        private SceneSystem m_SceneSystem;
        private RenderSystem m_RenderSystem;

        private bool m_LoadingLock = false;

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
            RequestSystem<RenderSystem>((other) => m_RenderSystem = other);

            return base.OnInitializeAsync();
        }
        protected override PresentationResult OnStartPresentation()
        {
            m_SceneSystem.OnLoadingEnter += () =>
            {
                m_LoadingLock = true;

                foreach (var item in m_TerminatedProxies)
                {
                    var prefabInfo = PrefabList.Instance.ObjectSettings[item.Key];
                    int count = item.Value.Count;
                    for (int i = 0; i < count; i++)
                    {
                        prefabInfo.RefPrefab.ReleaseInstance(item.Value.Dequeue().gameObject);
                    }
                }
                m_TerminatedProxies.Clear();

                #region Clear Data Components
                foreach (var item in m_ComponentList.Values)
                {
                    for (int i = 0; i < item.Count; i++)
                    {
                        ((IDisposable)item[i]).Dispose();
                    }
                }
                m_ComponentList.Clear();
                #endregion

                #region Clear Data Transforms
                for (int i = 0; i < m_MappedTransforms.Length; i++)
                {
                    if (m_MappedTransforms[i].HasProxyObject)
                    {
                        int2 proxyIdx = m_MappedTransforms[i].m_ProxyIdx;

                        PrefabList.Instance.ObjectSettings[proxyIdx.x].RefPrefab.ReleaseInstance(m_Instances[proxyIdx.x][proxyIdx.y].gameObject);
                    }
                }
                m_MappedTransforms.Clear();
                m_MappedTransformIdxes.Clear();
                #endregion

                #region Clear Data GameObjects
                for (int i = 0; i < m_MappedGameObjects.Length; i++)
                {
                    ref GridManager.Grid g = ref GridManager.GetGrid(m_MappedGameObjects[i].m_GridIdxes.x);
                    ref GridManager.GridCell c = ref g.GetCell(m_MappedGameObjects[i].m_GridIdxes.y);

                    c.RemoveCustomData();
                }
                m_MappedGameObjects.Clear();
                m_MappedGameObjectIdxes.Clear();
                #endregion

                m_Instances.Clear();

                m_LoadingLock = false;
            };
            return base.OnStartPresentation();
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
        private readonly List<int> m_RemovedTransformIdxes = new List<int>();
        private readonly List<int> m_RemovedGameObjectIdxes = new List<int>();
        protected override PresentationResult AfterPresentationAsync()
        {
            #region Object Proxy Work
            int temp1 = m_RequestProxies.Count;
            for (int i = 0; i < temp1; i++)
            {
                if (!m_RequestProxies.TryDequeue(out var data)) continue;
                CoreSystem.Logger.NotNull(data);

                RequestProxy(data.GameObject, data.Idx, null);
                //$"requesting {m_RequestProxies[i].Idx}".ToLog();
            }
            //m_RequestProxies.RemoveRange(0, temp1);
            int temp2 = m_RemoveProxies.Count;
            for (int i = 0; i < temp2; i++)
            {
                if (!m_RemoveProxies.TryDequeue(out var data)) continue;
                CoreSystem.Logger.NotNull(data);

                RemoveProxy(data.Idx);
            }
            //m_RemoveProxies.RemoveRange(0, temp2);
            #endregion

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

                        OnDataObjectDestoryAsync?.Invoke(m_MappedGameObjects[objIdx]);

                        if (m_MappedTransforms[trIdx].HasProxyObject)
                        {
                            RemoveProxy(m_MappedGameObjects[objIdx].m_Transform);
                        }

                        //GridManager.GetGrid(m_MappedGameObjects[objIdx].m_GridIdxes.x)
                        //    .GetCell(m_MappedGameObjects[objIdx].m_GridIdxes.y)
                        //    .RemoveCustomData();

                        // 여기서 지우면 다른 오브젝트의 인덱스가 헷갈리니까 일단 위치저장
                        m_RemovedTransformIdxes.Add(trIdx);
                        m_MappedTransformIdxes.Remove(m_MappedGameObjects[objIdx].m_Transform);

                        if (m_ComponentList.TryGetValue(objHash, out List<DataComponentEntity> components))
                        {
                            for (int j = 0; j < components.Count; j++)
                            {
                                ((IDisposable)components[j]).Dispose();
                            }
                            m_ComponentList.Remove(objHash);
                        }
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

            base.Dispose();
        }
        #endregion

        public void DestoryDataObject(Hash objHash)
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

        public DataGameObject CreateNewPrefab(int prefabIdx, Vector3 pos, Quaternion rot)
            => CreateNewPrefab(prefabIdx, pos, rot, Vector3.one, true, null);
        internal DataGameObject CreateNewPrefab(int prefabIdx, 
            Vector3 pos, Quaternion rot, Vector3 localScale, bool enableCull,
            Action<DataGameObject, RecycleableMonobehaviour> onCompleted)
        {
            CoreSystem.Logger.NotNull(m_RenderSystem, $"You've call this method too early or outside of PresentationSystem");

            //if (!GridManager.HasGrid(pos))
            //{
            //    CoreSystem.Logger.LogError(Channel.Data, $"Can\'t spawn prefab {prefabIdx} at {pos}, There\'s no grid");
            //    throw new Exception();
            //}
            //ref GridManager.Grid grid = ref GridManager.GetGrid(pos);
            //if (!grid.HasCell(pos))
            //{
            //    CoreSystem.Logger.LogError(Channel.Data, $"Can\'t spawn prefab {prefabIdx} at {pos}, There\'s no grid cell");
            //    throw new Exception();
            //}
            //ref GridManager.GridCell cell = ref grid.GetCell(pos);
            //if (cell.GetCustomData() != null)
            //{
            //    CoreSystem.Logger.LogError(Channel.Data, $"Can\'t spawn prefab {prefabIdx} at {pos}, target grid cell has object");
            //    throw new Exception();
            //}

            Hash trHash = Hash.NewHash();
            Hash objHash = Hash.NewHash();

            int2 proxyIdx = DataTransform.ProxyNull;
            if (m_RenderSystem.IsInCameraScreen(pos))
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
                m_PrefabIdx = prefabIdx,
                m_EnableCull = enableCull,

                m_Position = new ThreadSafe.Vector3(pos),
                m_Rotation = rot,
                m_LocalScale = new ThreadSafe.Vector3(localScale)
            };
            DataGameObject objData = new DataGameObject()
            {
                m_Idx = objHash,
                //m_GridIdxes = cell.Idxes,

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
                RequestProxy(objHash, trHash, onCompleted);
            }

            //cell.SetCustomData(objData);
            //$"{prefabIdx} spawned at {pos}".ToLog();
            return objData;
        }

        #region Proxy Object Control
        private readonly Dictionary<int, Queue<RecycleableMonobehaviour>> m_TerminatedProxies = new Dictionary<int, Queue<RecycleableMonobehaviour>>();
        unsafe private void RequestProxy(Hash objHash, Hash trHash, Action<DataGameObject, RecycleableMonobehaviour> onCompleted)
        {
            if (m_LoadingLock) return;
            if (!m_MappedGameObjectIdxes.ContainsKey(objHash) || !m_MappedTransformIdxes.ContainsKey(trHash)) return;

            ref DataTransform tr = ref *GetDataTransformPointer(trHash);
            tr.m_ProxyIdx = DataTransform.ProxyQueued;
            int prefabIdx = tr.m_PrefabIdx;
            
            m_RequestedJobs.Enqueue(() =>
            {
                if (!m_TerminatedProxies.TryGetValue(prefabIdx, out Queue<RecycleableMonobehaviour> pool) ||
                    pool.Count == 0)
                {
                    InstantiatePrefab(prefabIdx, (other) =>
                    {
                        ref DataTransform tr = ref *GetDataTransformPointer(trHash);
                        tr.m_ProxyIdx = new int2(tr.m_PrefabIdx, other.m_Idx);

                        other.transform.position = tr.m_Position;
                        other.transform.rotation = tr.m_Rotation;
                        other.transform.localScale = tr.m_LocalScale;

                        ProxyMonoComponent datas = other.GetComponent<ProxyMonoComponent>();
                        if (datas == null)
                        {
                            datas = other.gameObject.AddComponent<ProxyMonoComponent>();
                        }

                        datas.m_GameObject = objHash;
                        onCompleted?.Invoke(m_MappedGameObjects[m_MappedGameObjectIdxes[objHash]], other);

                        tr.gameObject.OnProxyCreated();
                        OnDataObjectProxyCreated?.Invoke(tr.gameObject);
                    });
                }
                else
                {
                    RecycleableMonobehaviour other = pool.Dequeue();

                    ref DataTransform tr = ref *GetDataTransformPointer(trHash);
                    tr.m_ProxyIdx = new int2(tr.m_PrefabIdx, other.m_Idx);

                    other.transform.position = tr.m_Position;
                    other.transform.rotation = tr.m_Rotation;
                    other.transform.localScale = tr.m_LocalScale;

                    ProxyMonoComponent datas = other.GetComponent<ProxyMonoComponent>();
                    if (datas == null)
                    {
                        datas = other.gameObject.AddComponent<ProxyMonoComponent>();
                    }

                    datas.m_GameObject = objHash;
                    if (other.InitializeOnCall) other.Initialize();
                    onCompleted?.Invoke(m_MappedGameObjects[m_MappedGameObjectIdxes[objHash]], other);

                    tr.gameObject.OnProxyCreated();
                    OnDataObjectProxyCreated?.Invoke(tr.gameObject);
                }
            });
        }
        unsafe private void RemoveProxy(Hash trHash)
        {
            if (m_LoadingLock) return;

            //ref DataTransform tr = ref *GetDataTransformPointer(trHash);
            //int2 proxyIdx = tr.m_ProxyIdx;
            //CoreSystem.Logger.False(proxyIdx.Equals(DataTransform.ProxyNull), $"proxy index null {proxyIdx}");

            //tr.gameObject.OnProxyRemoved();
            //OnDataObjectProxyRemoved?.Invoke(tr.gameObject);
            //tr.m_ProxyIdx = DataTransform.ProxyNull;

            m_RequestedJobs.Enqueue(() =>
            {
                ref DataTransform tr = ref *GetDataTransformPointer(trHash);
                int2 proxyIdx = tr.m_ProxyIdx;
                CoreSystem.Logger.False(proxyIdx.Equals(DataTransform.ProxyNull), $"proxy index null {proxyIdx}");

                tr.gameObject.OnProxyRemoved();
                OnDataObjectProxyRemoved?.Invoke(tr.gameObject);
                tr.m_ProxyIdx = DataTransform.ProxyNull;

                RecycleableMonobehaviour obj;
                try
                {
                    //obj = PrefabManager.Instance.RecycleObjects[proxyIdx.x].Instances[proxyIdx.y];
                    obj = m_Instances[proxyIdx.x][proxyIdx.y];
                }
                catch (Exception)
                {
                    $"{proxyIdx}".ToLog();
                    throw;
                }
                obj.Terminate();
                obj.transform.position = INIT_POSITION;

                ProxyMonoComponent datas = obj.GetComponent<ProxyMonoComponent>();
                datas.m_GameObject = Hash.Empty;

                if (!m_TerminatedProxies.TryGetValue(proxyIdx.x, out Queue<RecycleableMonobehaviour> pool))
                {
                    pool = new Queue<RecycleableMonobehaviour>();
                    m_TerminatedProxies.Add(proxyIdx.x, pool);
                }
                pool.Enqueue(obj);
            });
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
            //oriTr.localPosition = boxed.m_LocalPosition;

            oriTr.rotation = boxed.m_Rotation;
            //oriTr.localRotation = boxed.m_LocalRotation;

            oriTr.localScale = boxed.m_LocalScale;
        }

        private void ProxyVisibleCheckPararellJob()
        {
            const int maxCountForeachJob = 10;

            if (m_LoadingLock) return;

            int listCount = m_MappedTransforms.Length;
            int div = listCount / maxCountForeachJob;
            NativeArray<DataTransform>.ReadOnly readOnly = m_MappedTransforms.AsParallelReader();
            for (int i = 0; i < div + 1; i++)
            {
                BackgroundJob job = PoolContainer<BackgroundJob>.Dequeue();
                int startIdx = i * maxCountForeachJob;
                m_VisibleCheckJobs.Add(job);
                int maxIdx = (i + 1) * maxCountForeachJob;
                if (maxIdx > listCount) maxIdx = listCount;
                //if (startIdx == maxIdx) break;

                //$"{startIdx} to {maxIdx} :: {listCount}".ToLog();

                job.Action = () =>
                {
                    for (int j = startIdx; j < maxIdx; j++)
                    {
                        if (m_LoadingLock) break;
                        //int idx = m_MappedTransformIdxes[m_MappedTransformList[j]];

                        if (readOnly[j] is DataTransform tr)
                        {
                            if (!tr.IsValid()) continue;
                            if (m_RenderSystem.IsInCameraScreen(tr.position))
                            {
                                if (!readOnly[j].ProxyRequested &&
                                    !readOnly[j].HasProxyObject)
                                {
                                    //"in".ToLog();
                                    if (tr.m_EnableCull) m_RequestProxies.Enqueue(tr);
                                }

                                if (!tr.m_IsVisible)
                                {
                                    tr.m_IsVisible = true;
                                    OnDataObjectVisibleAsync?.Invoke(tr.gameObject);
                                }
                            }
                            else
                            {
                                if (tr.m_EnableCull && readOnly[j].HasProxyObject)
                                {
                                    if (m_RemoveProxies.Contains(tr))
                                    {
                                        throw new Exception();
                                    }
                                    m_RemoveProxies.Enqueue(tr);
                                }

                                if (tr.m_IsVisible)
                                {
                                    tr.m_IsVisible = false;
                                    OnDataObjectInvisibleAsync?.Invoke(tr.gameObject);
                                }
                            }
                        }
                        else throw new NotImplementedException();
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
            int idx = m_MappedGameObjectIdxes[objHash];

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
                return *GetDataGameObjectPointer(hash);
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

        internal readonly Dictionary<int, List<RecycleableMonobehaviour>> m_Instances = new Dictionary<int, List<RecycleableMonobehaviour>>();
        private sealed class PrefabRequester
        {
            GameObjectProxySystem m_ProxySystem;
            SceneSystem m_SceneSystem;
            AssetReferenceGameObject m_RefObject;
            Scene m_RequestedScene;

            public void Setup(GameObjectProxySystem proxySystem, SceneSystem sceneSystem, int prefabIdx, Vector3 pos, Quaternion rot,
                Action<RecycleableMonobehaviour> onCompleted)
            {
                var prefabInfo = PrefabList.Instance.ObjectSettings[prefabIdx];

                m_ProxySystem = proxySystem;
                m_SceneSystem = sceneSystem;
                m_RefObject = prefabInfo.RefPrefab;
                m_RequestedScene = m_SceneSystem.CurrentScene;

                if (CoreSystem.InstanceGroupTr == null) CoreSystem.InstanceGroupTr = new GameObject("InstanceSystemGroup").transform;
                var oper = prefabInfo.RefPrefab.InstantiateAsync(pos, rot, CoreSystem.InstanceGroupTr);
                oper.Completed += (other) =>
                {
                    Scene currentScene = m_SceneSystem.CurrentScene;
                    if (!currentScene.Equals(m_RequestedScene))
                    {
                        CoreSystem.Logger.LogWarning(Channel.Presentation, $"{other.Result.name} is returned because Scene has been changed");
                        m_RefObject.ReleaseInstance(other.Result);
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

        private void InstantiatePrefab(int idx, Action<RecycleableMonobehaviour> onCompleted)
            => InstantiatePrefab(idx, INIT_POSITION, Quaternion.identity, onCompleted);
        private void InstantiatePrefab(int idx, Vector3 position, Quaternion rotation, Action<RecycleableMonobehaviour> onCompleted)
        {
            PoolContainer<PrefabRequester>.Dequeue().Setup(this, m_SceneSystem, idx, position, rotation, onCompleted);
        }

        #endregion
    }
}
