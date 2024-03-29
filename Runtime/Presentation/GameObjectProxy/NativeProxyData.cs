﻿// Copyright 2021 Seung Ha Kim
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

using Syadeu.Collections;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Syadeu.Presentation.Proxy
{
    [NativeContainer, StructLayout(LayoutKind.Sequential)]
    unsafe internal struct NativeProxyData : IDisposable
    {
        public static readonly ProxyTransformData Null = new ProxyTransformData();
        private static readonly long s_BoolenSize = UnsafeUtility.SizeOf<bool>();
        private static readonly long s_HashSize = UnsafeUtility.SizeOf<Hash>();
        private static readonly long s_TransformSize = UnsafeUtility.SizeOf<ProxyTransformData>();

        #region Safeties
#if UNITY_EDITOR
        public AtomicSafetyHandle m_Safety;
        [NativeSetClassTypeToNullOnSchedule] public DisposeSentinel m_DisposeSentinel;
#endif
        #endregion

        public struct UnsafeList : IDisposable
        {
            [NativeDisableUnsafePtrRestriction] public IntPtr m_Buffer;
            public uint m_Length;
            public Allocator m_Allocator;

            public void* Pointer => m_Buffer.ToPointer();
            public ProxyTransformData* m_TransformBuffer => (ProxyTransformData*)m_Buffer;
            public uint Length => m_Length;

            public ProxyTransformData* this[int index]
            {
                get
                {
                    if (index >= m_Length || index < 0) throw new ArgumentOutOfRangeException();
                    return m_TransformBuffer + index;
                }
            }
            public void Dispose()
            {
                UnsafeUtility.MemClear(m_Buffer.ToPointer(), m_Length * s_TransformSize);
                UnsafeUtility.Free(m_Buffer.ToPointer(), m_Allocator);
            }

            public ref ProxyTransformData ElementAt(int index)
            {
                if (index < 0 || index >= m_Length) throw new ArgumentOutOfRangeException(nameof(index) + $" of {m_Length} at {index}");

                return ref m_TransformBuffer[index];
            }
        }

        [NativeDisableUnsafePtrRestriction] private UnsafeList* m_UnsafeList;
        private Allocator m_AllocatorLabel;

        public ProxyTransform this[int index]
        {
            get
            {
                if (index < 0 || index >= m_UnsafeList->m_Length) throw new ArgumentOutOfRangeException(nameof(index) + $"index of {index} in {m_UnsafeList->m_Length}");
                ProxyTransformData* p = (*m_UnsafeList)[index];

                return new ProxyTransform(m_UnsafeList, index, p->m_Generation, p->m_Hash);
            }
        }
        public ref UnsafeList List => ref *m_UnsafeList;

        public NativeProxyData(int length, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            Allocate(Convert.ToUInt32(length), allocator, out this);

            // Set the memory block to 0 if requested.
            if ((options & NativeArrayOptions.ClearMemory) == NativeArrayOptions.ClearMemory)
            {
                UnsafeUtility.MemClear(m_UnsafeList->m_Buffer.ToPointer(), length * s_TransformSize);
            }
        }
        public void Dispose()
        {
#if UNITY_EDITOR
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif

            (*m_UnsafeList).Dispose();
            UnsafeUtility.MemClear(m_UnsafeList, UnsafeUtility.SizeOf<UnsafeList>());
            UnsafeUtility.Free(m_UnsafeList, m_AllocatorLabel);
        }
        private static void Allocate(uint length, Allocator allocator, out NativeProxyData array)
        {
            if (allocator <= Allocator.None)
                throw new ArgumentException("Allocator must be Temp, TempJob or Persistent", nameof(allocator));

            array = default(NativeProxyData);
            array.m_UnsafeList = (UnsafeList*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<UnsafeList>(), UnsafeUtility.AlignOf<UnsafeList>(), allocator);

            array.m_UnsafeList->m_Buffer = (IntPtr)UnsafeUtility.Malloc(s_TransformSize * length, UnsafeUtility.AlignOf<ProxyTransformData>(), allocator);

            UnsafeUtility.MemClear(array.m_UnsafeList->Pointer, s_TransformSize * length);

            array.m_UnsafeList->m_Length = length;
            array.m_UnsafeList->m_Allocator = allocator;
            array.m_AllocatorLabel = allocator;

#if UNITY_EDITOR
            DisposeSentinel.Create(out array.m_Safety, out array.m_DisposeSentinel, 1, allocator);
#endif
        }

        private void Incremental(uint length)
        {
            long transformShiftSize = s_TransformSize * (m_UnsafeList->m_Length + length);

            var transformBuffer = (ProxyTransformData*)UnsafeUtility.Malloc(transformShiftSize, UnsafeUtility.AlignOf<ProxyTransformData>(), m_AllocatorLabel);

            UnsafeUtility.MemClear(transformBuffer, transformShiftSize);
            UnsafeUtility.MemCpy(transformBuffer, m_UnsafeList->m_TransformBuffer, s_TransformSize * m_UnsafeList->m_Length);

            UnsafeUtility.Free(m_UnsafeList->m_Buffer.ToPointer(), m_AllocatorLabel);

            m_UnsafeList->m_Buffer = (IntPtr)transformBuffer;

            m_UnsafeList->m_Length += length;
            $"increased to {m_UnsafeList->m_Length}".ToLog();
        }
        //private void Decremental(uint length)
        //{
        //    AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);

        //    long transformShiftSize = s_TransformSize * (m_Length - length);

        //    var transformBuffer = (ProxyTransformData*)UnsafeUtility.Malloc(transformShiftSize, UnsafeUtility.AlignOf<ProxyTransformData>(), m_AllocatorLabel);

        //    UnsafeUtility.MemCpy(transformBuffer, m_TransformBuffer, transformShiftSize);

        //    UnsafeUtility.Free(m_TransformBuffer, m_AllocatorLabel);

        //    m_TransformBuffer = transformBuffer;

        //    m_Length -= length;
        //}

        public ProxyTransform Add(PrefabReference prefab, 
            float3 translation, quaternion rotation, float3 scale, bool enableCull,
            float3 center, float3 size, bool gpuInstanced)
        {
#if UNITY_EDITOR
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            int index = -1;
            for (int i = 0; i < m_UnsafeList->m_Length; i++)
            {
                if (!(*m_UnsafeList)[i]->m_IsOccupied)
                {
                    index = i;
                    break;
                }
            }
            if (index < 0)
            {
                uint length = m_UnsafeList->m_Length;
                Incremental(length);

                ProxyTransform result = Add(prefab, translation, rotation, scale, enableCull, center, size, gpuInstanced);
                return result;
            }

            Hash hash = Hash.NewHash();
            ProxyTransformData* targetP = (*m_UnsafeList)[index];
            int generation = targetP->m_Generation;
            if (generation.Equals(int.MaxValue)) generation = 0;
            else generation++;

            ProxyTransformData tr = new ProxyTransformData
            {
                m_IsOccupied = true,

                m_Hash = hash,
                m_Index = index,
                m_Generation = generation,

                m_Prefab = prefab,
                m_ProxyIndex = ProxyTransform.ProxyNull,
                m_EnableCull = enableCull,
                m_IsVisible = false,
                m_DestroyQueued = false,

                m_Translation = translation,
                m_Rotation = rotation,
                m_Scale = scale,

                m_Center = center,
                m_Size = size,

                m_ParentIndex = -1,

                m_GpuInstanced = gpuInstanced
            };
            
            *targetP = tr;

            ProxyTransform transform = new ProxyTransform(m_UnsafeList, index, generation, hash);
            return transform;
        }
        public ProxyTransform GetTransform(int index)
        {
            if (index < 0 || index >= m_UnsafeList->m_Length) throw new ArgumentOutOfRangeException(nameof(index) + $"index of {index} in {m_UnsafeList->m_Length}");

            int gen = m_UnsafeList->m_TransformBuffer[index].m_Generation;
            Hash hash = m_UnsafeList->m_TransformBuffer[index].m_Hash;

            return new ProxyTransform(m_UnsafeList, index, gen, hash);
        }
        public void Remove(ProxyTransform transform)
        {
            //const string c_DestroyMsg = "ProxyTransform({0}) has been destroyed.";

#if UNITY_EDITOR
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            ProxyTransformData* p = (*m_UnsafeList)[transform.m_Index];
            if (!p->m_IsOccupied || !p->m_Generation.Equals(transform.m_Generation))
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
            }

            p->m_IsOccupied = false;
            p->m_DestroyQueued = false;

#if DEBUG
            if (p->m_ParentIndex > 0)
            {
                CoreSystem.Logger.LogError(LogChannel.Proxy,
                    $"Internal proxy error. Parent has not disclosed while child is destroy.");
            }
            if (p->m_ChildIndices.Length > 0)
            {
                CoreSystem.Logger.LogError(LogChannel.Proxy,
                    $"Internal proxy error. Parent that has childs(count: {p->m_ChildIndices.Length}) doesn\'t disclosed.");
            }
#endif
            p->m_ParentIndex = -1;
            p->m_ChildIndices.Clear();

            //CoreSystem.Logger.Log(Channel.Proxy,
            //    string.Format(c_DestroyMsg, p->m_Prefab.GetObjectSetting().m_Name));
        }
        public void Clear()
        {
            UnsafeUtility.MemClear(m_UnsafeList->m_TransformBuffer, m_UnsafeList->m_Length * s_TransformSize);
        }

        public ref ProxyTransformData ElementAt(int i)
        {
            return ref *List[i];
        }

        [Obsolete, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeArray<ProxyTransformData> GetActiveData(Allocator allocator)
        {
            // 이게 망할놈임
            ProxyTransformData[] buffer = new ProxyTransformData[List.m_Length];
            int j = 0;
            for (int i = 0; i < List.m_Length; i++)
            {
                if (!List[i]->m_IsOccupied) continue;

                buffer[j] = *List[i];
                j++;
            }
            ProxyTransformData[] arr = new ProxyTransformData[j];
            Array.Copy(buffer, arr, j);

            NativeArray<ProxyTransformData> temp = new NativeArray<ProxyTransformData>(arr, allocator);
            //UnsafeUtility.MemCpy(temp.GetUnsafePtr(), buffer, s_TransformSize * j);

            return temp;
        }
        [Obsolete] public void For(Action<ProxyTransform> action)
        {
            for (int i = 0; i < m_UnsafeList->m_Length; i++)
            {
                if (!(*m_UnsafeList)[i]->m_IsOccupied) return;
                action.Invoke(new ProxyTransform(m_UnsafeList, i, (*m_UnsafeList)[i]->m_Generation, (*m_UnsafeList)[i]->m_Hash));
            }
        }
    }
}
