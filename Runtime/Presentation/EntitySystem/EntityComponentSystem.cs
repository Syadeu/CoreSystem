using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Entities;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation
{
    unsafe internal sealed class EntityComponentSystem : PresentationSystemEntity<EntityComponentSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private long m_EntityLength;
        private NativeArray<EntityComponentBuffer> m_ComponentBuffer;
        //private readonly HashSet<int> m_InitializedBufferIndex = new HashSet<int>();

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
                    hash = types[i].GetHashCode(),
                    idx = types[i].GetHashCode() % length;

                
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
            //UnsafeUtility.MemCpy(m_ComponentBuffer.GetUnsafePtr(), tempBuffer, UnsafeUtility.SizeOf<EntityComponentBuffer>() * length);

            RequestSystem<EntitySystem>(Bind);

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
                //if (!m_InitializedBufferIndex.Contains(i)) continue;
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

        public void AddComponent<T>(EntityData<IEntityData> entity, T data) where T : unmanaged, IEntityComponent
        {
            CoreSystem.Logger.ThreadBlock(nameof(AddComponent), ThreadInfo.Unity);

            int componentIdx = math.abs(TypeHelper.TypeOf<T>.Type.GetHashCode()) % m_ComponentBuffer.Length;

            if (m_ComponentBuffer[componentIdx].length == 0)
            {
                long
                    occSize = UnsafeUtility.SizeOf<bool>() * EntityComponentBuffer.c_InitialCount,
                    idxSize = UnsafeUtility.SizeOf<Hash>() * EntityComponentBuffer.c_InitialCount,
                    bufferSize = UnsafeUtility.SizeOf<T>() * EntityComponentBuffer.c_InitialCount;
                void*
                    occBuffer = UnsafeUtility.Malloc(occSize, UnsafeUtility.AlignOf<bool>(), Allocator.Persistent),
                    idxBuffer = UnsafeUtility.Malloc(idxSize, UnsafeUtility.AlignOf<Hash>(), Allocator.Persistent),
                    buffer = UnsafeUtility.Malloc(bufferSize, UnsafeUtility.AlignOf<T>(), Allocator.Persistent);

                UnsafeUtility.MemClear(occBuffer, occSize);
                UnsafeUtility.MemClear(idxBuffer, idxSize);
                UnsafeUtility.MemClear(buffer, bufferSize);

                EntityComponentBuffer boxed = m_ComponentBuffer[componentIdx];
                boxed.occupied = (bool*)occBuffer;
                boxed.entity = (Hash*)idxBuffer;
                boxed.buffer = buffer;
                boxed.length = EntityComponentBuffer.c_InitialCount;
                m_ComponentBuffer[componentIdx] = boxed;

                //m_InitializedBufferIndex.Add(componentIdx);
            }

            int entityIdx = entity.Idx.ToInt32() % m_ComponentBuffer[componentIdx].length;

            if (m_ComponentBuffer[componentIdx].occupied[entityIdx] &&
                m_ComponentBuffer[componentIdx].entity[entityIdx] != entity.Idx &&
                m_EntitySystem != null &&
                !m_EntitySystem.IsDestroyed(m_ComponentBuffer[componentIdx].entity[entityIdx]))
            {
                int length = m_ComponentBuffer[componentIdx].length * 2;
                long
                    occSize = UnsafeUtility.SizeOf<bool>() * length,
                    idxSize = UnsafeUtility.SizeOf<Hash>() * length,
                    bufferSize = UnsafeUtility.SizeOf<T>() * length;
                void*
                    occBuffer = UnsafeUtility.Malloc(occSize, UnsafeUtility.AlignOf<bool>(), Allocator.Persistent),
                    idxBuffer = UnsafeUtility.Malloc(occSize, UnsafeUtility.AlignOf<Hash>(), Allocator.Persistent),
                    buffer = UnsafeUtility.Malloc(bufferSize, UnsafeUtility.AlignOf<T>(), Allocator.Persistent);

                UnsafeUtility.MemClear(occBuffer, occSize);
                UnsafeUtility.MemClear(idxBuffer, idxSize);
                UnsafeUtility.MemClear(buffer, bufferSize);

                for (int i = 0; i < m_ComponentBuffer[componentIdx].length; i++)
                {
                    if (!m_ComponentBuffer[componentIdx].occupied[i]) continue;

                    int newEntityIdx = m_ComponentBuffer[componentIdx].entity[i].ToInt32() % length;

                    if (((bool*)occBuffer)[newEntityIdx])
                    {
                        "... conflect again".ToLogError();
                    }

                    ((bool*)occBuffer)[newEntityIdx] = true;
                    ((Hash*)idxBuffer)[newEntityIdx] = m_ComponentBuffer[componentIdx].entity[i];
                    ((T*)buffer)[newEntityIdx] = ((T*)m_ComponentBuffer[componentIdx].buffer)[i];
                }

                EntityComponentBuffer boxed = m_ComponentBuffer[componentIdx];

                UnsafeUtility.Free(boxed.occupied, Allocator.Persistent);
                UnsafeUtility.Free(boxed.entity, Allocator.Persistent);
                UnsafeUtility.Free(boxed.buffer, Allocator.Persistent);

                boxed.occupied = (bool*)occBuffer;
                boxed.entity = (Hash*)idxBuffer;
                boxed.buffer = buffer;
                boxed.length = length;
                m_ComponentBuffer[componentIdx] = boxed;

                $"Component {TypeHelper.TypeOf<T>.Name} buffer increased to {length}".ToLog();

                AddComponent(entity, data);
                return;
            }

            ((T*)m_ComponentBuffer[componentIdx].buffer)[entityIdx] = data;
            m_ComponentBuffer[componentIdx].occupied[entityIdx] = true;
            m_ComponentBuffer[componentIdx].entity[entityIdx] = entity.Idx;

            $"Component {TypeHelper.TypeOf<T>.Name} set at entity({entity.Name})".ToLog();
        }
        public T GetComponent<T>(EntityData<IEntityData> entity) where T : unmanaged, IEntityComponent
        {
            int 
                componentIdx = math.abs(TypeHelper.TypeOf<T>.Type.GetHashCode()) % m_ComponentBuffer.Length,
                entityIdx = entity.Idx.ToInt32() % m_ComponentBuffer[componentIdx].length;

            if (!m_ComponentBuffer[componentIdx].occupied[entityIdx])
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({entity.Name}) doesn\'t have component({TypeHelper.TypeOf<T>.Name})");
                return default(T);
            }

            if (m_ComponentBuffer[componentIdx].entity[entityIdx] != entity.Idx)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Component({TypeHelper.TypeOf<T>.Name}) validation error. Maybe conflect.");
                return default(T);
            }

            return ((T*)m_ComponentBuffer[componentIdx].buffer)[entityIdx];
        }
    }

    unsafe internal struct EntityComponentBuffer : IDisposable
    {
        public const int c_InitialCount = 512;

        public int hash;
        public int index;

        public int length;

        [NativeDisableUnsafePtrRestriction] public bool* occupied;
        [NativeDisableUnsafePtrRestriction] public Hash* entity;
        [NativeDisableUnsafePtrRestriction] public void* buffer;

        public void Dispose()
        {
            UnsafeUtility.Free(occupied, Allocator.Persistent);
            UnsafeUtility.Free(entity, Allocator.Persistent);
            UnsafeUtility.Free(buffer, Allocator.Persistent);
        }
    }

    public interface IEntityComponent
    {

    }
}
