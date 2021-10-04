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

        private long m_EntityLength;
        private NativeArray<EntityComponentBuffer> m_ComponentBuffer;
        private MethodInfo m_RemoveComponentMethod;

        private readonly Dictionary<Type, int> m_ComponentIndices = new Dictionary<Type, int>();

        private EntitySystem m_EntitySystem;

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            Type[] entityTypes = TypeHelper.GetTypes(CollectTypes<IEntityData>);
            m_EntityLength = entityTypes.LongLength;

            Type[] types = TypeHelper.GetTypes(CollectTypes<IEntityComponent>);

            int length;
            if (types.Length < 512)
            {
                length = 1024;
            }
            else length = types.Length * 2;

            EntityComponentBuffer[] tempBuffer = new EntityComponentBuffer[length];
            for (int i = 0; i < types.Length; i++)
            {
                if (!m_ComponentIndices.TryGetValue(types[i], out int idx))
                {
                    idx = math.abs(types[i].GetHashCode()) % tempBuffer.Length;
                    m_ComponentIndices.Add(types[i], idx);
                }

                var temp = new EntityComponentBuffer();
                temp.Initialize(idx, UnsafeUtility.SizeOf(types[i]), AlignOf(types[i]));

                tempBuffer[idx] = temp;
            }

            m_ComponentBuffer = new NativeArray<EntityComponentBuffer>(tempBuffer, Allocator.Persistent);

            RequestSystem<EntitySystem>(Bind);

            SharedStatic<EntityComponentConstrains> constrains = SharedStatic<EntityComponentConstrains>.GetOrCreate<EntityComponentSystem>();

            constrains.Data.SystemID = SystemID;

            return base.OnInitialize();
        }
        protected override PresentationResult OnInitializeAsync()
        {
            m_RemoveComponentMethod = TypeHelper.TypeOf<EntityComponentSystem>.Type.GetMethod(nameof(RemoveComponent),
                new Type[] { TypeHelper.TypeOf<EntityData<IEntityData>>.Type });

            return base.OnInitializeAsync();
        }
        private bool CollectTypes<T>(Type t)
        {
            if (t.IsAbstract || t.IsInterface) return false;

            if (TypeHelper.TypeOf<T>.Type.IsAssignableFrom(t)) return true;

            return false;
        }
        public override void OnDispose()
        {
            for (int i = 0; i < m_ComponentBuffer.Length; i++)
            {
                if (!m_ComponentBuffer[i].IsCreated) continue;

                m_ComponentBuffer[i].Dispose();
            }
            m_ComponentBuffer.Dispose();

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

        private int GetComponentIndex<TComponent>() => GetComponentIndex(TypeHelper.TypeOf<TComponent>.Type);
        private int GetComponentIndex(Type t)
        {
#if DEBUG_MODE
            if (!m_ComponentIndices.ContainsKey(t))
            {
                CoreSystem.Logger.LogError(Channel.Component,
                    $"Component buffer error. " +
                    $"Didn\'t collected this component({t.Name}) infomation at initializing stage.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
#endif
            return m_ComponentIndices[t];
        }
        private int GetEntityIndex(EntityData<IEntityData> entity)
        {
            int idx = math.abs(entity.GetHashCode()) % EntityComponentBuffer.c_InitialCount;
            return idx;
        }
        private int2 GetIndex(Type t, EntityData<IEntityData> entity)
        {
            int
                cIdx = GetComponentIndex(t),
                eIdx = GetEntityIndex(entity);

            return new int2(cIdx, eIdx);
        }
        private int2 GetIndex<TComponent>(EntityData<IEntityData> entity)
        {
            int
                cIdx = GetComponentIndex<TComponent>(),
                eIdx = GetEntityIndex(entity);

            return new int2(cIdx, eIdx);
        }

        #endregion

        #region Component Methods

        public void ComponentBufferSafetyCheck<TComponent>(out bool result)
        {
#if DEBUG_MODE
            if (!m_ComponentIndices.ContainsKey(TypeHelper.TypeOf<TComponent>.Type))
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
        public EntityComponentBuffer GetComponentBuffer<TComponent>()
        {
            int idx = GetComponentIndex<TComponent>();

            return UnsafeUtility.ReadArrayElement<EntityComponentBuffer>(m_ComponentBuffer.GetUnsafeReadOnlyPtr(), idx);
        }
        public EntityComponentBuffer* GetComponentBufferPointer<TComponent>()
        {
            int idx = GetComponentIndex<TComponent>();

            return ((EntityComponentBuffer*)m_ComponentBuffer.GetUnsafeReadOnlyPtr()) + idx;
        }
        public IntPtr GetComponentBufferPointerIntPtr<TComponent>()
        {
            int idx = GetComponentIndex<TComponent>();

            return (IntPtr)((EntityComponentBuffer*)m_ComponentBuffer.GetUnsafeReadOnlyPtr() + idx);
        }

        public TComponent AddComponent<TComponent>(in EntityData<IEntityData> entity, in TComponent data) where TComponent : unmanaged, IEntityComponent
        {
            CoreSystem.Logger.ThreadBlock(nameof(AddComponent), ThreadInfo.Unity);

            int2 index = GetIndex<TComponent>(entity);
#if DEBUG_MODE
            if (!m_ComponentBuffer[index.x].IsCreated)
            {
                CoreSystem.Logger.LogError(Channel.Component,
                    $"Component buffer error. " +
                    $"Didn\'t collected this component({TypeHelper.TypeOf<TComponent>.Name}) infomation at initializing stage.");

                return data;
            }
#endif
            if (!m_ComponentBuffer[index.x].Find(entity, ref index.y) &&
                !m_ComponentBuffer[index.x].FindEmpty(entity, ref index.y))
            {
                EntityComponentBuffer boxed = m_ComponentBuffer[index.x];
                boxed.Increment<TComponent>();
                m_ComponentBuffer[index.x] = boxed;

                if (!m_ComponentBuffer[index.x].FindEmpty(entity, ref index.y))
                {
                    CoreSystem.Logger.LogError(Channel.Component,
                        $"Component buffer error. " +
                        $"Component({TypeHelper.TypeOf<TComponent>.Name}) Hash has been conflected twice. Maybe need to increase default buffer size?");

                    throw new InvalidOperationException($"Component buffer error. See Error Log.");
                }
            }

            ((TComponent*)m_ComponentBuffer[index.x].m_ComponentBuffer)[index.y] = data;
            m_ComponentBuffer[index.x].m_OccupiedBuffer[index.y] = true;
            m_ComponentBuffer[index.x].m_EntityBuffer[index.y] = entity;

            CoreSystem.Logger.Log(Channel.Component,
                $"Component {TypeHelper.TypeOf<TComponent>.Name} set at entity({entity.Name}), index {index}");
            return data;
        }
        public void RemoveComponent<TComponent>(EntityData<IEntityData> entity)
            where TComponent : unmanaged, IEntityComponent
        {
            int2 index = GetIndex<TComponent>(entity);
#if DEBUG_MODE
            if (!m_ComponentBuffer[index.x].IsCreated)
            {
                CoreSystem.Logger.LogError(Channel.Component,
                    $"Component buffer error. " +
                    $"Didn\'t collected this component({TypeHelper.TypeOf<TComponent>.Name}) infomation at initializing stage.");

                return;
            }
#endif
            if (!m_ComponentBuffer[index.x].Find(entity, ref index.y))
            {
                return;
            }

            m_ComponentBuffer[index.x].m_OccupiedBuffer[index.y] = false;

            unsafe
            {
                TComponent* buffer = (TComponent*)m_ComponentBuffer[index.x].m_ComponentBuffer;
                if (buffer[index.y] is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            CoreSystem.Logger.Log(Channel.Component,
                $"{TypeHelper.TypeOf<TComponent>.Name} component at {entity.Name} removed");
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
            if (!m_ComponentBuffer[index.x].IsCreated)
            {
                CoreSystem.Logger.LogError(Channel.Component,
                    $"Component buffer error. " +
                    $"Didn\'t collected this component({TypeHelper.TypeOf<TComponent>.Name}) infomation at initializing stage.");

                return false;
            }
#endif
            if (!m_ComponentBuffer[index.x].Find(entity, ref index.y))
            {
                return false;
            }

            if (((TComponent*)m_ComponentBuffer[index.x].m_ComponentBuffer)[index.y] is IValidation validation &&
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
            if (!m_ComponentBuffer[index.x].IsCreated)
            {
                CoreSystem.Logger.LogError(Channel.Component,
                    $"Component buffer error. " +
                    $"Didn\'t collected this component({componentType.Name}) infomation at initializing stage.");

                return false;
            }
#endif
            if (!m_ComponentBuffer[index.x].Find(entity, ref index.y))
            {
                return false;
            }

            return true;
        }
        public ref TComponent GetComponent<TComponent>(EntityData<IEntityData> entity) 
            where TComponent : unmanaged, IEntityComponent
        {
            int2 index = GetIndex<TComponent>(entity);
#if DEBUG_MODE
            if (!m_ComponentBuffer[index.x].IsCreated)
            {
                CoreSystem.Logger.LogError(Channel.Component,
                    $"Component buffer error. " +
                    $"Didn\'t collected this component({TypeHelper.TypeOf<TComponent>.Name}) infomation at initializing stage.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }

            if (!m_ComponentBuffer[index.x].Find(entity, ref index.y))
            {
                CoreSystem.Logger.LogError(Channel.Component,
                    $"Entity({entity.Name}) doesn\'t have component({TypeHelper.TypeOf<TComponent>.Name})");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
#endif
            return ref ((TComponent*)m_ComponentBuffer[index.x].m_ComponentBuffer)[index.y];
        }
        public TComponent* GetComponentPointer<TComponent>(EntityData<IEntityData> entity) 
            where TComponent : unmanaged, IEntityComponent
        {
            int2 index = GetIndex<TComponent>(entity);
#if DEBUG_MODE
            if (!m_ComponentBuffer[index.x].IsCreated)
            {
                CoreSystem.Logger.LogError(Channel.Component,
                    $"Component buffer error. " +
                    $"Didn\'t collected this component({TypeHelper.TypeOf<TComponent>.Name}) infomation at initializing stage.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }

            if (!m_ComponentBuffer[index.x].Find(entity, ref index.y))
            {
                CoreSystem.Logger.LogError(Channel.Component,
                    $"Entity({entity.Name}) doesn\'t have component({TypeHelper.TypeOf<TComponent>.Name})");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
#endif
            return ((TComponent*)m_ComponentBuffer[index.x].m_ComponentBuffer) + index.y;
        }

        #endregion

        public QueryBuilder<TComponent> CreateQueryBuilder<TComponent>() where TComponent : unmanaged, IEntityComponent
        {
            int componentIdx = math.abs(TypeHelper.TypeOf<TComponent>.Type.GetHashCode()) % m_ComponentBuffer.Length;
            
            if (!PoolContainer<QueryBuilder<TComponent>>.Initialized)
            {
                PoolContainer<QueryBuilder<TComponent>>.Initialize(QueryBuilder<TComponent>.QueryFactory, 32);
            }

            QueryBuilder<TComponent> builder = PoolContainer<QueryBuilder<TComponent>>.Dequeue();
            builder.Entities = m_ComponentBuffer[componentIdx].m_EntityBuffer;
            builder.Components = (TComponent*)m_ComponentBuffer[componentIdx].m_ComponentBuffer;
            builder.Length = m_ComponentBuffer[componentIdx].Length;

            return builder;
        }

        #region Utils

        private static int AlignOf(Type t)
        {
            Type temp = typeof(AlignOfHelper<>).MakeGenericType(t);

            return UnsafeUtility.SizeOf(temp) - UnsafeUtility.SizeOf(t);
        }

        #endregion

        #region Inner Classes

        [StructLayout(LayoutKind.Sequential)]
        private struct AlignOfHelper<T> where T : struct
        {
            public byte dummy;
            public T data;
        }
        unsafe internal struct EntityComponentBuffer : IDisposable
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
