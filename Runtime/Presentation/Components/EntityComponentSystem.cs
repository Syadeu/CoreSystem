﻿using AOT;
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
                if (!m_ComponentIndices.TryGetValue(types[i], out int idx))
                {
                    idx = math.abs(types[i].GetHashCode()) % tempBuffer.Length;
                    m_ComponentIndices.Add(types[i], idx);
                }

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

                temp.SetIndex(idx);
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

            m_EntitySystem.OnEntityDestroy += M_EntitySystem_OnEntityDestroy;
        }
        private void M_EntitySystem_OnEntityDestroy(EntityData<IEntityData> obj)
        {
            foreach (var item in m_ComponentIndices.Keys)
            {
                int2 index = GetIndex(item, obj);
                if (!m_ComponentBuffer[index.x].Find(obj, ref index.y))
                {
                    continue;
                }

                m_ComponentBuffer[index.x].occupied[index.y] = false;
                $"{item.Name} component at {obj.Name} removed".ToLog();
            }
        }

        #endregion

        private readonly Dictionary<Type, int> m_ComponentIndices = new Dictionary<Type, int>();

        private int GetComponentIndex<TComponent>() => GetComponentIndex(TypeHelper.TypeOf<TComponent>.Type);
        private int GetComponentIndex(Type t)
        {
            if (!m_ComponentIndices.TryGetValue(t, out int componentIdx))
            {
                //componentIdx = math.abs(t.GetHashCode());
                //m_ComponentIndices.Add(t, componentIdx);
                throw new Exception();
            }

            return componentIdx;
        }
        private int GetEntityIndex(int componentIdx, EntityData<IEntityData> entity)
        {
            int idx = math.abs(entity.GetHashCode()) % EntityComponentBuffer.c_InitialCount;
            return idx;
        }
        private int2 GetIndex(Type t, EntityData<IEntityData> entity)
        {
            int
                cIdx = GetComponentIndex(t),
                eIdx = GetEntityIndex(cIdx, entity);

            return new int2(cIdx, eIdx);
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

            if (!m_ComponentBuffer[index.x].IsValidFor(in index.y, entity) &&
                !m_ComponentBuffer[index.x].Find(entity, ref index.y))
            {
                "require increment".ToLog();

                EntityComponentBuffer boxed = m_ComponentBuffer[index.x];
                boxed.Increment<TComponent>();
                m_ComponentBuffer[index.x] = boxed;

                if (!m_ComponentBuffer[index.x].Find(entity, ref index.y))
                {
                    throw new Exception();
                }
            }

            //if (m_ComponentBuffer[index.x].occupied[index.y] &&
            //    !m_ComponentBuffer[index.x].entity[index.y].Idx.Equals(entity.Idx) &&
            //    m_EntitySystem != null &&
            //    !m_EntitySystem.IsDestroyed(m_ComponentBuffer[index.x].entity[index.y].Idx))
            //{
            //    throw new Exception();
            //}

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

            if (m_ComponentBuffer[index.x].length == 0)
            {
                throw new Exception();
            }

            if (!m_ComponentBuffer[index.x].Find(entity, ref index.y))
            {
                return false;
            }

            //if (!m_ComponentBuffer[index.x].entity[index.y].Idx.Equals(entity.Idx))
            //{
            //    if (!m_EntitySystem.IsDestroyed(m_ComponentBuffer[index.x].entity[index.y].Idx))
            //    {
            //        CoreSystem.Logger.LogError(Channel.Entity,
            //            $"Component({TypeHelper.TypeOf<TComponent>.Name}) validation error. Maybe conflect.");
            //    }

            //    return false;
            //}

            return true;
        }
        public TComponent GetComponent<TComponent>(EntityData<IEntityData> entity) where TComponent : unmanaged, IEntityComponent
        {
            int2 index = GetIndex<TComponent>(entity);

            if (m_ComponentBuffer[index.x].length == 0)
            {
                throw new Exception();
            }

            if (!m_ComponentBuffer[index.x].Find(entity, ref index.y))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({entity.Name}) doesn\'t have component({TypeHelper.TypeOf<TComponent>.Name})");
                return default(TComponent);
            }

            //if (!m_ComponentBuffer[index.x].entity[index.y].Idx.Equals(entity.Idx) &&
            //    !m_EntitySystem.IsDestroyed(m_ComponentBuffer[index.x].entity[index.y].Idx))
            //{
            //    CoreSystem.Logger.LogError(Channel.Entity,
            //        $"Component({TypeHelper.TypeOf<TComponent>.Name}) validation error. Maybe conflect.");
            //    return default(TComponent);
            //}

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

            public int index;

            public int length;
            public int increased;

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

            public void SetIndex(in int index)
            {
                this.index = index;
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
                increased = 1;
            }
            public void Increment<TComponent>() where TComponent : unmanaged, IEntityComponent
            {
                if (!IsCreated) throw new Exception();

                int count = c_InitialCount * (increased + 1);
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

                UnsafeUtility.MemCpy(occBuffer, occupied, UnsafeUtility.SizeOf<bool>() * length);
                UnsafeUtility.MemCpy(idxBuffer, entity, UnsafeUtility.SizeOf<EntityData<IEntityData>>() * length);
                UnsafeUtility.MemCpy(buffer, buffer, UnsafeUtility.SizeOf<TComponent>() * length);

                UnsafeUtility.Free(this.occupied, Allocator.Persistent);
                UnsafeUtility.Free(this.entity, Allocator.Persistent);
                UnsafeUtility.Free(this.buffer, Allocator.Persistent);

                this.occupied = (bool*)occBuffer;
                this.entity = (EntityData<IEntityData>*)idxBuffer;
                this.buffer = buffer;

                increased += 1;
                length = c_InitialCount * increased;
            }

            public bool IsValidFor(in int entityIndex, EntityData<IEntityData> entity)
            {
                if (!occupied[entityIndex] || this.entity[entityIndex].Idx.Equals(entity.Idx)) return true;
                return false;
            }
            public bool Find(EntityData<IEntityData> entity, ref int entityIndex)
            {
                if (length == 0)
                {
                    entityIndex = -1;
                    return false;
                }

                for (int i = 0; i < increased; i++)
                {
                    int idx = (c_InitialCount * i) + entityIndex;

                    if (!occupied[idx]) continue;
                    else if (this.entity[idx].Idx.Equals(entity.Idx))
                    {
                        entityIndex = idx;
                        return true;
                    }
                }

                entityIndex = -1;
                return false;
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