using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Mono;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace Syadeu.Mono
{
    [Obsolete("Will be deprecated soon, Use PresentationSystem<GameObjectProxySystem>", true)]
    public class PrefabManager : StaticManager<PrefabManager>
    {
        #region Initialize

        private static object s_LockObj = new object();
        internal static Vector3 INIT_POSITION = new Vector3(-9999, -9999, -9999);

        internal class RecycleObject
        {
            public int Index { get; }
            public GameObject Prefab { get; }
#if UNITY_ADDRESSABLES
            public AssetReferenceGameObject RefPrefab { get; }
#endif
            public int MaxCount { get; }
            public int InstanceCreationBlock { get; }
            public List<RecycleableMonobehaviour> Instances { get; }
#if UNITY_ADDRESSABLES
            public List<PromiseRecycleableObject> Promises { get; }
#endif
            public List<Transform> Transforms { get; }

            public Timer DeletionTimer { get; }
            public int DeletionTriggerCount { get; }

            public RecycleObject(int i, PrefabList.ObjectSetting setting)
            {
                Index = i;
                Prefab = setting.Prefab;
#if UNITY_ADDRESSABLES
                RefPrefab = setting.RefPrefab;
#endif
                MaxCount = setting.MaxInstanceCount;
                InstanceCreationBlock = setting.InstanceCreationBlock;

                DeletionTimer = new Timer()
                    .SetTargetTime(setting.DeletionWaitSeconds)
                    .OnTimerEnd(() => Instance.ReleaseTerminatedObjects(Index));

                DeletionTriggerCount = setting.DeletionTriggerCount;

                Instances = new List<RecycleableMonobehaviour>();
#if UNITY_ADDRESSABLES
                Promises = new List<PromiseRecycleableObject>();
#endif
                Transforms = new List<Transform>();
            }

            public void AddNewInstance(RecycleableMonobehaviour obj)
            {
                obj.m_Idx = Instances.Count;
                Instances.Add(obj);
                Transforms.Add(obj.transform);
            }
        }
        public override string DisplayName => "Prefab Manager";
        public override bool DontDestroy => false;
        public override bool HideInHierarchy => false;
        internal Dictionary<int, RecycleObject> RecycleObjects { get; } = new Dictionary<int, RecycleObject>();
        
        internal ConcurrentQueue<ITerminate> Terminators { get; } = new ConcurrentQueue<ITerminate>();
        public override void OnInitialize()
        {
            for (int i = 0; i < PrefabList.Instance.ObjectSettings.Count; i++)
            {
                RecycleObject obj = new RecycleObject(i, PrefabList.Instance.ObjectSettings[i]);
                RecycleObjects.Add(i, obj);
            }
        }
        public override void OnStart()
        {
            StartUnityUpdate(Updater());
            foreach (var recycle in RecycleObjects.Values)
            {
                StartBackgroundUpdate(RecycleInstancesUpdate(recycle));
            }
        }
        internal RecycleObject InternalGetRecycleObject(int idx)
        {
            if (RecycleObjects.TryGetValue(idx, out var obj))
            {
                return obj;
            }
            return null;
        }
        internal bool InternalHasRecycleObject(int idx) => InternalGetRecycleObject(idx) != null;

        private IEnumerator RecycleInstancesUpdate(RecycleObject recycle)
        {
            while (true)
            {
                int activatedCount = 0;
                for (int i = 0; i < recycle.Instances.Count; i++)
                {
                    if (recycle.Instances[i].WaitForDeletion &&
                        !recycle.Instances[i].Activated)
                    {
#if UNITY_ADDRESSABLES
                        if (recycle.Instances[i].CreatedWithAddressable)
                        {
                            CoreSystem.AddForegroundJob(() =>
                            {
                                recycle.RefPrefab.ReleaseInstance(recycle.Instances[i].gameObject);
                            });
                        }
                        else
#endif
                        {
                            SendDestroy(recycle.Instances[i]);
                        }
                        //Destroy();
                        recycle.Instances.RemoveAt(i);
                        recycle.Transforms.RemoveAt(i);
                        i--;
                        continue;
                    }

                    if (!recycle.Instances[i].Activated)
                    {
                        //if (recycle.Instances[i].transform.parent != transform)
                        //{
                        //    recycle.Instances[i].transform.SetParent(transform);
                        //}
                        continue;
                    }
                    if (CoreSystem.IsNull(recycle.Instances[i]))
                    {
                        if (SyadeuSettings.Instance.m_PMErrorAutoFix)
                        {
                            recycle.Instances.RemoveAt(i);
                            recycle.Transforms.RemoveAt(i);
                            i--;
                            continue;
                        }
                        else throw new CoreSystemException(CoreSystemExceptionFlag.RecycleObject, "PrefabManager에 의해 관리되던 RecycleMonobehaviour가 다른 객체에 의해 파괴되었습니다. 관리중인 객체는 다른 객체에서 파괴될 수 없습니다.");
                    }

                    if (recycle.Instances[i].OnActivated != null &&
                        !recycle.Instances[i].OnActivated.Invoke())
                    {
                        SendTerminate(recycle.Instances[i]);
                        //recycle.Instances[i].Terminate();
                        recycle.Instances[i].Activated = false;
                    }

                    activatedCount += 1;
                    if (i != 0 && i % 500 == 0) yield return null;
                }

                if (recycle.Instances.Count - activatedCount >= recycle.DeletionTriggerCount &&
                        !recycle.DeletionTimer.IsTimerActive())
                {
                    recycle.DeletionTimer.Start();
                }
                else if (recycle.DeletionTimer.IsTimerActive() &&
                    recycle.Instances.Count - activatedCount < recycle.DeletionTriggerCount)
                {
                    recycle.DeletionTimer.Kill();
                }

                yield return null;
            }
        }
        private void SendDestroy(RecycleableMonobehaviour obj)
        {
            CoreSystem.AddForegroundJob(() => Destroy(obj.gameObject));
        }
        private IEnumerator Updater()
        {
            while (Initialized)
            {
                if (Terminators.Count > 0)
                {
                    int c = Terminators.Count;
                    for (int i = 0; i < c; i++)
                    {
                        if (!Terminators.TryDequeue(out ITerminate obj)) continue;

                        obj.Terminate();
                    }
                }

                yield return null;
            }

            //Debug.Log("exit");
        }
        #endregion

        /// <summary>
        /// 해당 타입(<typeparamref name="T"/>)과 일치하는 리사이클 인스턴스를 받아옵니다.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetRecycleObject<T>(Transform parent = null) where T : Component
        {
            for (int i = 0; i < PrefabList.Instance.ObjectSettings.Count; i++)
            {
                if (PrefabList.Instance.ObjectSettings[i].Prefab == null)
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.RecycleObject, $"인덱스 {i} 의 Prefab 항목이 없습니다.");
                }

                T _ins = null;
                if (IsMainthread())
                {
                    if (PrefabList.Instance.ObjectSettings[i].Prefab.GetComponent<T>() != null)
                    {
                        _ins = GetRecycleObject(i).GetComponent<T>();
                        if (parent != null) _ins.transform.SetParent(parent);
                    }
                }
                else
                {
                    CoreSystem.AddForegroundJob(() =>
                    {
                        if (PrefabList.Instance.ObjectSettings[i].Prefab.GetComponent<T>() != null)
                        {
                            _ins = GetRecycleObject(i).GetComponent<T>();
                            if (parent != null) _ins.transform.SetParent(parent);
                        }
                    }).Await();
                }

                if (_ins != null) return _ins;
            }

            throw new InvalidCastException($"CoreSystem.Prefab :: {TypeHelper.TypeOf<T>.Name}와 일치하는 타입이 프리팹 리스트에 등록되지않아 찾을 수 없음");
        }
#if UNITY_ADDRESSABLES
        public static PromiseRecycleableObject GetRecycleObjectAsync<T>() where T : Component
            => GetRecycleObjectAsync<T>(null);
        public static PromiseRecycleableObject GetRecycleObjectAsync<T>(Action<RecycleableMonobehaviour> onCompleted, bool manualInit = false) where T : Component
        {
            RecycleObject obj = null;
            for (int i = 0; i < PrefabList.Instance.ObjectSettings.Count; i++)
            {
                if (PrefabList.Instance.ObjectSettings[i].Prefab == null)
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.RecycleObject, $"인덱스 {i} 의 Prefab 항목이 없습니다.");
                }

                if (PrefabList.Instance.ObjectSettings[i].Prefab.GetComponent<T>() != null)
                {
                    obj = Instance.RecycleObjects[i];
                    break;
                }
            }

            if (obj == null) throw new InvalidCastException($"CoreSystem.Prefab :: {TypeHelper.TypeOf<T>.Name}와 일치하는 타입이 프리팹 리스트에 등록되지않아 찾을 수 없음");
            for (int i = 0; i < obj.Instances.Count; i++)
            {
                if (obj.Instances[i] == null)
                {
                    if (SyadeuSettings.Instance.m_PMErrorAutoFix)
                    {
                        obj.Instances.RemoveAt(i);
                        obj.Transforms.RemoveAt(i);
                        i--;
                        continue;
                    }
                    else throw new CoreSystemException(CoreSystemExceptionFlag.RecycleObject, "PrefabManager에 의해 관리되던 RecycleMonobehaviour가 다른 객체에 의해 파괴되었습니다. 관리중인 객체는 다른 객체에서 파괴될 수 없습니다.");
                }
                else if (obj.Instances[i].Activated) continue;

                if (onCompleted != null)
                {
                    obj.Instances[i].Initialize();
                    onCompleted.Invoke(obj.Instances[i]);
                }
                //"in1".ToLog();
                return new PromiseRecycleableObject(obj.Instances[i]);
            }
            for (int i = obj.Promises.Count - 1; i >= 0; i--)
            {
                PromiseRecycleableObject target = obj.Promises[i];
                if (onCompleted != null)
                {
                    if (obj.Promises[i].IsDone)
                    {
                        //"2".ToLog();
                        obj.Promises[i].Target.Initialize();
                        onCompleted.Invoke(obj.Promises[i].Target);
                    }
                    else
                    {
                        //"1".ToLog();
                        obj.Promises[i].m_OnCompleted += onCompleted;
                    }
                }
                obj.Promises.RemoveAt(i);
                //"in2".ToLog();
                return target;
            }

            lock (s_LockObj)
            {
                if (obj.MaxCount < 0 ||
                    obj.MaxCount > obj.Instances.Count)
                {
                    PromiseRecycleableObject recycleObj = Instance.InternalInstantiateAsync(obj, onCompleted);
                    //"in3".ToLog();
                    return recycleObj;
                }
            }
            $"CoreSystem: PrefabManager Warning: 이 프리팹({obj.Prefab.name})은 최대 인스턴스 갯수에 도달하여 요청이 무시되었습니다.".ToLog();
            return null;
        }
        public static PromiseRecycleableObject GetRecycleObjectAsync(int index) => GetRecycleObjectAsync(index, null);
        public static PromiseRecycleableObject GetRecycleObjectAsync(int index, Action<RecycleableMonobehaviour> onCompleted)
        {
            RecycleObject obj = Instance.RecycleObjects[index];
            for (int i = 0; i < obj.Instances.Count; i++)
            {
                if (!obj.Instances[i].Activated)
                {
                    if (CoreSystem.GetTransform(obj.Instances[i]) == null)
                    {
                        if (SyadeuSettings.Instance.m_PMErrorAutoFix)
                        {
                            obj.Instances.RemoveAt(i);
                            obj.Transforms.RemoveAt(i);
                            i--;
                            continue;
                        }
                        else throw new CoreSystemException(CoreSystemExceptionFlag.RecycleObject, "PrefabManager에 의해 관리되던 RecycleMonobehaviour가 다른 객체에 의해 파괴되었습니다. 관리중인 객체는 다른 객체에서 파괴될 수 없습니다.");
                    }

                    if (onCompleted != null)
                    {
                        if (obj.Instances[i].InitializeOnCall) obj.Instances[i].Initialize();
                        onCompleted.Invoke(obj.Instances[i]);
                    }
                    return new PromiseRecycleableObject(obj.Instances[i]);
                }
            }

            lock (s_LockObj)
            {
                if (obj.MaxCount < 0 ||
                    obj.MaxCount > obj.Instances.Count)
                {
                    PromiseRecycleableObject recycleObj = Instance.InternalInstantiateAsync(obj, onCompleted);
                    return recycleObj;
                }
            }
            $"CoreSystem: PrefabManager Warning: 이 프리팹(인덱스: {index})은 최대 인스턴스 갯수에 도달하여 요청이 무시되었습니다.".ToLog();
            return null;
        }
#endif
        /// <summary>
        /// <see cref="PrefabList"/>에서 리스트 인덱스(<see cref="PrefabList.m_ObjectSettings"/>)값으로 
        /// 재사용 인스턴스를 받아옵니다.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public static RecycleableMonobehaviour GetRecycleObject(int index, bool initOnCall = true)
        {
            RecycleObject obj = Instance.RecycleObjects[index];

            for (int i = 0; i < obj.Instances.Count; i++)
            {
                if (!obj.Instances[i].Activated)
                {
                    if (CoreSystem.GetTransform(obj.Instances[i]) == null)
                    {
                        if (SyadeuSettings.Instance.m_PMErrorAutoFix)
                        {
                            obj.Instances.RemoveAt(i);
                            obj.Transforms.RemoveAt(i);
                            i--;
                            continue;
                        }
                        else throw new CoreSystemException(CoreSystemExceptionFlag.RecycleObject, "PrefabManager에 의해 관리되던 RecycleMonobehaviour가 다른 객체에 의해 파괴되었습니다. 관리중인 객체는 다른 객체에서 파괴될 수 없습니다.");
                    }

                    if (initOnCall)
                    {
                        obj.Instances[i].Initialize();
                        //obj.Instances[i].OnInitialize();
                    }

                    return obj.Instances[i];
                }
            }

            // »ý¼º
            lock (s_LockObj)
            {
                if (obj.MaxCount < 0 ||
                    obj.MaxCount > obj.Instances.Count)
                {
                    RecycleableMonobehaviour recycleObj = null;
                    if (!IsMainthread())
                    {
                        CoreSystem.AddForegroundJob(() =>
                        {
                            recycleObj = Instance.InternalInstantiate(obj, initOnCall);
                        }).Await();
                    }
                    else recycleObj = Instance.InternalInstantiate(obj, initOnCall);
                    return recycleObj;
                }
            }

            //"Return null because this item has reached maximum instance count lock".ToLog();
            $"CoreSystem: PrefabManager Warning: 이 프리팹(인덱스: {index})은 최대 인스턴스 갯수에 도달하여 요청이 무시되었습니다.".ToLog();
            return null;
        }
        private RecycleableMonobehaviour InternalInstantiate(RecycleObject obj, bool initOnCall, Action onTerminate = null)
        {
            for (int i = 0; i < obj.InstanceCreationBlock; i++)
            {
                RecycleableMonobehaviour recycleObj;
                RecycleableMonobehaviour recycleable = obj.Prefab.GetComponent<RecycleableMonobehaviour>();
                if (recycleable == null)
                {
                    recycleObj = Instantiate(obj.Prefab, transform).AddComponent<ManagedRecycleObject>();
                }
                else
                {
                    recycleObj = Instantiate(obj.Prefab, transform).GetComponent<RecycleableMonobehaviour>();
                }

                recycleObj.transform.localPosition = INIT_POSITION;
                recycleObj.InternalOnCreated();
                recycleObj.onTerminate = onTerminate;

                obj.AddNewInstance(recycleObj);
            }
            return GetRecycleObject(obj.Index, initOnCall);
        }
#if UNITY_ADDRESSABLES
        private PromiseRecycleableObject InternalInstantiateAsync(RecycleObject obj, Action<RecycleableMonobehaviour> onCompleted)
        {
            if (IsMainthread())
            {
                return Method();
            }
            else
            {
                PromiseRecycleableObject temp = null;
                CoreSystem.AddForegroundJob(() => temp = Method()).Await();
                return temp;
            }

            PromiseRecycleableObject Method()
            {
                PromiseRecycleableObject output = null;
                for (int i = 0; i < obj.InstanceCreationBlock; i++)
                {
                    if (output == null)
                    {
                        output = new PromiseRecycleableObject(obj, onCompleted);
                    }
                    else
                    {
                        obj.Promises.Add(new PromiseRecycleableObject(obj));
                    }
                }
                return output;
            }
        }
#endif
        [Obsolete]
        internal T InternalInstantitate<T>(int prefabIdx, Action onTerminate = null) where T : RecycleableMonobehaviour
        {
            RecycleObject obj = Instance.RecycleObjects[prefabIdx];
            for (int i = 0; i < obj.InstanceCreationBlock; i++)
            {
                T recycleObj;
                T recycleable = obj.Prefab.GetComponent<T>();
                if (recycleable == null)
                {
                    recycleObj = Instantiate(obj.Prefab, transform).AddComponent<T>();
                }
                else
                {
                    recycleObj = Instantiate(obj.Prefab, transform).GetComponent<T>();
                }

                recycleObj.transform.localPosition = INIT_POSITION;
                recycleObj.InternalOnCreated();
                recycleObj.onTerminate = onTerminate;

                obj.AddNewInstance(recycleObj);
            }
            return (T)GetRecycleObject(obj.Index);
        }

        /// <summary>
        /// 현재 Terminated 처리된 재사용 오브젝트들을 메모리에서 방출합니다.
        /// </summary>
        /// <returns></returns>
        public static int ReleaseTerminatedObjects()
        {
            int sum = 0;
            foreach (var recycle in Instance.RecycleObjects.Values)
            {
                for (int i = 0; i < recycle.Instances.Count; i++)
                {
                    if (!recycle.Instances[i].Activated)
                    {
                        recycle.Instances[i].WaitForDeletion = true;
                        sum += 1;
                    }
                }
            }
            return sum;
        }
        private void ReleaseTerminatedObjects(int index)
        {
            for (int i = 0; i < RecycleObjects[index].Instances.Count; i++)
            {
                if (!RecycleObjects[index].Instances[i].Activated)
                {
                    RecycleObjects[index].Instances[i].WaitForDeletion = true;
                }
            }
        }

        /// <summary>
        /// 해당 인덱스의 재사용 오브젝트들의 인스턴스 갯수를 반환합니다.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public int GetInstanceCount(int index) => RecycleObjects[index].Instances.Count;
        /// <summary>
        /// 해당 인덱스의 모든 인스턴스들을 리스트에 담아 반환합니다.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public IReadOnlyList<RecycleableMonobehaviour> GetInstances(int index)
            => RecycleObjects[index].Instances;
        /// <summary>
        /// 등록된 모든 재사용 모노 프리팹을 받아옵니다.
        /// 인덱스 번호와 프리팹을 담아서 리스트로 반환합니다.
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<KeyValuePair<int, GameObject>> GetRecycleObjectList()
        {
            KeyValuePair<int, GameObject>[] list = new KeyValuePair<int, GameObject>[RecycleObjects.Count];

            for (int i = 0; i < list.Length; i++)
            {
                list[i] = new KeyValuePair<int, GameObject>(i, RecycleObjects[i].Prefab);
            }

            return list;
        }

        public static int AddRecycleObject(PrefabList.ObjectSetting objectSetting)
        {
            PrefabList.Instance.ObjectSettings.Add(objectSetting);
            int i = PrefabList.Instance.ObjectSettings.Count;

            RecycleObject obj = new RecycleObject(i, PrefabList.Instance.ObjectSettings[i - 1]);
            Instance.RecycleObjects.Add(i, obj);

            Instance.StartBackgroundUpdate(Instance.RecycleInstancesUpdate(obj));

            return i;
        }

        public static void SendTerminate(ITerminate terminator)
        {
            Instance.Terminators.Enqueue(terminator);
        }
    }
}
