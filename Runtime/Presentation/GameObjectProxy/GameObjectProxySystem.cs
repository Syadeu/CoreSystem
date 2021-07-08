using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Mono;
using Syadeu.Presentation.Entities;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation
{
    public sealed class GameObjectProxySystem : PresentationSystemEntity<GameObjectProxySystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private ModuleBuilder m_ModuleBuilder;
        private readonly Dictionary<Type, GenericType> m_GenericTypes = new Dictionary<Type, GenericType>();
        internal Dictionary<int3, IDataComponent> m_MappedData = new Dictionary<int3, IDataComponent>();
        //internal NativeHashMap<Hash, DataTransform> m_MappedTrData;
        //private readonly Dictionary<int, Queue<DataMonoBehaviour>> m_MonoDataPool = new Dictionary<int, Queue<DataMonoBehaviour>>();

        //private NativeList<DataMonoBehaviour> m_PinnedMonoData;
        internal Queue<IDataComponent> m_RequireUpdateList = new Queue<IDataComponent>();

        public override PresentationResult OnInitialize()
        {
            //m_PinnedMonoData = new NativeList<DataMonoBehaviour>(1, Allocator.Persistent);
            //m_MappedData = new NativeHashMap<Hash, IDataComponent>(1, Allocator.Persistent);
            //m_MappedTrData = new NativeHashMap<Hash, DataTransform>(1, Allocator.Persistent);
            return base.OnInitialize();
        }
        public override PresentationResult OnInitializeAsync()
        {
            AssemblyName aName = new AssemblyName("CoreSystem_Runtime");
            AssemblyBuilder ab = AppDomain.CurrentDomain.DefineDynamicAssembly(aName, AssemblyBuilderAccess.Run);
            m_ModuleBuilder = ab.DefineDynamicModule(aName.Name);

            return base.OnInitializeAsync();
        }
        public override PresentationResult OnStartPresentation()
        {
            return base.OnStartPresentation();
        }

        public override PresentationResult AfterPresentation()
        {
            int updateCount = m_RequireUpdateList.Count;
            for (int i = 0; i < updateCount; i++)
            {
                IDataComponent data = m_RequireUpdateList.Dequeue();
                switch (data.Type)
                {
                    //case DataComponentType.Component:
                    //    break;
                    case DataComponentType.Transform:
                        UpdateDataTransform(data.Idx);
                        break;
                    default:
                        throw new Exception();
                        //break;
                }

                //if (i != 0 && i % 50 == 0) break;
            }
            return base.AfterPresentation();
        }
        //public override void Dispose()
        //{
        //    m_PinnedMonoData.Dispose();
        //    //m_MappedData.Dispose();
        //    //m_MappedTrData.Dispose();
        //}

        
        public void RequestPrefab(int prefabIdx, Vector3 pos, Quaternion rot, 
            Action<DataMonoBehaviour> onCompleted)
        {
            PrefabManager.GetRecycleObjectAsync(prefabIdx, (other) =>
            {
                Transform tr = other.transform;
                tr.position = pos;
                tr.rotation = rot;

                int3 
                    monoIdx = new int3(prefabIdx, other.m_Idx, (int)DataComponentType.Component),
                    trIdx = new int3(prefabIdx, other.m_Idx, (int)DataComponentType.Transform);

                //int trID = tr.GetInstanceID();
                if (!m_MappedData.ContainsKey(trIdx))
                {
                    DataTransform trData = new DataTransform()
                    {
                        m_Idx = trIdx
                    };

                    m_MappedData.Add(trIdx, trData);
                }
                if (!m_MappedData.ContainsKey(monoIdx))
                {
                    DataMonoBehaviour mono = new DataMonoBehaviour()
                    {
                        m_Hash = Hash.NewHash(),

                        m_Idx = monoIdx,
                        m_Transform = trIdx
                    };

                    m_MappedData.Add(monoIdx, mono);
                }

                DownloadDataTransform(trIdx);
                $"{((DataTransform)m_MappedData[trIdx]).position}".ToLog();

                onCompleted?.Invoke((DataMonoBehaviour)m_MappedData[monoIdx]);
            });
        }
        //public DataMonoBehaviour GetPrefab(int prefabIdx)
        //{
        //    return m_MonoDataPool[prefabIdx].Dequeue();
        //}

        private void DownloadDataTransform(int3 trIdx)
        {
            DataTransform boxed = (DataTransform)m_MappedData[trIdx];
            Transform oriTr = PrefabManager.Instance.RecycleObjects[boxed.m_Idx.x].Instances[boxed.m_Idx.y].transform;

            boxed.position = new ThreadSafe.Vector3(oriTr.position);
            boxed.localPosition = new ThreadSafe.Vector3(oriTr.localPosition);

            boxed.eulerAngles = new ThreadSafe.Vector3(oriTr.eulerAngles);
            boxed.localEulerAngles = new ThreadSafe.Vector3(oriTr.localEulerAngles);
            boxed.rotation = oriTr.rotation;
            boxed.localRotation = oriTr.localRotation;

            boxed.right = new ThreadSafe.Vector3(oriTr.right);
            boxed.up = new ThreadSafe.Vector3(oriTr.up);
            boxed.forward = new ThreadSafe.Vector3(oriTr.forward);

            boxed.lossyScale = new ThreadSafe.Vector3(oriTr.lossyScale);
            boxed.localScale = new ThreadSafe.Vector3(oriTr.localScale);

            m_MappedData[trIdx] = boxed;
        }
        private void UpdateDataTransform(int3 trIdx)
        {
            DataTransform boxed = (DataTransform)m_MappedData[trIdx];
            Transform oriTr = PrefabManager.Instance.RecycleObjects[boxed.m_Idx.x].Instances[boxed.m_Idx.y].transform;

            oriTr.position = boxed.position;
            oriTr.localPosition = boxed.localPosition;

            oriTr.eulerAngles = boxed.eulerAngles;
            oriTr.localEulerAngles = boxed.localEulerAngles;
            oriTr.rotation = boxed.rotation;
            oriTr.localRotation = boxed.localRotation;

            oriTr.right = boxed.right;
            oriTr.up = boxed.up;
            oriTr.forward = boxed.forward;

            oriTr.localScale = boxed.localScale;
        }

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
