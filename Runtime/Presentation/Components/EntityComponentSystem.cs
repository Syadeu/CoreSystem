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
        internal NativeArray<EntityComponentBuffer> m_ComponentBuffer;

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
                int 
                    hash = math.abs(types[i].GetHashCode()),
                    idx = hash % length;

                if (hash.Equals(tempBuffer[idx].hash))
                {
                    "require increase buffer size".ToLogError();
                    continue;
                }

                tempBuffer[idx] = new EntityComponentBuffer()
                {
                    hash = hash,
                    index = idx,

                    length = 0
                };
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
                if (m_ComponentBuffer[i].length == 0) continue;

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

        public void AddComponent<TComponent>(EntityData<IEntityData> entity, TComponent data) where TComponent : unmanaged, IEntityComponent
        {
            CoreSystem.Logger.ThreadBlock(nameof(AddComponent), ThreadInfo.Unity);

            int componentIdx = math.abs(TypeHelper.TypeOf<TComponent>.Type.GetHashCode()) % m_ComponentBuffer.Length;

            if (m_ComponentBuffer[componentIdx].length == 0)
            {
                long
                    occSize = UnsafeUtility.SizeOf<bool>() * EntityComponentBuffer.c_InitialCount,
                    idxSize = UnsafeUtility.SizeOf<EntityData<IEntityData>>() * EntityComponentBuffer.c_InitialCount,
                    bufferSize = UnsafeUtility.SizeOf<TComponent>() * EntityComponentBuffer.c_InitialCount;
                void*
                    occBuffer = UnsafeUtility.Malloc(occSize, UnsafeUtility.AlignOf<bool>(), Allocator.Persistent),
                    idxBuffer = UnsafeUtility.Malloc(idxSize, UnsafeUtility.AlignOf<EntityData<IEntityData>>(), Allocator.Persistent),
                    buffer = UnsafeUtility.Malloc(bufferSize, UnsafeUtility.AlignOf<TComponent>(), Allocator.Persistent);

                UnsafeUtility.MemClear(occBuffer, occSize);
                UnsafeUtility.MemClear(idxBuffer, idxSize);
                UnsafeUtility.MemClear(buffer, bufferSize);

                EntityComponentBuffer boxed = m_ComponentBuffer[componentIdx];
                boxed.occupied = (bool*)occBuffer;
                boxed.entity = (EntityData<IEntityData>*)idxBuffer;
                boxed.buffer = buffer;
                boxed.length = EntityComponentBuffer.c_InitialCount;
                m_ComponentBuffer[componentIdx] = boxed;
            }

            int entityIdx = entity.Idx.ToInt32() % m_ComponentBuffer[componentIdx].length;

            if (m_ComponentBuffer[componentIdx].occupied[entityIdx] &&
                !m_ComponentBuffer[componentIdx].entity[entityIdx].Equals(entity) &&
                m_EntitySystem != null &&
                !m_EntitySystem.IsDestroyed(m_ComponentBuffer[componentIdx].entity[entityIdx].Idx))
            {
                int length = m_ComponentBuffer[componentIdx].length * 2;
                long
                    occSize = UnsafeUtility.SizeOf<bool>() * length,
                    idxSize = UnsafeUtility.SizeOf<EntityData<IEntityData>>() * length,
                    bufferSize = UnsafeUtility.SizeOf<TComponent>() * length;
                void*
                    occBuffer = UnsafeUtility.Malloc(occSize, UnsafeUtility.AlignOf<bool>(), Allocator.Persistent),
                    idxBuffer = UnsafeUtility.Malloc(occSize, UnsafeUtility.AlignOf<EntityData<IEntityData>>(), Allocator.Persistent),
                    buffer = UnsafeUtility.Malloc(bufferSize, UnsafeUtility.AlignOf<TComponent>(), Allocator.Persistent);

                UnsafeUtility.MemClear(occBuffer, occSize);
                UnsafeUtility.MemClear(idxBuffer, idxSize);
                UnsafeUtility.MemClear(buffer, bufferSize);

                for (int i = 0; i < m_ComponentBuffer[componentIdx].length; i++)
                {
                    if (!m_ComponentBuffer[componentIdx].occupied[i]) continue;

                    int newEntityIdx = m_ComponentBuffer[componentIdx].entity[i].Idx.ToInt32() % length;

                    if (((bool*)occBuffer)[newEntityIdx])
                    {
                        "... conflect again".ToLogError();
                    }

                    ((bool*)occBuffer)[newEntityIdx] = true;
                    ((EntityData<IEntityData>*)idxBuffer)[newEntityIdx] = m_ComponentBuffer[componentIdx].entity[i];
                    ((TComponent*)buffer)[newEntityIdx] = ((TComponent*)m_ComponentBuffer[componentIdx].buffer)[i];
                }

                EntityComponentBuffer boxed = m_ComponentBuffer[componentIdx];

                UnsafeUtility.Free(boxed.occupied, Allocator.Persistent);
                UnsafeUtility.Free(boxed.entity, Allocator.Persistent);
                UnsafeUtility.Free(boxed.buffer, Allocator.Persistent);

                boxed.occupied = (bool*)occBuffer;
                boxed.entity = (EntityData<IEntityData>*)idxBuffer;
                boxed.buffer = buffer;
                boxed.length = length;
                m_ComponentBuffer[componentIdx] = boxed;

                $"Component {TypeHelper.TypeOf<TComponent>.Name} buffer increased to {length}".ToLog();

                AddComponent(entity, data);
                return;
            }

            ((TComponent*)m_ComponentBuffer[componentIdx].buffer)[entityIdx] = data;
            m_ComponentBuffer[componentIdx].occupied[entityIdx] = true;
            m_ComponentBuffer[componentIdx].entity[entityIdx] = entity.Idx;

            $"Component {TypeHelper.TypeOf<TComponent>.Name} set at entity({entity.Name})".ToLog();
        }
        public bool HasComponent<TComponent>(EntityData<IEntityData> entity) 
            where TComponent : unmanaged, IEntityComponent
        {
            int
                componentIdx = math.abs(TypeHelper.TypeOf<TComponent>.Type.GetHashCode()) % m_ComponentBuffer.Length,
                entityIdx = entity.Idx.ToInt32() % m_ComponentBuffer[componentIdx].length;

            if (!m_ComponentBuffer[componentIdx].occupied[entityIdx])
            {
                return false;
            }

            if (!m_ComponentBuffer[componentIdx].entity[entityIdx].Equals(entity))
            {
                if (!m_EntitySystem.IsDestroyed(m_ComponentBuffer[componentIdx].entity[entityIdx].Idx))
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
            int 
                componentIdx = math.abs(TypeHelper.TypeOf<TComponent>.Type.GetHashCode()) % m_ComponentBuffer.Length,
                entityIdx = entity.Idx.ToInt32() % m_ComponentBuffer[componentIdx].length;

            if (!m_ComponentBuffer[componentIdx].occupied[entityIdx])
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({entity.Name}) doesn\'t have component({TypeHelper.TypeOf<TComponent>.Name})");
                return default(TComponent);
            }

            if (!m_ComponentBuffer[componentIdx].entity[entityIdx].Equals(entity) &&
                !m_EntitySystem.IsDestroyed(m_ComponentBuffer[componentIdx].entity[entityIdx].Idx))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Component({TypeHelper.TypeOf<TComponent>.Name}) validation error. Maybe conflect.");
                return default(TComponent);
            }

            return ((TComponent*)m_ComponentBuffer[componentIdx].buffer)[entityIdx];
        }

        public QueryBuilder<TComponent> CreateQueryBuilder<TComponent>() where TComponent : unmanaged, IEntityComponent
        {
            int componentIdx = math.abs(TypeHelper.TypeOf<TComponent>.Type.GetHashCode()) % m_ComponentBuffer.Length;

            
            QueryBuilder<TComponent> queryBuilder = new QueryBuilder<TComponent>
            {
                ComponentIndex = componentIdx,
            };

            return queryBuilder;
        }

        unsafe internal struct EntityComponentBuffer : IDisposable
        {
            public const int c_InitialCount = 512;

            public int hash;
            public int index;

            public int length;

            [NativeDisableUnsafePtrRestriction] public bool* occupied;
            [NativeDisableUnsafePtrRestriction] public EntityData<IEntityData>* entity;
            [NativeDisableUnsafePtrRestriction] public void* buffer;

            public void Dispose()
            {
                UnsafeUtility.Free(occupied, Allocator.Persistent);
                UnsafeUtility.Free(entity, Allocator.Persistent);
                UnsafeUtility.Free(buffer, Allocator.Persistent);
            }
        }
    }

    public delegate void EntityComponentDelegate<TEntity, TComponent>(in TEntity entity, in TComponent component) where TComponent : unmanaged, IEntityComponent;
}
