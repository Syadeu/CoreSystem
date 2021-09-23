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
        private readonly HashSet<int> m_InitializedBufferIndex = new HashSet<int>();

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

            EntityComponentBuffer* tempBuffer = stackalloc EntityComponentBuffer[length];
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

            m_ComponentBuffer = new NativeArray<EntityComponentBuffer>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            UnsafeUtility.MemCpy(m_ComponentBuffer.GetUnsafePtr(), tempBuffer, UnsafeUtility.SizeOf<EntityComponentBuffer>() * length);

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
                if (!m_InitializedBufferIndex.Contains(i)) continue;

                m_ComponentBuffer[i].Dispose();
            }
            m_ComponentBuffer.Dispose();
        }

        public void AddComponent<T>(IEntityData entity, T data) where T : unmanaged, IEntityComponent
        {
            CoreSystem.Logger.ThreadBlock(nameof(AddComponent), ThreadInfo.Unity);

            int componentIdx = TypeHelper.TypeOf<T>.Type.GetHashCode() % m_ComponentBuffer.Length;

            if (m_ComponentBuffer[componentIdx].length == 0)
            {
                long
                    occSize = UnsafeUtility.SizeOf<int2>() * EntityComponentBuffer.c_InitialCount,
                    bufferSize = UnsafeUtility.SizeOf<T>() * EntityComponentBuffer.c_InitialCount;
                void*
                    occBuffer = UnsafeUtility.Malloc(occSize, UnsafeUtility.AlignOf<int2>(), Allocator.Persistent),
                    buffer = UnsafeUtility.Malloc(bufferSize, UnsafeUtility.AlignOf<T>(), Allocator.Persistent);

                UnsafeUtility.MemClear(occBuffer, occSize);
                UnsafeUtility.MemClear(buffer, bufferSize);

                EntityComponentBuffer boxed = m_ComponentBuffer[componentIdx];
                boxed.occupied = (int2*)occBuffer;
                boxed.buffer = buffer;
                boxed.length = EntityComponentBuffer.c_InitialCount;
                m_ComponentBuffer[componentIdx] = boxed;

                m_InitializedBufferIndex.Add(componentIdx);
            }

            int entityIdx = entity.Idx.ToInt32() % m_ComponentBuffer[componentIdx].length;

            if (m_ComponentBuffer[componentIdx].occupied[entityIdx].x == 1 &&
                m_ComponentBuffer[componentIdx].occupied[entityIdx].y != entity.Idx.ToInt32())
            {
                int length = m_ComponentBuffer[componentIdx].length * 2;
                long
                    occSize = UnsafeUtility.SizeOf<int2>() * length,
                    bufferSize = UnsafeUtility.SizeOf<T>() * length;
                void*
                    occBuffer = UnsafeUtility.Malloc(occSize, UnsafeUtility.AlignOf<int2>(), Allocator.Persistent),
                    buffer = UnsafeUtility.Malloc(bufferSize, UnsafeUtility.AlignOf<T>(), Allocator.Persistent);

                UnsafeUtility.MemClear(occBuffer, occSize);
                UnsafeUtility.MemClear(buffer, bufferSize);

                for (int i = 0; i < m_ComponentBuffer[componentIdx].length; i++)
                {
                    if (m_ComponentBuffer[componentIdx].occupied[i].x == 0) continue;

                    int newEntityIdx = m_ComponentBuffer[componentIdx].occupied[i].y % length;

                    if (((int2*)occBuffer)[newEntityIdx].x == 1)
                    {
                        "... conflect again".ToLogError();
                    }

                    ((int2*)occBuffer)[newEntityIdx] = new int2(1, m_ComponentBuffer[componentIdx].occupied[i].y);
                    ((T*)buffer)[newEntityIdx] = ((T*)m_ComponentBuffer[componentIdx].buffer)[i];
                }

                //UnsafeUtility.MemCpy(occBuffer, m_ComponentBuffer[componentIdx].occupied, UnsafeUtility.SizeOf<int2>() * m_ComponentBuffer[componentIdx].length);
                //UnsafeUtility.MemCpy(buffer, m_ComponentBuffer[componentIdx].buffer, UnsafeUtility.SizeOf<T>() * m_ComponentBuffer[componentIdx].length);

                EntityComponentBuffer boxed = m_ComponentBuffer[componentIdx];

                UnsafeUtility.Free(boxed.occupied, Allocator.Persistent);
                UnsafeUtility.Free(boxed.buffer, Allocator.Persistent);

                boxed.occupied = (int2*)occBuffer;
                boxed.buffer = buffer;
                boxed.length = length;
                m_ComponentBuffer[componentIdx] = boxed;

                AddComponent(entity, data);
                return;
            }

            ((T*)m_ComponentBuffer[componentIdx].buffer)[entityIdx] = data;
            m_ComponentBuffer[componentIdx].occupied[entityIdx] = new int2(1, entity.Idx.ToInt32());
        }
        public T GetComponent<T>(IEntityData entity) where T : unmanaged, IEntityComponent
        {
            int 
                componentIdx = TypeHelper.TypeOf<T>.Type.GetHashCode() % m_ComponentBuffer.Length,
                entityIdx = entity.Idx.ToInt32() % m_ComponentBuffer[componentIdx].length;

            if (m_ComponentBuffer[componentIdx].occupied[entityIdx].x == 0)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({entity.Name}) doesn\'t have component({TypeHelper.TypeOf<T>.Name})");
                return default(T);
            }

            if (m_ComponentBuffer[componentIdx].occupied[entityIdx].y != entity.Idx.ToInt32())
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

        [NativeDisableUnsafePtrRestriction] public int2* occupied;
        [NativeDisableUnsafePtrRestriction] public void* buffer;

        public void Dispose()
        {
            UnsafeUtility.Free(occupied, Allocator.Persistent);
            UnsafeUtility.Free(buffer, Allocator.Persistent);
        }
    }

    public interface IEntityComponent
    {

    }
}
