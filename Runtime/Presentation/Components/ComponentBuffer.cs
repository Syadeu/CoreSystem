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
using Syadeu.Presentation.Actor;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using TypeInfo = Syadeu.Collections.TypeInfo;

namespace Syadeu.Presentation.Components
{
    [BurstCompatible]
    internal unsafe struct ComponentBuffer : IDisposable
    {
        public const int c_InitialCount = 512;

        private TypeInfo m_ComponentTypeInfo;

        private int m_Length;
        private int m_Increased;

        [NativeDisableUnsafePtrRestriction] private InstanceID* m_EntityBuffer;
        [NativeDisableUnsafePtrRestriction] private void* m_ComponentBuffer;

        public TypeInfo TypeInfo => m_ComponentTypeInfo;
        public bool IsCreated => m_ComponentBuffer != null;
        public int Length => m_Length;

        public void Initialize(in TypeInfo typeInfo)
        {
            int
                //occSize = UnsafeUtility.SizeOf<bool>() * c_InitialCount,
                idxSize = UnsafeUtility.SizeOf<InstanceID>() * c_InitialCount,
                bufferSize = typeInfo.Size * c_InitialCount;
            void*
                //occBuffer = UnsafeUtility.Malloc(occSize, UnsafeUtility.AlignOf<bool>(), Allocator.Persistent),
                idxBuffer = UnsafeUtility.Malloc(idxSize, UnsafeUtility.AlignOf<InstanceID>(), Allocator.Persistent),
                buffer = UnsafeUtility.Malloc(bufferSize, typeInfo.Align, Allocator.Persistent);

            //UnsafeUtility.MemClear(occBuffer, occSize);
            // TODO: 할당되지도 않았는데 엔티티와 데이터 버퍼는 초기화 할 필요가 있나?
            UnsafeUtility.MemClear(idxBuffer, idxSize);
            UnsafeUtility.MemClear(buffer, bufferSize);

            this.m_ComponentTypeInfo = typeInfo;
            //this.m_OccupiedBuffer = (bool*)occBuffer;
            this.m_EntityBuffer = (InstanceID*)idxBuffer;
            this.m_ComponentBuffer = buffer;
            this.m_Length = c_InitialCount;
            m_Increased = 1;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CLSTypedDictionary<ComponentBufferAtomicSafety>.SetValue(m_ComponentTypeInfo.Type,
                ComponentBufferAtomicSafety.Construct(typeInfo));
#endif
        }
        public void Increment()
        {
            if (!IsCreated) throw new Exception();

            int count = c_InitialCount * (m_Increased + 1);
            long
                idxSize = UnsafeUtility.SizeOf<InstanceID>() * count,
                bufferSize = TypeInfo.Size * count;
            void*
                idxBuffer = UnsafeUtility.Malloc(idxSize, UnsafeUtility.AlignOf<InstanceID>(), Allocator.Persistent),
                buffer = UnsafeUtility.Malloc(bufferSize, TypeInfo.Align, Allocator.Persistent);

            UnsafeUtility.MemClear(idxBuffer, idxSize);
            UnsafeUtility.MemClear(buffer, bufferSize);

            UnsafeUtility.MemCpy(idxBuffer, m_EntityBuffer, UnsafeUtility.SizeOf<InstanceID>() * m_Length);
            UnsafeUtility.MemCpy(buffer, m_ComponentBuffer, TypeInfo.Size * m_Length);

            UnsafeUtility.Free(this.m_EntityBuffer, Allocator.Persistent);
            UnsafeUtility.Free(this.m_ComponentBuffer, Allocator.Persistent);

            this.m_EntityBuffer = (InstanceID*)idxBuffer;
            this.m_ComponentBuffer = buffer;

            m_Increased += 1;
            m_Length = c_InitialCount * m_Increased;

            CoreSystem.Logger.Log(Channel.Component, $"increased {TypeHelper.ToString(TypeInfo.Type)} {m_Length} :: {m_Increased}");
        }

        public bool Find(InstanceID entity, ref int entityIndex)
        {
            if (m_Length == 0)
            {
                return false;
            }

            //int index = entityIndex;
            //for (int i = 0; i < m_Increased && m_EntityBuffer[index].IsEmpty() && !m_EntityBuffer[index].Equals(entity); i++)
            //{
            //    index = (c_InitialCount * i) + entityIndex;
            //}

            for (int i = 0; i < m_Increased; i++)
            {
                int idx = (c_InitialCount * i) + entityIndex;

                if (m_EntityBuffer[idx].IsEmpty()) continue;
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

                if (!m_EntityBuffer[idx].IsEmpty()) continue;

                entityIndex = idx;
                return true;
            }

            return false;
        }

        public void HasElementAt(int i, out bool result)
        {
            result = !m_EntityBuffer[i].IsEmpty();
        }
        public bool HasElementAt(int i)
        {
            return !m_EntityBuffer[i].IsEmpty();
        }
        [BurstCompatible(GenericTypeArguments = new [] {typeof(ActorControllerComponent)})]
        public void ElementAt<TComponent>(int i, out InstanceID entity, out TComponent component)
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (!TypeHelper.TypeOf<TComponent>.Type.Equals(TypeInfo.Type))
            {
                CoreSystem.Logger.LogError(Channel.Component,
                    $"Trying to access component with an invalid type({TypeHelper.TypeOf<TComponent>.ToString()}). " +
                    $"This buffer type is {TypeHelper.ToString(TypeInfo.Type)}.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
#endif
            entity = m_EntityBuffer[i];
            component = ((TComponent*)m_ComponentBuffer)[i];
        }
        [BurstCompatible(GenericTypeArguments = new[] { typeof(ActorControllerComponent) })]
        public void ElementAt<TComponent>(int i, out InstanceID entity, out TComponent* component)
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (!TypeHelper.TypeOf<TComponent>.Type.Equals(TypeInfo.Type))
            {
                CoreSystem.Logger.LogError(Channel.Component,
                    $"Trying to access component with an invalid type({TypeHelper.TypeOf<TComponent>.ToString()}). " +
                    $"This buffer type is {TypeHelper.ToString(TypeInfo.Type)}.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
#endif
            entity = m_EntityBuffer[i];
            component = ((TComponent*)m_ComponentBuffer) + i;
        }
        [BurstCompatible]
        public IntPtr ElementAt(in int i)
        {
            IntPtr p = (IntPtr)m_ComponentBuffer;
            // Align 은 필요없음.
            return IntPtr.Add(p, TypeInfo.Size * i);
        }

        [BurstDiscard]
        public void RemoveAt(in int index)
        {
            IntPtr p = ElementAt(in index);

            object obj = System.Runtime.InteropServices.Marshal.PtrToStructure(p, TypeInfo.Type);

            // 해당 컴포넌트가 IDisposable 인터페이스를 상속받으면 해당 인터페이스를 실행
            if (obj is IDisposable disposable)
            {
                disposable.Dispose();

                CoreSystem.Logger.Log(Channel.Component,
                    $"{TypeInfo.Type.Name} component at {m_EntityBuffer[index].Hash}:{m_EntityBuffer[index].GetObject()?.Name} disposed.");
            }

            CoreSystem.Logger.Log(Channel.Component,
                $"{TypeInfo.Type.Name} component at {m_EntityBuffer[index].Hash}:{m_EntityBuffer[index].GetObject()?.Name} removed");

            m_EntityBuffer[index] = InstanceID.Empty;
        }

        [BurstCompatible]
        public void SetElementAt(in int index, in InstanceID entity)
        {
            IntPtr p = ElementAt(in index);
            UnsafeUtility.MemClear(p.ToPointer(), TypeInfo.Size);
            m_EntityBuffer[index] = entity;
        }

        [BurstDiscard]
        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            ComponentBufferAtomicSafety safety
                = CLSTypedDictionary<ComponentBufferAtomicSafety>.GetValue(m_ComponentTypeInfo.Type);

            safety.CheckExistsAndThrow();
#endif

            //UnsafeUtility.Free(m_OccupiedBuffer, Allocator.Persistent);
            UnsafeUtility.Free(m_EntityBuffer, Allocator.Persistent);
            UnsafeUtility.Free(m_ComponentBuffer, Allocator.Persistent);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            safety.Dispose();
#endif
        }
    }
}
