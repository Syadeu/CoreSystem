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
using System.Threading;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

using TypeInfo = Syadeu.Collections.TypeInfo;

namespace Syadeu.Presentation.Components
{
    public struct ComponentType
    {
        public static SharedStatic<ComponentType> GetValue(Type componentType)
        {
            return SharedStatic<ComponentType>.GetOrCreate(
                TypeHelper.TypeOf<EntityComponentSystem>.Type,
                componentType, (uint)UnsafeUtility.AlignOf<ComponentType>());
        }

        internal unsafe UnsafeRingQueue<int>* m_ComponentECBRequester;

        internal unsafe ComponentBuffer* m_ComponentBuffer;
        internal int m_ComponentIndex;

        public int Length
        {
            get
            {
                unsafe
                {
                    return m_ComponentBuffer->Length;
                }
            }
        }
        public ref EntityComponentBuffer ECB
        {
            get
            {
                unsafe
                {
                    Interlocked.MemoryBarrier();
                    if (!m_ComponentBuffer->m_ECB.Value.IsCreated)
                    {
                        ref var ecb = ref m_ComponentBuffer->m_ECB.Value;
                        ecb = new EntityComponentBuffer(1024);

                        ref UnsafeRingQueue<int> requester
                            = ref UnsafeUtility.AsRef<UnsafeRingQueue<int>>(m_ComponentECBRequester);

                        requester.Enqueue(m_ComponentIndex);

                        CoreSystem.Logger.Log(LogChannel.Component, true,
                            $"New ECB has been created for component({TypeHelper.ToString(m_ComponentBuffer->TypeInfo.Type)})");
                    }
                    Interlocked.MemoryBarrier();

                    return ref m_ComponentBuffer->m_ECB.Value;
                }
            }
        }

        private static int GetEntityIndex(in InstanceID entity)
        {
            return Math.Abs(entity.GetHashCode()) % ComponentBuffer.c_InitialCount;
        }

        public ref TComponent ComponentAt<TComponent>(in int index) 
            where TComponent : unmanaged, IEntityComponent
        {
            unsafe
            {
                return ref m_ComponentBuffer->ElementAt<TComponent>(in index);
            }
        }

        //public bool HasComponent(in InstanceID entity)
        //{
        //    int index = GetEntityIndex(in entity);
        //    unsafe
        //    {
        //        if (!m_ComponentBuffer->Find(in entity, ref index))
        //        {
        //            return false;
        //        }
        //    }

        //    return true;
        //}
        public void AddComponent<T>(in InstanceID entity, ref T component) 
            where T : unmanaged, IEntityComponent
        {
            var wr = ECB.Begin();
            ECB.Add(ref wr, in entity, ref component);
            ECB.End(ref wr);
        }
        public void RemoveComponent(in InstanceID entity)
        {
            var wr = ECB.Begin();
            ECB.Remove(ref wr, in entity);
            ECB.End(ref wr);
        }
    }
    public struct ComponentType<TComponent> where TComponent : unmanaged, IEntityComponent
    {
        private static SharedStatic<ComponentType> Value
            => SharedStatic<ComponentType>.GetOrCreate<EntityComponentSystem, TComponent>((uint)UnsafeUtility.AlignOf<ComponentType>());

        internal static unsafe ComponentBuffer* ComponentBuffer => Value.Data.m_ComponentBuffer;

        public static int Length => Value.Data.Length;
        public static ref TComponent ComponentAt(in int index) => ref Value.Data.ComponentAt<TComponent>(in index);

        public static ref EntityComponentBuffer ECB => ref Value.Data.ECB;
    }
}
