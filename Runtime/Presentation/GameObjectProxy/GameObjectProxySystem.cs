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
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

namespace Syadeu.Presentation
{
    public sealed class GameObjectProxySystem : PresentationSystemEntity<GameObjectProxySystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => true;

        internal readonly Queue<IDataComponent> m_RequireUpdateList = new Queue<IDataComponent>();
        internal readonly HashSet<IDataComponent> m_RequireUpdateQueuedList = new HashSet<IDataComponent>();
        //internal readonly Dictionary<Hash, IDataComponent> m_MappedData = new Dictionary<Hash, IDataComponent>();
        internal NativeHashMap<Hash, DataTransform> m_MappedTransforms = new NativeHashMap<Hash, DataTransform>(1000, Allocator.Persistent);
        private readonly List<Hash> m_MappedTransformList = new List<Hash>();

        private int m_VisibleCheckJobWorker;
        private BackgroundJob m_VisibleCheckJob;

        private RenderSystem m_RenderSystem;

        protected override PresentationResult OnInitialize()
        {
            m_VisibleCheckJobWorker = CoreSystem.CreateNewBackgroundJobWorker(true);
            m_VisibleCheckJob = new BackgroundJob(VisibleCheckJob);
            return base.OnInitialize();
        }
        protected override PresentationResult OnInitializeAsync()
        {
            AssemblyName aName = new AssemblyName("CoreSystem_Runtime");
            AssemblyBuilder ab = AppDomain.CurrentDomain.DefineDynamicAssembly(aName, AssemblyBuilderAccess.Run);
            m_ModuleBuilder = ab.DefineDynamicModule(aName.Name);

            RequestSystem<RenderSystem>((other) => m_RenderSystem = other);

            return base.OnInitializeAsync();
        }
        protected override PresentationResult OnStartPresentation()
        {
            return base.OnStartPresentation();
        }

        protected override PresentationResult AfterPresentation()
        {
            int updateCount = m_RequireUpdateList.Count;
            for (int i = 0; i < updateCount; i++)
            {
                IDataComponent data = m_RequireUpdateList.Dequeue();
                //$"update in {data.Idx}".ToLog();
                if (!data.HasProxyObject) continue;

                switch (data.Type)
                {
                    //case DataComponentType.Component:
                    //    break;
                    case DataComponentType.Transform:
                        UpdateDataTransform(data.Idx);
                        break;
                    default:
                        $"{data.Type}".ToLog();
                        //throw new Exception();
                        break;
                }
                m_RequireUpdateQueuedList.Remove(data);

                //if (i != 0 && i % 50 == 0) break;
            }

            int jobCount = m_RequestedJobs.Count;
            for (int i = 0; i < jobCount; i++)
            {
                if (!m_RequestedJobs.TryDequeue(out Action job)) continue;
                job.Invoke();
            }

            return base.AfterPresentation();
        }
        private readonly ConcurrentQueue<Action> m_RequestedJobs = new ConcurrentQueue<Action>();
        private readonly ConcurrentQueue<IDataComponent> m_RequestProxies = new ConcurrentQueue<IDataComponent>();
        private readonly ConcurrentQueue<IDataComponent> m_RemoveProxies = new ConcurrentQueue<IDataComponent>();
        protected override PresentationResult AfterPresentationAsync()
        {
            int temp1 = m_RequestProxies.Count;
            for (int i = 0; i < temp1; i++)
            {
                if (!m_RequestProxies.TryDequeue(out var data)) continue;
                CoreSystem.Logger.NotNull(data);

                RequestProxy(data.Idx);
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

            if (m_VisibleCheckJob.IsDone)
            {
                m_VisibleCheckJob.Start(m_VisibleCheckJobWorker);
            }

            return base.AfterPresentationAsync();
        }
        public override void Dispose()
        {
            CoreSystem.RemoveBackgroundJobWorker(m_VisibleCheckJobWorker);
            m_MappedTransforms.Dispose();

            base.Dispose();
        }

        readonly List<BackgroundJob> jobs = new List<BackgroundJob>();
        private void VisibleCheckJob()
        {
            const int maxCountForeachJob = 10;

            int listCount = m_MappedTransformList.Count;
            int div = listCount / maxCountForeachJob;
            for (int i = 0; i < div + 1; i++)
            {
                BackgroundJob job = PoolContainer<BackgroundJob>.Dequeue();
                int startIdx = i * maxCountForeachJob;
                jobs.Add(job);
                int maxIdx = (i + 1) * maxCountForeachJob;
                if (maxIdx > listCount) maxIdx = listCount;
                //if (startIdx == maxIdx) break;

                $"{startIdx} to {maxIdx} :: {listCount}".ToLog();

                job.Action = () =>
                {
                    for (int j = startIdx; j < maxIdx; j++)
                    {
                        if (m_MappedTransforms[m_MappedTransformList[j]] is DataTransform tr)
                        {
                            if (m_RenderSystem.IsInCameraScreen(tr.position))
                            {
                                if (!m_MappedTransforms[m_MappedTransformList[j]].ProxyRequested && 
                                    !m_MappedTransforms[m_MappedTransformList[j]].HasProxyObject)
                                {
                                    //"in".ToLog();
                                    m_RequestProxies.Enqueue(tr);
                                }
                            }
                            else
                            {
                                if (m_MappedTransforms[m_MappedTransformList[j]].HasProxyObject)
                                {
                                    if (m_RemoveProxies.Contains(tr))
                                    {
                                        throw new Exception();
                                    }
                                    m_RemoveProxies.Enqueue(tr);
                                }
                            }
                        }
                        else throw new NotImplementedException();
                    }
                    PoolContainer<BackgroundJob>.Enqueue(job);
                };
                job.Start();
            }

            for (int i = 0; i < jobs.Count; i++)
            {
                jobs[i].Await();
            }
            jobs.Clear();
            //for (int i = 0; i < tempList.Count; i++)
            //{
            //    if (tempList[i] is DataMonoBehaviour mono)
            //    {
            //        //DataTransform tr = (DataTransform)m_MappedData[mono.m_Transform];

            //    }
            //    else if (tempList[i] is DataTransform tr)
            //    {
            //        if (m_RenderSystem.IsInCameraScreen(tr.position))
            //        {
            //            if (!tempList[i].ProxyRequested && !tempList[i].HasProxyObject)
            //            {
            //                //"in".ToLog();
            //                m_RequestProxies.Add(tr);
            //            }
            //        }
            //        else
            //        {
            //            if (tempList[i].HasProxyObject)
            //            {
            //                m_RemoveProxies.Add(tr);
            //            }
            //        }
            //    }
            //    else throw new NotImplementedException();
            //}
        }
        public DataMonoBehaviour CreateNewPrefab(int prefabIdx, Vector3 pos, Quaternion rot, Vector3 localScale)
        {
            Hash trHash = Hash.NewHash();
            Hash monoHash = Hash.NewHash();

            int2 proxyIdx = DataMonoBehaviour.ProxyNull;
            if (m_RenderSystem.IsInCameraScreen(pos))
            {
                proxyIdx = DataMonoBehaviour.ProxyQueued;
            }

            DataTransform trData = new DataTransform()
            {
                m_Idx = trHash,
                m_ProxyIdx = proxyIdx,
                m_PrefabIdx = prefabIdx,

                m_Position = new ThreadSafe.Vector3(pos),
                m_Rotation = rot,
                m_LocalScale = new ThreadSafe.Vector3(localScale),
            };
            DataMonoBehaviour monoData = new DataMonoBehaviour()
            {
                m_Idx = monoHash,

                m_Transform = trHash
            };
            //m_MappedData.Add(trHash, trData);
            //m_MappedData.Add(monoHash, monoData);
            m_MappedTransformList.Add(trHash);
            //m_MappedDataList.Add(monoHash);
            m_MappedTransforms.Add(trHash, trData);
            if (proxyIdx.Equals(DataMonoBehaviour.ProxyQueued))
            {
                PrefabManager.GetRecycleObjectAsync(prefabIdx, (other) =>
                {
                    DataTransform tr;
                    tr = (DataTransform)m_MappedTransforms[trHash];
                    tr.m_ProxyIdx = new int2(tr.m_PrefabIdx, other.m_Idx);
                    m_MappedTransforms[trHash] = tr;

                    other.transform.position = tr.m_Position;
                    other.transform.rotation = tr.m_Rotation;
                    other.transform.localScale = tr.m_LocalScale;
                });
            }

            $"{prefabIdx} spawned at {pos}".ToLog();
            return monoData;
        }
        private void RequestProxy(Hash trHash)
        {
            DataTransform tr = (DataTransform)m_MappedTransforms[trHash];

            tr.m_ProxyIdx = DataMonoBehaviour.ProxyQueued;
            m_MappedTransforms[trHash] = tr;

            m_RequestedJobs.Enqueue(() =>
            {
                PrefabManager.GetRecycleObjectAsync(tr.m_PrefabIdx, (other) =>
                {
                    DataTransform tr;
                    //lock (m_MappedData)
                    {
                        tr = (DataTransform)m_MappedTransforms[trHash];
                        tr.m_ProxyIdx = new int2(tr.m_PrefabIdx, other.m_Idx);
                        m_MappedTransforms[trHash] = tr;
                    }

                    other.transform.position = tr.m_Position;
                    other.transform.rotation = tr.m_Rotation;
                    other.transform.localScale = tr.m_LocalScale;
                });
            });
            
            //$"request in {trHash}".ToLog();
        }
        private void RemoveProxy(Hash trHash)
        {
            DataTransform tr = (DataTransform)m_MappedTransforms[trHash];
            int2 proxyIdx = tr.m_ProxyIdx;
            CoreSystem.Logger.False(proxyIdx.Equals(DataMonoBehaviour.ProxyNull), $"proxy index null {proxyIdx}");

            tr.m_ProxyIdx = DataMonoBehaviour.ProxyNull;
            m_MappedTransforms[trHash] = tr;

            m_RequestedJobs.Enqueue(() =>
            {
                RecycleableMonobehaviour obj;
                try
                {
                    obj = PrefabManager.Instance.RecycleObjects[proxyIdx.x].Instances[proxyIdx.y];
                }
                catch (Exception)
                {
                    $"{proxyIdx}".ToLog();
                    throw;
                }
                obj.Terminate();
                obj.transform.position = PrefabManager.INIT_POSITION;
            });
            
            //$"terminate in {trHash}".ToLog();
        }

        //public void RequestPrefab(int prefabIdx, Vector3 pos, Quaternion rot, 
        //    Action<DataMonoBehaviour> onCompleted)
        //{
        //    PrefabManager.GetRecycleObjectAsync(prefabIdx, (other) =>
        //    {
        //        Transform tr = other.transform;
        //        tr.position = pos;
        //        tr.rotation = rot;

        //        int3 
        //            monoIdx = new int3(prefabIdx, other.m_Idx, (int)DataComponentType.Component),
        //            trIdx = new int3(prefabIdx, other.m_Idx, (int)DataComponentType.Transform);

        //        //int trID = tr.GetInstanceID();
        //        if (!m_MappedData.ContainsKey(trIdx))
        //        {
        //            DataTransform trData = new DataTransform()
        //            {
        //                m_Idx = trIdx
        //            };

        //            m_MappedData.Add(trIdx, trData);
        //            DownloadDataTransform(trIdx);
        //        }
        //        if (!m_MappedData.ContainsKey(monoIdx))
        //        {
        //            DataMonoBehaviour mono = new DataMonoBehaviour()
        //            {
        //                m_Hash = Hash.NewHash(),

        //                m_Idx = monoIdx,
        //                m_Transform = trIdx
        //            };

        //            m_MappedData.Add(monoIdx, mono);
        //        }

        //        $"{((DataTransform)m_MappedData[trIdx]).m_Position}".ToLog();

        //        onCompleted?.Invoke((DataMonoBehaviour)m_MappedData[monoIdx]);
        //    });
        //}

        private void DownloadDataTransform(Hash trIdx)
        {
            DataTransform boxed = (DataTransform)m_MappedTransforms[trIdx];
            //Transform oriTr = PrefabManager.Instance.RecycleObjects[boxed.m_Idx.x].Instances[boxed.m_Idx.y].transform;
            Transform oriTr = boxed.ProxyObject;

            boxed.m_Position = new ThreadSafe.Vector3(oriTr.position);
            //boxed.m_LocalPosition = new ThreadSafe.Vector3(oriTr.localPosition);

            //boxed.m_EulerAngles = new ThreadSafe.Vector3(oriTr.eulerAngles);
            //boxed.m_LocalEulerAngles = new ThreadSafe.Vector3(oriTr.localEulerAngles);
            boxed.m_Rotation = oriTr.rotation;
            //boxed.m_LocalRotation = oriTr.localRotation;

            boxed.m_Right = new ThreadSafe.Vector3(oriTr.right);
            boxed.m_Up = new ThreadSafe.Vector3(oriTr.up);
            boxed.m_Forward = new ThreadSafe.Vector3(oriTr.forward);

            //boxed.m_LossyScale = new ThreadSafe.Vector3(oriTr.lossyScale);
            boxed.m_LocalScale = new ThreadSafe.Vector3(oriTr.localScale);

            m_MappedTransforms[trIdx] = boxed;
        }
        private void UpdateDataTransform(Hash trIdx)
        {
            DataTransform boxed = (DataTransform)m_MappedTransforms[trIdx];
            //Transform oriTr = PrefabManager.Instance.RecycleObjects[boxed.m_Idx.x].Instances[boxed.m_Idx.y].transform;
            Transform oriTr = boxed.ProxyObject;

            $"1 . {oriTr.position} => {boxed.m_Position}".ToLog(oriTr);
            oriTr.position = boxed.m_Position;
            
            //oriTr.localPosition = boxed.m_LocalPosition;

            //oriTr.eulerAngles = boxed.m_EulerAngles;
            //oriTr.localEulerAngles = boxed.m_LocalEulerAngles;
            oriTr.rotation = boxed.m_Rotation;
            //oriTr.localRotation = boxed.m_LocalRotation;

            //oriTr.right = boxed.m_Right;
            //oriTr.up = boxed.m_Up;
            //oriTr.forward = boxed.m_Forward;

            oriTr.localScale = boxed.m_LocalScale;
            $"2 . {oriTr.position} => {boxed.m_Position}".ToLog(oriTr);
        }

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
    }

    public abstract class MonoBehaviour<T> : MonoBehaviour
    {
        [SerializeReference] public T m_Value;

        private void Start()
        {
            "TestMono DONE".ToLog();
        }
    }
}
