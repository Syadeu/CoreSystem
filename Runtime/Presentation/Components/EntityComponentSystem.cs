#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using Syadeu.Internal;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.Components
{
    /// <summary>
    /// <see cref="EntityData{T}"/> 컴포넌트 시스템입니다.
    /// </summary>
    /// <remarks>
    /// 컴포넌트는 Dispose 패턴을 따르며, <seealso cref="IDisposable"/> 를 컴포넌트가 상속받고 있으면
    /// 체크하여 해당 컴포넌트가 제거될 시 수행됩니다. <seealso cref="INotifyComponent{TComponent}"/> 를 통해
    /// 상속받는 <seealso cref="ObjectBase"/> 가 파괴될 시 같이 파괴되도록 수행할 수 있습니다.<br/>
    /// <br/>
    /// 사용자는 직접 이 시스템을 통하여 컴포넌트 관련 작업을 수행하는 것이 아닌, <seealso cref="EntityData{T}.AddComponent{TComponent}(in TComponent)"/>
    /// 등과 같은 간접 메소드를 통해 작업을 수행할 수 있습니다. 자세한 기능은 <seealso cref="EntityData{T}"/> 를 참조하세요.
    /// </remarks>
    unsafe internal sealed class EntityComponentSystem : PresentationSystemEntity<EntityComponentSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private NativeArray<ComponentBuffer> m_ComponentArrayBuffer;
        private NativeQueue<DisposedComponent> m_DisposedComponents;
        //private MethodInfo m_RemoveComponentMethod;

        /// <summary>
        /// Key => <see cref="ComponentBuffer.TypeInfo"/>.GetHashCode()<br/>
        /// Value => <see cref="ComponentBuffer.m_ComponentBuffer"/> index
        /// </summary>
        private UnsafeMultiHashMap<int, int> m_ComponentHashMap;

        private Unity.Mathematics.Random m_Random;

#if DEBUG_MODE
        private static Unity.Profiling.ProfilerMarker
            s_GetComponentMarker = new Unity.Profiling.ProfilerMarker("get_GetComponent"),
            s_GetComponentReadOnlyMarker = new Unity.Profiling.ProfilerMarker("get_GetComponentReadOnly"),
            s_GetComponentPointerMarker = new Unity.Profiling.ProfilerMarker("get_GetComponentPointer");
#endif

        private EntitySystem m_EntitySystem;
        private SceneSystem m_SceneSystem;

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            // Type Hashing 을 위해 랜덤 생성자를 만듭니다.
            // 왜인지 모르겠지만 간혈적으로 Type.GetHashCode() 가 0 을 반환하여, 직접 값을 만듭니다.
            m_Random = new Unity.Mathematics.Random();
            m_Random.InitState();

            RequestSystem<DefaultPresentationGroup, EntitySystem>(Bind);
            RequestSystem<DefaultPresentationGroup, SceneSystem>(Bind);

            // 버퍼를 생성하기 위해 미리 모든 컴포넌트 타입들의 정보를 가져옵니다.
            Type[] types = TypeHelper.GetTypes(CollectTypes<IEntityComponent>);

            // Hashing 을 위해 최소 버퍼 사이즈인 512 값보다 낮으면 강제로 버퍼 사이즈를 1024 로 맞춥니다.
            int length;
            if (types.Length < 512)
            {
                length = 1024;
            }
            else length = types.Length * 2;

            ComponentBuffer[] tempBuffer = new ComponentBuffer[length];
            for (int i = 0; i < types.Length; i++)
            {
                ComponentBuffer buffer = BuildComponentBuffer(types[i], length, out int idx);
                tempBuffer[idx] = buffer;
            }

            m_ComponentArrayBuffer = new NativeArray<ComponentBuffer>(tempBuffer, Allocator.Persistent);
            m_DisposedComponents = new NativeQueue<DisposedComponent>(Allocator.Persistent);

            m_ComponentHashMap = new UnsafeMultiHashMap<int, int>(length, AllocatorManager.Persistent);
            ref UntypedUnsafeHashMap hashMap =
                ref UnsafeUtility.As<UnsafeMultiHashMap<int, int>, UntypedUnsafeHashMap>(ref m_ComponentHashMap);

            DisposedComponent.Initialize(
                (ComponentBuffer*)m_ComponentArrayBuffer.GetUnsafePtr(),
                (UntypedUnsafeHashMap*)UnsafeUtility.AddressOf(ref hashMap));

            ConstructSharedStatics();

            PresentationManager.Instance.PreUpdate += CompleteAllDisposedComponents;

            return base.OnInitialize();
        }
        private ComponentBuffer BuildComponentBuffer(Type componentType, in int totalLength, out int idx)
        {
            int hashCode = CreateHashCode();
            idx = math.abs(hashCode) % totalLength;

            if (IsZeroSizeStruct(componentType))
            {
                CoreSystem.Logger.LogError(Channel.Component,
                    $"Zero sized wrapper struct({TypeHelper.ToString(componentType)}) is in component list.");
            }
            
            if (/*!UnsafeUtility.IsBlittable(componentType) ||*/ !UnsafeUtility.IsUnmanaged(componentType))
            {
                CoreSystem.Logger.LogError(Channel.Component,
                    $"managed struct({TypeHelper.ToString(componentType)}) is in component list.");

                return default(ComponentBuffer);
            }

            ComponentBuffer temp = new ComponentBuffer();

            // 왜인지는 모르겠지만 Type.GetHashCode() 의 정보가 런타임 중 간혹 유효하지 않은 값 (0) 을 뱉어서 미리 파싱합니다.
            TypeInfo runtimeTypeInfo 
                = TypeInfo.Construct(componentType, idx, UnsafeUtility.SizeOf(componentType), AlignOf(componentType), hashCode);
            ComponentType.GetValue(componentType).Data = runtimeTypeInfo;

            ComponentTypeQuery.s_All = ComponentTypeQuery.s_All.Add(runtimeTypeInfo);
            
            // 새로운 버퍼를 생성하고, heap 에 메모리를 할당합니다.
            temp.Initialize(runtimeTypeInfo, runtimeTypeInfo.Size, runtimeTypeInfo.Align);

            return temp;
        }
        private void ConstructSharedStatics()
        {
            Constants.Value.Data.SystemID = SystemID;

            //UntypedUnsafeHashMap test = new UntypedUnsafeHashMap();
            
        }
        
        public override void OnDispose()
        {
            PresentationManager.Instance.PreUpdate -= CompleteAllDisposedComponents;
            m_SceneSystem.OnSceneChangeCalled -= CompleteAllDisposedComponents;

            int count = m_DisposedComponents.Count;
            for (int i = 0; i < count; i++)
            {
                m_DisposedComponents.Dequeue().Dispose();
            }
            m_DisposedComponents.Dispose();

            for (int i = 0; i < m_ComponentArrayBuffer.Length; i++)
            {
                if (!m_ComponentArrayBuffer[i].IsCreated) continue;

                m_ComponentArrayBuffer[i].Dispose();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                ComponentBufferAtomicSafety safety
                    = CLSTypedDictionary<ComponentBufferAtomicSafety>.GetValue(m_ComponentArrayBuffer[i].TypeInfo.Type);

                if (!safety.Disposed)
                {
                    if (!CoreSystem.BlockCreateInstance)
                    {
                        CoreSystem.Logger.LogError(Channel.Component,
                            $"Component({TypeHelper.ToString(m_ComponentArrayBuffer[i].TypeInfo.Type)}) buffer has not safely disposed.");
                    }

                    safety.Dispose();
                }
#endif
            }
            m_ComponentArrayBuffer.Dispose();

            m_ComponentHashMap.Dispose();

            m_EntitySystem = null;
            m_SceneSystem = null;
        }

        #region Binds

        private void Bind(EntitySystem other)
        {
            m_EntitySystem = other;
        }
        private void Bind(SceneSystem other)
        {
            m_SceneSystem = other;
            m_SceneSystem.OnSceneChangeCalled += CompleteAllDisposedComponents;
        }

        #endregion

        private void CompleteAllDisposedComponents()
        {
            if (m_DisposedComponents.Count > 0)
            {
                // Parallel Job 이 수행되고 있는 중에 컴포넌트가 제거되면 안되므로
                // 먼저 모든 Job 을 완료합니다.
                IJobParallelForEntitiesExtensions.CompleteAllJobs();

                int count = m_DisposedComponents.Count;
                for (int i = 0; i < count; i++)
                {
                    m_DisposedComponents.Dequeue().Dispose();
                }
            }
        }

        #endregion

        #region Hashing

        public int CreateHashCode() => m_Random.NextInt(int.MinValue, int.MaxValue);

        private static int GetComponentIndex<TComponent>()
        {
            int idx = ComponentType<TComponent>.Index;
#if DEBUG_MODE
            if (idx == 0)
            {
                CoreSystem.Logger.LogError(Channel.Component,
                    $"Component buffer error. " +
                    $"Didn\'t collected this component({TypeHelper.TypeOf<TComponent>.Name}) infomation at initializing stage.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
#endif
            return idx;
        }
        private static int GetComponentIndex(Type t)
        {
            int idx = ComponentType.GetValue(t).Data.Index;
#if DEBUG_MODE
            if (idx == 0)
            {
                CoreSystem.Logger.LogError(Channel.Component,
                    $"Component buffer error. " +
                    $"Didn\'t collected this component({t.Name}) infomation at initializing stage.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
#endif
            return idx;
        }
        private static int GetEntityIndex(EntityData<IEntityData> entity)
        {
            int idx = math.abs(entity.GetHashCode()) % ComponentBuffer.c_InitialCount;
            if (idx == 0)
            {
                $"err {entity.GetHashCode()}".ToLogError();
            }
            return idx;
        }
        private static int2 GetIndex(Type t, EntityData<IEntityData> entity)
        {
            int
                cIdx = GetComponentIndex(t),
                eIdx = GetEntityIndex(entity);

            return new int2(cIdx, eIdx);
        }
        private static int2 GetIndex<TComponent>(EntityData<IEntityData> entity)
        {
            int
                cIdx = GetComponentIndex<TComponent>(),
                eIdx = GetEntityIndex(entity);

            return new int2(cIdx, eIdx);
        }

        #endregion

        #region Component Methods

        /// <summary>
        /// DEBUG_MODE 중에 해당 컴포넌트가 제대로 사용될 수 있는지 확인합니다.
        /// </summary>
        /// <typeparam name="TComponent"></typeparam>
        /// <param name="result"></param>
        public void ComponentBufferSafetyCheck<TComponent>(out bool result)
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (ComponentType<TComponent>.Index == 0)
            {
                CoreSystem.Logger.LogError(Channel.Component,
                    $"Component buffer error. " +
                    $"Didn\'t collected this component({TypeHelper.TypeOf<TComponent>.ToString()}) infomation at initializing stage.");

                result = false;
                return;
            }
#endif
            result = true;
        }
        public ComponentBuffer GetComponentBuffer<TComponent>()
        {
            int idx = GetComponentIndex<TComponent>();

            return UnsafeUtility.ReadArrayElement<ComponentBuffer>(m_ComponentArrayBuffer.GetUnsafeReadOnlyPtr(), idx);
        }
        public ComponentBuffer* GetComponentBufferPointer<TComponent>()
        {
            int idx = GetComponentIndex<TComponent>();

            return ((ComponentBuffer*)m_ComponentArrayBuffer.GetUnsafeReadOnlyPtr()) + idx;
        }
        public IntPtr GetComponentBufferPointerIntPtr<TComponent>()
        {
            int idx = GetComponentIndex<TComponent>();

            return (IntPtr)((ComponentBuffer*)m_ComponentArrayBuffer.GetUnsafeReadOnlyPtr() + idx);
        }

        public TComponent AddComponent<TComponent>(in EntityData<IEntityData> entity, in TComponent data) where TComponent : unmanaged, IEntityComponent
        {
            CoreSystem.Logger.ThreadBlock(nameof(AddComponent), ThreadInfo.Unity);

            int2 index = GetIndex<TComponent>(entity);
#if DEBUG_MODE
            if (!m_ComponentArrayBuffer[index.x].IsCreated)
            {
                CoreSystem.Logger.LogError(Channel.Component,
                    $"Component buffer error. " +
                    $"Didn\'t collected this component({TypeHelper.TypeOf<TComponent>.Name}) infomation at initializing stage.");

                return data;
            }
#endif
            if (!m_ComponentArrayBuffer[index.x].Find(entity, ref index.y) &&
                !m_ComponentArrayBuffer[index.x].FindEmpty(entity, ref index.y))
            {
                ComponentBuffer boxed = m_ComponentArrayBuffer[index.x];
                boxed.Increment<TComponent>();
                m_ComponentArrayBuffer[index.x] = boxed;

                if (!m_ComponentArrayBuffer[index.x].FindEmpty(entity, ref index.y))
                {
                    CoreSystem.Logger.LogError(Channel.Component,
                        $"Component buffer error. " +
                        $"Component({TypeHelper.TypeOf<TComponent>.Name}) Hash has been conflected twice. Maybe need to increase default buffer size?");

                    throw new InvalidOperationException($"Component buffer error. See Error Log.");
                }
            }

            ((TComponent*)m_ComponentArrayBuffer[index.x].m_ComponentBuffer)[index.y] = data;
            m_ComponentArrayBuffer[index.x].m_OccupiedBuffer[index.y] = true;
            m_ComponentArrayBuffer[index.x].m_EntityBuffer[index.y] = entity;

            m_ComponentHashMap.Add(m_ComponentArrayBuffer[index.x].TypeInfo.GetHashCode(), index.y);

            CoreSystem.Logger.Log(Channel.Component,
                $"Component {TypeHelper.TypeOf<TComponent>.Name} set at entity({entity.Name}), index {index}");
            return data;
        }
        public void RemoveComponent<TComponent>(EntityData<IEntityData> entity)
            where TComponent : unmanaged, IEntityComponent
        {
            int2 index = GetIndex<TComponent>(entity);
#if DEBUG_MODE
            if (!m_ComponentArrayBuffer[index.x].IsCreated)
            {
                CoreSystem.Logger.LogError(Channel.Component,
                    $"Component buffer error. " +
                    $"Didn\'t collected this component({TypeHelper.TypeOf<TComponent>.Name}) infomation at initializing stage.");

                return;
            }
#endif
            DisposedComponent dispose = DisposedComponent.Construct(index, entity);

            //if (CoreSystem.BlockCreateInstance)
            {
                dispose.Dispose();
            }
            //else
            //{
            //    m_DisposedComponents.Enqueue(dispose);
            //    CoreSystem.Logger.Log(Channel.Component,
            //        $"{TypeHelper.TypeOf<TComponent>.Name} component at {entity.Name} remove queued.");
            //}
            $"entity({entity.RawName}) removed component ({TypeHelper.TypeOf<TComponent>.Name})".ToLog();
        }
        public void RemoveComponent(EntityData<IEntityData> entity, Type componentType)
        {
            int2 index = GetIndex(componentType, entity);
#if DEBUG_MODE
            if (!m_ComponentArrayBuffer[index.x].IsCreated)
            {
                CoreSystem.Logger.LogError(Channel.Component,
                    $"Component buffer error. " +
                    $"Didn\'t collected this component({TypeHelper.ToString(componentType)}) infomation at initializing stage.");

                return;
            }
#endif
            DisposedComponent dispose = DisposedComponent.Construct(index, entity);

            //if (CoreSystem.BlockCreateInstance)
            {
                dispose.Dispose();
            }
            //else
            //{
            //    m_DisposedComponents.Enqueue(dispose);
            //    CoreSystem.Logger.Log(Channel.Component,
            //        $"{TypeHelper.ToString(componentType)} component at {entity.RawName} remove queued.");
            //}

            $"entity({entity.RawName}) removed component ({componentType.Name})".ToLog();
        }
        /// <summary>
        /// TODO: Reflection 이 일어나서 SharedStatic 으로 interface 해싱 후 받아오는 게 좋아보임.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="interfaceType"></param>
        public void RemoveComponent(ObjectBase obj, Type interfaceType)
        {
            //const string c_Parent = "Parent";

            //PropertyInfo property = interfaceType
            //    .GetProperty(c_Parent, TypeHelper.TypeOf<EntityData<IEntityData>>.Type);

            EntityData<IEntityData> entity = ((INotifyComponent)obj).Parent;
            RemoveComponent(entity, interfaceType.GetGenericArguments().First());
        }
        public bool HasComponent<TComponent>(EntityData<IEntityData> entity) 
            where TComponent : unmanaged, IEntityComponent
        {
            int2 index = GetIndex<TComponent>(entity);
#if DEBUG_MODE
            if (!m_ComponentArrayBuffer[index.x].IsCreated)
            {
                CoreSystem.Logger.LogError(Channel.Component,
                    $"Component buffer error. " +
                    $"Didn\'t collected this component({TypeHelper.TypeOf<TComponent>.Name}) infomation at initializing stage.");

                return false;
            }
#endif
            if (!m_ComponentArrayBuffer[index.x].Find(entity, ref index.y))
            {
                return false;
            }

            if (((TComponent*)m_ComponentArrayBuffer[index.x].m_ComponentBuffer)[index.y] is IValidation validation &&
                !validation.IsValid())
            {
                return false;
            }

            return true;
        }
        public bool HasComponent(EntityData<IEntityData> entity, Type componentType)
        {
            int2 index = GetIndex(componentType, entity);
#if DEBUG_MODE
            if (!m_ComponentArrayBuffer[index.x].IsCreated)
            {
                CoreSystem.Logger.LogError(Channel.Component,
                    $"Component buffer error. " +
                    $"Didn\'t collected this component({componentType.Name}) infomation at initializing stage.");

                return false;
            }
#endif
            if (!m_ComponentArrayBuffer[index.x].Find(entity, ref index.y))
            {
                return false;
            }

            return true;
        }
        public ref TComponent GetComponent<TComponent>(EntityData<IEntityData> entity) 
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            s_GetComponentMarker.Begin();
#endif
            int2 index = GetIndex<TComponent>(entity);
#if DEBUG_MODE
            if (!m_ComponentArrayBuffer[index.x].IsCreated)
            {
                CoreSystem.Logger.LogError(Channel.Component,
                    $"Component buffer error. " +
                    $"Didn\'t collected this component({TypeHelper.TypeOf<TComponent>.Name}) infomation at initializing stage.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }

            if (!m_ComponentArrayBuffer[index.x].Find(entity, ref index.y))
            {
                CoreSystem.Logger.LogError(Channel.Component,
                    $"Entity({entity.Name}) doesn\'t have component({TypeHelper.TypeOf<TComponent>.Name})");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
#endif
            IJobParallelForEntitiesExtensions.CompleteAllJobs();
#if DEBUG_MODE
            s_GetComponentMarker.End();
#endif
            return ref ((TComponent*)m_ComponentArrayBuffer[index.x].m_ComponentBuffer)[index.y];
        }
        public TComponent GetComponentReadOnly<TComponent>(EntityData<IEntityData> entity)
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            s_GetComponentReadOnlyMarker.Begin();
#endif
            int2 index = GetIndex<TComponent>(entity);
#if DEBUG_MODE
            if (!m_ComponentArrayBuffer[index.x].IsCreated)
            {
                CoreSystem.Logger.LogError(Channel.Component,
                    $"Component buffer error. " +
                    $"Didn\'t collected this component({TypeHelper.TypeOf<TComponent>.Name}) infomation at initializing stage.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }

            if (!m_ComponentArrayBuffer[index.x].Find(entity, ref index.y))
            {
                CoreSystem.Logger.LogError(Channel.Component,
                    $"Entity({entity.Name}) doesn\'t have component({TypeHelper.TypeOf<TComponent>.Name})");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
#endif
            TComponent boxed = ((TComponent*)m_ComponentArrayBuffer[index.x].m_ComponentBuffer)[index.y];
#if DEBUG_MODE
            s_GetComponentReadOnlyMarker.End();
#endif
            return boxed;
        }
        public TComponent* GetComponentPointer<TComponent>(EntityData<IEntityData> entity) 
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            s_GetComponentPointerMarker.Begin();
#endif
            int2 index = GetIndex<TComponent>(entity);
#if DEBUG_MODE
            if (!m_ComponentArrayBuffer[index.x].IsCreated)
            {
                CoreSystem.Logger.LogError(Channel.Component,
                    $"Component buffer error. " +
                    $"Didn\'t collected this component({TypeHelper.TypeOf<TComponent>.Name}) infomation at initializing stage.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }

            if (!m_ComponentArrayBuffer[index.x].Find(entity, ref index.y))
            {
                CoreSystem.Logger.LogError(Channel.Component,
                    $"Entity({entity.Name}) doesn\'t have component({TypeHelper.TypeOf<TComponent>.Name})");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }

            s_GetComponentPointerMarker.End();
#endif
            return ((TComponent*)m_ComponentArrayBuffer[index.x].m_ComponentBuffer) + index.y;
        }

        #endregion

        [Obsolete("Use IJobParallelForEntities")]
        public QueryBuilder<TComponent> CreateQueryBuilder<TComponent>() where TComponent : unmanaged, IEntityComponent
        {
            int componentIdx = math.abs(TypeHelper.TypeOf<TComponent>.Type.GetHashCode()) % m_ComponentArrayBuffer.Length;
            
            if (!PoolContainer<QueryBuilder<TComponent>>.Initialized)
            {
                PoolContainer<QueryBuilder<TComponent>>.Initialize(QueryBuilder<TComponent>.QueryFactory, 32);
            }

            QueryBuilder<TComponent> builder = PoolContainer<QueryBuilder<TComponent>>.Dequeue();
            builder.Entities = m_ComponentArrayBuffer[componentIdx].m_EntityBuffer;
            builder.Components = (TComponent*)m_ComponentArrayBuffer[componentIdx].m_ComponentBuffer;
            builder.Length = m_ComponentArrayBuffer[componentIdx].Length;

            return builder;
        }

        [Obsolete("In development")]
        public void ECB(EntityComponentBuffer ecb)
        {
            ref UntypedUnsafeHashMap hashMap = 
                ref UnsafeUtility.As<UnsafeMultiHashMap<int, int>, UntypedUnsafeHashMap>(ref m_ComponentHashMap);
            //UnsafeUtility.

            unsafe
            {
                //m_ComponentHashMap.ke

                //if (m_ComponentHashMap.TryGetFirstValue(m_TypeIndex, out int i, out var iterator))
                //{
                //    do
                //    {
                //        target->HasElementAt(i, out bool has);
                //        if (!has) continue;

                //        target->ElementAt(i, out IntPtr componentPtr, out EntityData<IEntityData> entity);


                //    } while (cache.TryGetNextValue(out i, ref iterator));
                //}
            }
        }

        #region Utils

        /// <summary>
        /// Wrapper struct (아무 ValueType 맴버도 갖지 않은 구조체) 는 C# CLS 에서 무조건 1 byte 를 갖습니다. 
        /// 해당 컴포넌트 타입이 버퍼에 올라갈 필요가 있는지를 확인하여 메모리 낭비를 줄입니다.
        /// </summary>
        /// <remarks>
        /// https://stackoverflow.com/a/27851610
        /// </remarks>
        /// <param name="t"></param>
        /// <returns></returns>
        internal static bool IsZeroSizeStruct(Type t)
        {
            return t.IsValueType && !t.IsPrimitive &&
                t.GetFields((BindingFlags)0x34).All(fi => IsZeroSizeStruct(fi.FieldType));
        }
        internal static bool CollectTypes<T>(Type t)
        {
            if (t.IsAbstract || t.IsInterface) return false;

            if (TypeHelper.TypeOf<T>.Type.IsAssignableFrom(t)) return true;

            return false;
        }
        internal static int AlignOf(Type t)
        {
            Type temp = typeof(AlignOfHelper<>).MakeGenericType(t);

            return UnsafeUtility.SizeOf(temp) - UnsafeUtility.SizeOf(t);
        }

        internal static bool IsComponentType(Type t)
        {
            if (!TypeHelper.TypeOf<IEntityComponent>.Type.IsAssignableFrom(t))
            {
                return false;
            }

            return true;
        }

        #endregion

        #region Inner Classes

        public struct Constants
        {
            public static SharedStatic<EntityComponentConstrains> Value = SharedStatic<EntityComponentConstrains>.GetOrCreate<EntityComponentSystem, Constants>();

            public static PresentationSystemID<EntityComponentSystem> SystemID => Value.Data.SystemID;
        }
        
        /// <summary>
        /// Frame 단위로 Presentation 이 진행되기 떄문에, 해당 프레임에서 제거된 프레임일지어도
        /// 같은 프레임내에서 접근하는 Data Access Call 을 허용하기 위해 다음 프레임에 제거되도록 합니다.
        /// </summary>
        /// <remarks>
        /// <seealso cref="m_DisposedComponents"/> queue 에 저장되어 다음 Player Loop 의 
        /// <seealso cref="PresentationManager.PreUpdate"/> 에서 Dipose 됩니다.
        /// </remarks>
        private struct DisposedComponent : IDisposable
        {
            private static ComponentBuffer* s_Buffer;
            /// <summary>
            /// <see cref="EntityComponentSystem.m_ComponentHashMap"/>
            /// </summary>
            private static UntypedUnsafeHashMap* s_HashMap;

            private int2 index;
            private EntityData<IEntityData> entity;

            public static void Initialize(ComponentBuffer* buffer, UntypedUnsafeHashMap* hashMap)
            {
                s_Buffer = buffer;
                s_HashMap = hashMap;
            }
            public static DisposedComponent Construct(int2 index, EntityData<IEntityData> entity)
            {
                return new DisposedComponent()
                {
                    index = index,
                    entity = entity
                };
            }

            public void Dispose()
            {
                if (!s_Buffer[index.x].Find(entity, ref index.y))
                {
                    $"couldn\'t find component({s_Buffer[index.x].TypeInfo.Type.Name}) target in entity({entity.RawName}) : index{index}".ToLogError();
                    return;
                }

                ref UnsafeMultiHashMap<int, int> hashMap
                    = ref UnsafeUtility.As<UntypedUnsafeHashMap, UnsafeMultiHashMap<int, int>>(ref *s_HashMap);
                
                hashMap.Remove(s_Buffer[index.x].TypeInfo.GetHashCode(), index.y);

                s_Buffer[index.x].m_OccupiedBuffer[index.y] = false;

                void* buffer = s_Buffer[index.x].m_ComponentBuffer;

                IntPtr p = s_Buffer[index.x].ElementAt(index.y);

                object obj = Marshal.PtrToStructure(p, s_Buffer[index.x].TypeInfo.Type);

                // 해당 컴포넌트가 IDisposable 인터페이스를 상속받으면 해당 인터페이스를 실행
                if (obj is IDisposable disposable)
                {
                    disposable.Dispose();

                    CoreSystem.Logger.Log(Channel.Component,
                        $"{s_Buffer[index.x].TypeInfo.Type.Name} component at {entity.RawName} disposed.");
                }

                CoreSystem.Logger.Log(Channel.Component,
                    $"{s_Buffer[index.x].TypeInfo.Type.Name} component at {entity.RawName} removed");
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AlignOfHelper<T> where T : struct
        {
            public byte dummy;
            public T data;
        }

        #endregion
    }

    public delegate void EntityComponentDelegate<TEntity, TComponent>(in TEntity entity, in TComponent component) where TComponent : unmanaged, IEntityComponent;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
    internal unsafe sealed class ComponentBufferAtomicSafety : IDisposable
    {
        private AtomicSafetyHandle m_SafetyHandle;
        private DisposeSentinel m_DisposeSentinel;
        private TypeInfo m_TypeInfo;

        private bool m_Disposed;

        public AtomicSafetyHandle SafetyHandle => m_SafetyHandle;
        public bool Disposed => m_Disposed;

        private ComponentBufferAtomicSafety()
        {
            DisposeSentinel.Create(out m_SafetyHandle, out m_DisposeSentinel, 1, Allocator.Persistent);
        }

        public static ComponentBufferAtomicSafety Construct(TypeInfo typeInfo)
        {
            ComponentBufferAtomicSafety temp = new ComponentBufferAtomicSafety();
            temp.m_TypeInfo = typeInfo;
            return temp;
        }

        public void CheckExistsAndThrow()
        {
            if (m_Disposed)
            {
                throw new Exception();
            }

            AtomicSafetyHandle.CheckExistsAndThrow(m_SafetyHandle);
        }
        public void Dispose()
        {
            CheckExistsAndThrow();

            CoreSystem.Logger.Log(Channel.Component,
                $"Safely disposed component buffer of {TypeHelper.ToString(m_TypeInfo.Type)}");

            DisposeSentinel.Dispose(ref m_SafetyHandle, ref m_DisposeSentinel);
            m_Disposed = true;
        }
    }
#endif

    internal unsafe struct ComponentBuffer : IDisposable
    {
        public const int c_InitialCount = 512;

        private TypeInfo m_ComponentTypeInfo;

        private int m_Length;
        private int m_Increased;

        [NativeDisableUnsafePtrRestriction] public bool* m_OccupiedBuffer;
        [NativeDisableUnsafePtrRestriction] public EntityData<IEntityData>* m_EntityBuffer;
        [NativeDisableUnsafePtrRestriction] public void* m_ComponentBuffer;

        public TypeInfo TypeInfo => m_ComponentTypeInfo;
        public bool IsCreated => m_ComponentBuffer != null;
        public int Length => m_Length;

        public void Initialize(in TypeInfo typeInfo, in int size, in int align)
        {
            long
                occSize = UnsafeUtility.SizeOf<bool>() * c_InitialCount,
                idxSize = UnsafeUtility.SizeOf<EntityData<IEntityData>>() * c_InitialCount,
                bufferSize = size * c_InitialCount;
            void*
                occBuffer = UnsafeUtility.Malloc(occSize, UnsafeUtility.AlignOf<bool>(), Allocator.Persistent),
                idxBuffer = UnsafeUtility.Malloc(idxSize, UnsafeUtility.AlignOf<EntityData<IEntityData>>(), Allocator.Persistent),
                buffer = UnsafeUtility.Malloc(bufferSize, align, Allocator.Persistent);

            UnsafeUtility.MemClear(occBuffer, occSize);
            // TODO: 할당되지도 않았는데 엔티티와 데이터 버퍼는 초기화 할 필요가 있나?
            UnsafeUtility.MemClear(idxBuffer, idxSize);
            UnsafeUtility.MemClear(buffer, bufferSize);

            this.m_ComponentTypeInfo = typeInfo;
            this.m_OccupiedBuffer = (bool*)occBuffer;
            this.m_EntityBuffer = (EntityData<IEntityData>*)idxBuffer;
            this.m_ComponentBuffer = buffer;
            this.m_Length = c_InitialCount;
            m_Increased = 1;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CLSTypedDictionary<ComponentBufferAtomicSafety>.SetValue(m_ComponentTypeInfo.Type,
                ComponentBufferAtomicSafety.Construct(typeInfo));
#endif
        }
        public void Increment<TComponent>() where TComponent : unmanaged, IEntityComponent
        {
            if (!IsCreated) throw new Exception();

            int count = c_InitialCount * (m_Increased + 1);
            long
                occSize = UnsafeUtility.SizeOf<bool>() * count,
                idxSize = UnsafeUtility.SizeOf<EntityData<IEntityData>>() * count,
                bufferSize = UnsafeUtility.SizeOf<TComponent>() * count;
            void*
                occBuffer = UnsafeUtility.Malloc(occSize, UnsafeUtility.AlignOf<bool>(), Allocator.Persistent),
                idxBuffer = UnsafeUtility.Malloc(idxSize, UnsafeUtility.AlignOf<EntityData<IEntityData>>(), Allocator.Persistent),
                buffer = UnsafeUtility.Malloc(bufferSize, UnsafeUtility.AlignOf<TComponent>(), Allocator.Persistent);

            UnsafeUtility.MemClear(occBuffer, occSize);
            UnsafeUtility.MemClear(idxBuffer, idxSize);
            UnsafeUtility.MemClear(buffer, bufferSize);

            UnsafeUtility.MemCpy(occBuffer, m_OccupiedBuffer, UnsafeUtility.SizeOf<bool>() * m_Length);
            UnsafeUtility.MemCpy(idxBuffer, m_EntityBuffer, UnsafeUtility.SizeOf<EntityData<IEntityData>>() * m_Length);
            UnsafeUtility.MemCpy(buffer, m_ComponentBuffer, UnsafeUtility.SizeOf<TComponent>() * m_Length);

            UnsafeUtility.Free(this.m_OccupiedBuffer, Allocator.Persistent);
            UnsafeUtility.Free(this.m_EntityBuffer, Allocator.Persistent);
            UnsafeUtility.Free(this.m_ComponentBuffer, Allocator.Persistent);

            this.m_OccupiedBuffer = (bool*)occBuffer;
            this.m_EntityBuffer = (EntityData<IEntityData>*)idxBuffer;
            this.m_ComponentBuffer = buffer;

            m_Increased += 1;
            m_Length = c_InitialCount * m_Increased;

            CoreSystem.Logger.Log(Channel.Component, $"increased {TypeHelper.TypeOf<TComponent>.Name} {m_Length} :: {m_Increased}");
        }

        public bool Find(EntityData<IEntityData> entity, ref int entityIndex)
        {
            if (m_Length == 0)
            {
                return false;
            }

            for (int i = 0; i < m_Increased; i++)
            {
                int idx = (c_InitialCount * i) + entityIndex;

                if (!m_OccupiedBuffer[idx]) continue;
                else if (this.m_EntityBuffer[idx].Idx.Equals(entity.Idx))
                {
                    entityIndex = idx;
                    return true;
                }
            }

            return false;
        }
        public bool FindEmpty(EntityData<IEntityData> entity, ref int entityIndex)
        {
            if (m_Length == 0)
            {
                return false;
            }

            for (int i = 0; i < m_Increased; i++)
            {
                int idx = (c_InitialCount * i) + entityIndex;

                if (m_OccupiedBuffer[idx]) continue;

                entityIndex = idx;
                return true;
            }

            return false;
        }

        public void HasElementAt(int i, out bool result)
        {
            result = m_OccupiedBuffer[i];
        }
        public void ElementAt<TComponent>(int i, out EntityData<IEntityData> entity, out TComponent component)
            where TComponent : unmanaged, IEntityComponent
        {
            entity = m_EntityBuffer[i];
            component = ((TComponent*)m_ComponentBuffer)[i];
        }
        public void ElementAt<TComponent>(int i, out EntityData<IEntityData> entity, out TComponent* component)
            where TComponent : unmanaged, IEntityComponent
        {
            entity = m_EntityBuffer[i];
            component = ((TComponent*)m_ComponentBuffer) + i;
        }

        public IntPtr ElementAt(int i)
        {
            IntPtr p = (IntPtr)m_ComponentBuffer;
            // Align 은 필요없음.
            return IntPtr.Add(p, TypeInfo.Size * i);
        }
        public void ElementAt(int i, out IntPtr ptr, out EntityData<IEntityData> entity)
        {
            ptr = ElementAt(i);
            entity = m_EntityBuffer[i];
        }

        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            ComponentBufferAtomicSafety safety
                = CLSTypedDictionary<ComponentBufferAtomicSafety>.GetValue(m_ComponentTypeInfo.Type);

            safety.CheckExistsAndThrow();
#endif

            UnsafeUtility.Free(m_OccupiedBuffer, Allocator.Persistent);
            UnsafeUtility.Free(m_EntityBuffer, Allocator.Persistent);
            UnsafeUtility.Free(m_ComponentBuffer, Allocator.Persistent);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            safety.Dispose();
#endif
        }
    }

    /// <summary>
    /// Runtime 중 기본 <see cref="System.Type"/> 의 정보를 저장하고, 해당 타입의 binary 크기, alignment를 저장합니다.
    /// </summary>
    public readonly struct TypeInfo
    {
        private readonly RuntimeTypeHandle m_TypeHandle;
        private readonly int m_TypeIndex;
        private readonly int m_Size;
        private readonly int m_Align;

        private readonly int m_HashCode;

        public Type Type => Type.GetTypeFromHandle(m_TypeHandle);
        public int Index => m_TypeIndex;
        public int Size => m_Size;
        public int Align => m_Align;

        private TypeInfo(Type type, int index, int size, int align, int hashCode)
        {
            m_TypeHandle = type.TypeHandle;
            m_TypeIndex = index;
            m_Size = size;
            m_Align = align;

            unchecked
            {
                // https://stackoverflow.com/questions/102742/why-is-397-used-for-resharper-gethashcode-override
                m_HashCode = m_TypeIndex * 397 ^ hashCode;
            }
        }

        public static TypeInfo Construct(Type type, int index, int size, int align, int hashCode)
        {
            return new TypeInfo(type, index, size, align, hashCode);
        }
        public static TypeInfo Construct(Type type, int index, int hashCode)
        {
            if (!UnsafeUtility.IsUnmanaged(type))
            {
                CoreSystem.Logger.LogError(Channel.Component,
                    $"Could not resovle type of {TypeHelper.ToString(type)} is not ValueType.");

                return new TypeInfo(type, index, 0, 0, hashCode);
            }

            return new TypeInfo(type, index, UnsafeUtility.SizeOf(type), EntityComponentSystem.AlignOf(type), hashCode);
        }

        public override int GetHashCode() => m_HashCode;
    }
    public struct ComponentType
    {
        public static SharedStatic<TypeInfo> GetValue(Type componentType)
        {
            return SharedStatic<TypeInfo>.GetOrCreate(
                TypeHelper.TypeOf<EntityComponentSystem>.Type,
                componentType, (uint)UnsafeUtility.AlignOf<TypeInfo>());
        }
    }
    public struct ComponentType<TComponent>
    {
        private static readonly SharedStatic<TypeInfo> Value
            = SharedStatic<TypeInfo>.GetOrCreate<EntityComponentSystem, TComponent>((uint)UnsafeUtility.AlignOf<TypeInfo>());

        public static Type Type => Value.Data.Type;
        public static TypeInfo TypeInfo => Value.Data;
        public static int Index => Value.Data.Index;
        public static ComponentTypeQuery Query => ComponentTypeQuery.Create(TypeInfo);
    }

    
    

    
    unsafe internal struct ComponentChunk
    {
        public void* m_Pointer;
        public int m_Count;

        public ComponentChunk(void* p, int count)
        {
            m_Pointer = p;
            m_Count = count;
        }
        public ComponentChunk(IntPtr p, int count)
        {
            m_Pointer = p.ToPointer();
            m_Count = count;
        }
    }
}
