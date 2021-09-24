using AOT;
using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Entities;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
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

        private EntitySystem m_EntitySystem;

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
                if (!m_ComponentIndices.TryGetValue(types[i], out int componentIdx))
                {
                    componentIdx = math.abs(types[i].GetHashCode());
                    m_ComponentIndices.Add(types[i], componentIdx);
                }

                int idx = componentIdx % tempBuffer.Length;
                var temp = new EntityComponentBuffer()
                {
                    index = idx,

                    length = 0
                };

                long
                    occSize = UnsafeUtility.SizeOf<bool>() * EntityComponentBuffer.c_InitialCount,
                    idxSize = UnsafeUtility.SizeOf<EntityData<IEntityData>>() * EntityComponentBuffer.c_InitialCount,
                    bufferSize = UnsafeUtility.SizeOf(types[i]) * EntityComponentBuffer.c_InitialCount;
                void*
                    occBuffer = UnsafeUtility.Malloc(occSize, UnsafeUtility.AlignOf<bool>(), Allocator.Persistent),
                    idxBuffer = UnsafeUtility.Malloc(idxSize, UnsafeUtility.AlignOf<EntityData<IEntityData>>(), Allocator.Persistent),
                    buffer = UnsafeUtility.Malloc(bufferSize, AlignOf(types[i]), Allocator.Persistent);

                UnsafeUtility.MemClear(occBuffer, occSize);
                UnsafeUtility.MemClear(idxBuffer, idxSize);
                UnsafeUtility.MemClear(buffer, bufferSize);

                temp.Initialize(occBuffer, idxBuffer, buffer, EntityComponentBuffer.c_InitialCount);

                tempBuffer[idx] = temp;
            }

            m_ComponentBuffer = new NativeArray<EntityComponentBuffer>(tempBuffer, Allocator.Persistent);

            RequestSystem<EntitySystem>(Bind);

            SharedStatic<EntityComponentConstrains> constrains = SharedStatic<EntityComponentConstrains>.GetOrCreate<EntityComponentSystem>();

            constrains.Data.SystemID = SystemID;

            return base.OnInitialize();
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

        private readonly Dictionary<Type, int> m_ComponentIndices = new Dictionary<Type, int>();

        private int GetComponentIndex<TComponent>() => GetComponentIndex(TypeHelper.TypeOf<TComponent>.Type);
        private int GetComponentIndex(Type t)
        {
            if (!m_ComponentIndices.TryGetValue(t, out int componentIdx))
            {
                componentIdx = math.abs(t.GetHashCode());
                m_ComponentIndices.Add(t, componentIdx);
            }

            return componentIdx % m_ComponentBuffer.Length;
        }
        private int GetEntityIndex(int componentIdx, EntityData<IEntityData> entity)
        {
            return m_ComponentBuffer[componentIdx].length == 0 ? -1 : math.abs(entity.GetHashCode()) % m_ComponentBuffer[componentIdx].length;
        }
        private int2 GetIndex<TComponent>(EntityData<IEntityData> entity)
        {
            int
                cIdx = GetComponentIndex<TComponent>(),
                eIdx = GetEntityIndex(cIdx, entity);

            return new int2(cIdx, eIdx);
        }

        public TComponent AddComponent<TComponent>(EntityData<IEntityData> entity, TComponent data) where TComponent : unmanaged, IEntityComponent
        {
            CoreSystem.Logger.ThreadBlock(nameof(AddComponent), ThreadInfo.Unity);

            int2 index = GetIndex<TComponent>(entity);

            if (m_ComponentBuffer[index.x].length == 0)
            {
                throw new Exception();
            }

            if (m_ComponentBuffer[index.x].occupied[index.y] &&
                !m_ComponentBuffer[index.x].entity[index.y].Idx.Equals(entity.Idx) &&
                m_EntitySystem != null &&
                !m_EntitySystem.IsDestroyed(m_ComponentBuffer[index.x].entity[index.y].Idx))
            {
                //int length = m_ComponentBuffer[index.x].length * 2;
                //long
                //    occSize = UnsafeUtility.SizeOf<bool>() * length,
                //    idxSize = UnsafeUtility.SizeOf<EntityData<IEntityData>>() * length,
                //    bufferSize = UnsafeUtility.SizeOf<TComponent>() * length;
                //void*
                //    occBuffer = UnsafeUtility.Malloc(occSize, UnsafeUtility.AlignOf<bool>(), Allocator.Persistent),
                //    idxBuffer = UnsafeUtility.Malloc(occSize, UnsafeUtility.AlignOf<EntityData<IEntityData>>(), Allocator.Persistent),
                //    buffer = UnsafeUtility.Malloc(bufferSize, UnsafeUtility.AlignOf<TComponent>(), Allocator.Persistent);

                //UnsafeUtility.MemClear(occBuffer, occSize);
                //UnsafeUtility.MemClear(idxBuffer, idxSize);
                //UnsafeUtility.MemClear(buffer, bufferSize);

                //for (int i = 0; i < m_ComponentBuffer[index.x].length; i++)
                //{
                //    if (!m_ComponentBuffer[index.x].occupied[i]) continue;

                //    int newEntityIdx = math.abs(m_ComponentBuffer[index.x].entity[i].GetHashCode()) % length;

                //    if (((bool*)occBuffer)[newEntityIdx])
                //    {
                //        "... conflect again".ToLogError();
                //    }

                //    ((bool*)occBuffer)[newEntityIdx] = true;
                //    ((EntityData<IEntityData>*)idxBuffer)[newEntityIdx] = m_ComponentBuffer[index.x].entity[i];
                //    ((TComponent*)buffer)[newEntityIdx] = ((TComponent*)m_ComponentBuffer[index.x].buffer)[i];
                //}

                ////EntityComponentBuffer boxed = m_ComponentBuffer[index.x];

                ////UnsafeUtility.Free(boxed.occupied, Allocator.Persistent);
                ////UnsafeUtility.Free(boxed.entity, Allocator.Persistent);
                ////UnsafeUtility.Free(boxed.buffer, Allocator.Persistent);

                ////boxed.occupied = (bool*)occBuffer;
                ////boxed.entity = (EntityData<IEntityData>*)idxBuffer;
                ////boxed.buffer = buffer;
                ////boxed.length = length;
                ////m_ComponentBuffer[index.x] = boxed;

                //m_ComponentBuffer[index.x].Initialize(occBuffer, idxBuffer, buffer, length);

                //$"Component {TypeHelper.TypeOf<TComponent>.Name} buffer increased to {length}".ToLog();

                //return AddComponent(entity, data);
                throw new Exception();
            }

            ((TComponent*)m_ComponentBuffer[index.x].buffer)[index.y] = data;
            m_ComponentBuffer[index.x].occupied[index.y] = true;
            m_ComponentBuffer[index.x].entity[index.y] = entity;

            $"Component {TypeHelper.TypeOf<TComponent>.Name} set at entity({entity.Name})".ToLog();

            return data;
        }
        public bool HasComponent<TComponent>(EntityData<IEntityData> entity) 
            where TComponent : unmanaged, IEntityComponent
        {
            int2 index = GetIndex<TComponent>(entity);

            if (m_ComponentBuffer[index.x].length == 0) return false;

            if (!m_ComponentBuffer[index.x].occupied[index.y])
            {
                return false;
            }

            if (!m_ComponentBuffer[index.x].entity[index.y].Idx.Equals(entity.Idx))
            {
                if (!m_EntitySystem.IsDestroyed(m_ComponentBuffer[index.x].entity[index.y].Idx))
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"Component({TypeHelper.TypeOf<TComponent>.Name}) validation error. Maybe conflect.");
                }

                return false;
            }

            return true;
        }
        public TComponent GetComponent<TComponent>(EntityData<IEntityData> entity) where TComponent : unmanaged, IEntityComponent
        {
            int2 index = GetIndex<TComponent>(entity);

            if (!m_ComponentBuffer[index.x].occupied[index.y])
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({entity.Name}) doesn\'t have component({TypeHelper.TypeOf<TComponent>.Name})");
                return default(TComponent);
            }

            if (!m_ComponentBuffer[index.x].entity[index.y].Idx.Equals(entity.Idx) &&
                !m_EntitySystem.IsDestroyed(m_ComponentBuffer[index.x].entity[index.y].Idx))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Component({TypeHelper.TypeOf<TComponent>.Name}) validation error. Maybe conflect.");
                return default(TComponent);
            }

            return ((TComponent*)m_ComponentBuffer[index.x].buffer)[index.y];
        }

        public QueryBuilder<TComponent> CreateQueryBuilder<TComponent>() where TComponent : unmanaged, IEntityComponent
        {
            int componentIdx = math.abs(TypeHelper.TypeOf<TComponent>.Type.GetHashCode()) % m_ComponentBuffer.Length;
            
            if (!PoolContainer<QueryBuilder<TComponent>>.Initialized)
            {
                PoolContainer<QueryBuilder<TComponent>>.Initialize(QueryBuilder<TComponent>.QueryFactory, 32);
            }

            QueryBuilder<TComponent> builder = PoolContainer<QueryBuilder<TComponent>>.Dequeue();
            builder.Entities = m_ComponentBuffer[componentIdx].entity;
            builder.Components = (TComponent*)m_ComponentBuffer[componentIdx].buffer;
            builder.Length = m_ComponentBuffer[componentIdx].length;

            return builder;
        }

        private static int AlignOf(Type t)
        {
            Type temp = typeof(AlignOfHelper<>).MakeGenericType(t);

            return UnsafeUtility.SizeOf(temp) - UnsafeUtility.SizeOf(t);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AlignOfHelper<T> where T : struct
        {
            public byte dummy;
            public T data;
        }
        unsafe internal struct EntityComponentBuffer : IDisposable
        {
            public const int c_InitialCount = 512;

            //public Hash hash;
            public int index;

            public int length;

            [NativeDisableUnsafePtrRestriction] public bool* occupied;
            [NativeDisableUnsafePtrRestriction] public EntityData<IEntityData>* entity;
            [NativeDisableUnsafePtrRestriction] public void* buffer;

            public bool IsCreated
            {
                get
                {
                    unsafe
                    {
                        return buffer != null;
                    }
                }
            }
            public void Initialize(void* occupied, void* entity, void* buffer, int length)
            {
                if (IsCreated)
                {
                    UnsafeUtility.Free(occupied, Allocator.Persistent);
                    UnsafeUtility.Free(entity, Allocator.Persistent);
                    UnsafeUtility.Free(buffer, Allocator.Persistent);
                }

                this.occupied = (bool*)occupied;
                this.entity = (EntityData<IEntityData>*)entity;
                this.buffer = buffer;
                this.length = length;
            }

            public void Dispose()
            {
                if (!IsCreated)
                {
                    "??".ToLogError();
                    return;
                }

                UnsafeUtility.Free(occupied, Allocator.Persistent);
                UnsafeUtility.Free(entity, Allocator.Persistent);
                UnsafeUtility.Free(buffer, Allocator.Persistent);
            }
        }
    }

    public delegate void EntityComponentDelegate<TEntity, TComponent>(in TEntity entity, in TComponent component) where TComponent : unmanaged, IEntityComponent;
}
