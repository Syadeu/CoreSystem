// Copyright 2021 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using TypeInfo = Syadeu.Collections.TypeInfo;

namespace Syadeu.Presentation.Components
{
    internal unsafe struct ComponentBuffer : IDisposable
    {
        public const int c_InitialCount = 512;

        private TypeInfo m_ComponentTypeInfo;

        private int m_Length;
        private int m_Increased;

        [NativeDisableUnsafePtrRestriction] public bool* m_OccupiedBuffer;
        [NativeDisableUnsafePtrRestriction] public InstanceID* m_EntityBuffer;
        [NativeDisableUnsafePtrRestriction] public void* m_ComponentBuffer;

        public TypeInfo TypeInfo => m_ComponentTypeInfo;
        public bool IsCreated => m_ComponentBuffer != null;
        public int Length => m_Length;

        public void Initialize(in TypeInfo typeInfo)
        {
            int
                occSize = UnsafeUtility.SizeOf<bool>() * c_InitialCount,
                idxSize = UnsafeUtility.SizeOf<InstanceID>() * c_InitialCount,
                bufferSize = typeInfo.Size * c_InitialCount;
            void*
                occBuffer = UnsafeUtility.Malloc(occSize, UnsafeUtility.AlignOf<bool>(), Allocator.Persistent),
                idxBuffer = UnsafeUtility.Malloc(idxSize, UnsafeUtility.AlignOf<InstanceID>(), Allocator.Persistent),
                buffer = UnsafeUtility.Malloc(bufferSize, typeInfo.Align, Allocator.Persistent);

            UnsafeUtility.MemClear(occBuffer, occSize);
            // TODO: 할당되지도 않았는데 엔티티와 데이터 버퍼는 초기화 할 필요가 있나?
            UnsafeUtility.MemClear(idxBuffer, idxSize);
            UnsafeUtility.MemClear(buffer, bufferSize);

            this.m_ComponentTypeInfo = typeInfo;
            this.m_OccupiedBuffer = (bool*)occBuffer;
            this.m_EntityBuffer = (InstanceID*)idxBuffer;
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
                idxSize = UnsafeUtility.SizeOf<InstanceID>() * count,
                bufferSize = UnsafeUtility.SizeOf<TComponent>() * count;
            void*
                occBuffer = UnsafeUtility.Malloc(occSize, UnsafeUtility.AlignOf<bool>(), Allocator.Persistent),
                idxBuffer = UnsafeUtility.Malloc(idxSize, UnsafeUtility.AlignOf<InstanceID>(), Allocator.Persistent),
                buffer = UnsafeUtility.Malloc(bufferSize, UnsafeUtility.AlignOf<TComponent>(), Allocator.Persistent);

            UnsafeUtility.MemClear(occBuffer, occSize);
            UnsafeUtility.MemClear(idxBuffer, idxSize);
            UnsafeUtility.MemClear(buffer, bufferSize);

            UnsafeUtility.MemCpy(occBuffer, m_OccupiedBuffer, UnsafeUtility.SizeOf<bool>() * m_Length);
            UnsafeUtility.MemCpy(idxBuffer, m_EntityBuffer, UnsafeUtility.SizeOf<InstanceID>() * m_Length);
            UnsafeUtility.MemCpy(buffer, m_ComponentBuffer, UnsafeUtility.SizeOf<TComponent>() * m_Length);

            UnsafeUtility.Free(this.m_OccupiedBuffer, Allocator.Persistent);
            UnsafeUtility.Free(this.m_EntityBuffer, Allocator.Persistent);
            UnsafeUtility.Free(this.m_ComponentBuffer, Allocator.Persistent);

            this.m_OccupiedBuffer = (bool*)occBuffer;
            this.m_EntityBuffer = (InstanceID*)idxBuffer;
            this.m_ComponentBuffer = buffer;

            m_Increased += 1;
            m_Length = c_InitialCount * m_Increased;

            CoreSystem.Logger.Log(Channel.Component, $"increased {TypeHelper.TypeOf<TComponent>.Name} {m_Length} :: {m_Increased}");
        }

        public bool Find(InstanceID entity, ref int entityIndex)
        {
            if (m_Length == 0)
            {
                return false;
            }

            for (int i = 0; i < m_Increased; i++)
            {
                int idx = (c_InitialCount * i) + entityIndex;

                if (!m_OccupiedBuffer[idx]) continue;
                else if (this.m_EntityBuffer[idx].Equals(entity))
                {
                    entityIndex = idx;
                    return true;
                }
            }

            return false;
        }
        public bool FindEmpty(InstanceID entity, ref int entityIndex)
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
        public void ElementAt<TComponent>(int i, out InstanceID entity, out TComponent component)
            where TComponent : unmanaged, IEntityComponent
        {
            entity = m_EntityBuffer[i];
            component = ((TComponent*)m_ComponentBuffer)[i];
        }
        public void ElementAt<TComponent>(int i, out InstanceID entity, out TComponent* component)
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
        public void ElementAt(int i, out IntPtr ptr, out InstanceID entity)
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
}
