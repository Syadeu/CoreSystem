#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Database;
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
    unsafe internal sealed class EntityComponentSystem : PresentationSystemEntity<EntityComponentSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private NativeArray<ComponentBuffer> m_ComponentArrayBuffer;
        private MethodInfo m_RemoveComponentMethod;

        private Unity.Mathematics.Random m_Random;

#if DEBUG_MODE
        private static Unity.Profiling.ProfilerMarker
            s_GetComponentMarker = new Unity.Profiling.ProfilerMarker("get_GetComponent"),
            s_GetComponentReadOnlyMarker = new Unity.Profiling.ProfilerMarker("get_GetComponentReadOnly"),
            s_GetComponentPointerMarker = new Unity.Profiling.ProfilerMarker("get_GetComponentPointer");
#endif

        private EntitySystem m_EntitySystem;

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            m_Random = new Unity.Mathematics.Random();
            m_Random.InitState();

            RequestSystem<DefaultPresentationGroup, EntitySystem>(Bind);

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
                // 왜인지는 모르겠지만 Type.GetHash() 의 정보가 런타임 중 간혹 유효하지 않은 값 (0) 을 뱉어서 미리 파싱합니다.
                int idx = math.abs(CreateHashCode()) % tempBuffer.Length;
                ComponentType.GetValue(types[i]).Data = idx;

                // 새로운 버퍼를 생성하고, heap 에 메모리를 할당합니다.
                ComponentBuffer temp = new ComponentBuffer();
                temp.Initialize(idx, UnsafeUtility.SizeOf(types[i]), AlignOf(types[i]));

                tempBuffer[idx] = temp;
            }

            m_ComponentArrayBuffer = new NativeArray<ComponentBuffer>(tempBuffer, Allocator.Persistent);

            ConstructSharedStatics();

            PresentationManager.Instance.PreUpdate += Presentation_PreUpdate;

            return base.OnInitialize();
        }
        private void ConstructSharedStatics()
        {
            Constants.Value.Data.SystemID = SystemID;
            
        }

        private void Presentation_PreUpdate()
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

        protected override PresentationResult OnInitializeAsync()
        {
            m_RemoveComponentMethod = TypeHelper.TypeOf<EntityComponentSystem>.Type.GetMethod(nameof(RemoveComponent),
                new Type[] { TypeHelper.TypeOf<EntityData<IEntityData>>.Type });

            return base.OnInitializeAsync();
        }
        
        public override void OnDispose()
        {
            PresentationManager.Instance.PreUpdate -= Presentation_PreUpdate;

            int count = m_DisposedComponents.Count;
            for (int i = 0; i < count; i++)
            {
                m_DisposedComponents.Dequeue().Dispose();
            }

            for (int i = 0; i < m_ComponentArrayBuffer.Length; i++)
            {
                if (!m_ComponentArrayBuffer[i].IsCreated) continue;

                m_ComponentArrayBuffer[i].Dispose();
            }
            m_ComponentArrayBuffer.Dispose();

            m_EntitySystem = null;
        }

        #region Binds

        private void Bind(EntitySystem other)
        {
            m_EntitySystem = other;
        }

        #endregion

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
            int idx = ComponentType.GetValue(t).Data;
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

        private readonly Queue<IDisposable> m_DisposedComponents = new Queue<IDisposable>();
        private struct DisposedComponent<TComponent> : IDisposable
            where TComponent : unmanaged, IEntityComponent
        {
            private static ComponentBuffer* s_Buffer;

            public static void Initialize(ComponentBuffer* componentBuffer)
            {
                s_Buffer = componentBuffer;
            }

            public EntityData<IEntityData> entity;
            public int2 index;

            public void Dispose()
            {
                if (!s_Buffer[index.x].Find(entity, ref index.y))
                {
                    return;
                }

                s_Buffer[index.x].m_OccupiedBuffer[index.y] = false;

                TComponent* buffer = (TComponent*)s_Buffer[index.x].m_ComponentBuffer;
                if (buffer[index.y] is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                CoreSystem.Logger.Log(Channel.Component,
                    $"{TypeHelper.TypeOf<TComponent>.Name} component at {entity.RawName} removed");
            }
        }

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
            DisposedComponent<TComponent>.Initialize((ComponentBuffer*)m_ComponentArrayBuffer.GetUnsafePtr());

            DisposedComponent<TComponent> dispose = new DisposedComponent<TComponent>()
            {
                index = index,
                entity = entity
            };

            if (CoreSystem.BlockCreateInstance)
            {
                dispose.Dispose();
            }
            else
            {
                m_DisposedComponents.Enqueue(dispose);
                CoreSystem.Logger.Log(Channel.Component,
                    $"{TypeHelper.TypeOf<TComponent>.Name} component at {entity.Name} remove queued.");
            }
        }
        public void RemoveComponent(EntityData<IEntityData> entity, Type componentType)
        {
            MethodInfo method = m_RemoveComponentMethod.MakeGenericMethod(componentType);
            method.Invoke(this, new object[] { entity });
        }
        public void RemoveComponent(ObjectBase obj, Type interfaceType)
        {
            const string c_Parent = "Parent";

            PropertyInfo property = interfaceType
                .GetProperty(c_Parent, TypeHelper.TypeOf<EntityData<IEntityData>>.Type);

            EntityData<IEntityData> entity = (EntityData<IEntityData>)property.GetValue(obj);
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

        #region Utils

        private static bool CollectTypes<T>(Type t)
        {
            if (t.IsAbstract || t.IsInterface) return false;

            if (TypeHelper.TypeOf<T>.Type.IsAssignableFrom(t)) return true;

            return false;
        }
        private static int AlignOf(Type t)
        {
            Type temp = typeof(AlignOfHelper<>).MakeGenericType(t);

            return UnsafeUtility.SizeOf(temp) - UnsafeUtility.SizeOf(t);
        }

        #endregion

        #region Inner Classes

        public struct Constants
        {
            public static SharedStatic<EntityComponentConstrains> Value = SharedStatic<EntityComponentConstrains>.GetOrCreate<EntityComponentSystem, Constants>();

            public static PresentationSystemID<EntityComponentSystem> SystemID => Value.Data.SystemID;
        }
        public struct ComponentType
        {
            public static SharedStatic<int> GetValue(Type componentType)
            {
                return SharedStatic<int>.GetOrCreate(
                    TypeHelper.TypeOf<EntityComponentSystem>.Type,
                    componentType, (uint)UnsafeUtility.AlignOf<int>());
            }
        }
        public struct ComponentType<TComponent>
        {
            public static readonly SharedStatic<int> Value
                = SharedStatic<int>.GetOrCreate<EntityComponentSystem, TComponent>((uint)UnsafeUtility.AlignOf<int>());

            public static int Index => Value.Data;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AlignOfHelper<T> where T : struct
        {
            public byte dummy;
            public T data;
        }
        unsafe internal struct EntityComponents
        {
            private readonly ComponentBuffer* m_Buffer;
            private readonly uint m_Length;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            //private AtomicSafetyHandle m_SafetyHandle;
#endif

            public EntityComponents(ComponentBuffer* buffer, uint length)
            {
                m_Buffer = buffer;
                m_Length = length;

                //DisposeSentinel.
            }

            public ComponentBuffer* GetBuffer(int index)
            {
                if (index < 0 || index > m_Length)
                {
                    throw new IndexOutOfRangeException();
                }

                return m_Buffer + index;
            }
        }
        unsafe internal struct ComponentBuffer : IDisposable
        {
            public const int c_InitialCount = 512;

            private int m_Index;

            private int m_Length;
            private int m_Increased;

            [NativeDisableUnsafePtrRestriction] public bool* m_OccupiedBuffer;
            [NativeDisableUnsafePtrRestriction] public EntityData<IEntityData>* m_EntityBuffer;
            [NativeDisableUnsafePtrRestriction] public void* m_ComponentBuffer;

            public bool IsCreated => m_ComponentBuffer != null;
            public int Length => m_Length;

            public void Initialize(int index, int size, int align)
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
                UnsafeUtility.MemClear(idxBuffer, idxSize);
                UnsafeUtility.MemClear(buffer, bufferSize);

                this.m_Index = index;
                this.m_OccupiedBuffer = (bool*)occBuffer;
                this.m_EntityBuffer = (EntityData<IEntityData>*)idxBuffer;
                this.m_ComponentBuffer = buffer;
                this.m_Length = c_InitialCount;
                m_Increased = 1;
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

            public void Dispose()
            {
                if (!IsCreated)
                {
                    "??".ToLogError();
                    return;
                }

                UnsafeUtility.Free(m_OccupiedBuffer, Allocator.Persistent);
                UnsafeUtility.Free(m_EntityBuffer, Allocator.Persistent);
                UnsafeUtility.Free(m_ComponentBuffer, Allocator.Persistent);
            }
        }

        #endregion
    }

    public delegate void EntityComponentDelegate<TEntity, TComponent>(in TEntity entity, in TComponent component) where TComponent : unmanaged, IEntityComponent;
}
