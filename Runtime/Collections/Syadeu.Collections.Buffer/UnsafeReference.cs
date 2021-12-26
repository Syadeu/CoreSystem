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

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Syadeu.Collections.Buffer.LowLevel
{
    [BurstCompatible]
    public struct UnsafeReference
    {
        private bool m_IsCreated;
        [NativeDisableUnsafePtrRestriction]
        private unsafe void* m_Ptr;

        public unsafe void* Ptr => m_Ptr;
        public IntPtr IntPtr { get { unsafe { return (IntPtr)m_Ptr; } } }

        public bool IsCreated => m_IsCreated;

        public UnsafeReference(IntPtr ptr)
        {
            unsafe
            {
                m_Ptr = ptr.ToPointer();
            }
            m_IsCreated = true;
        }
    }
    [BurstCompatible]
    public struct UnsafeReference<T> where T : unmanaged
    {
        private bool m_IsCreated;
        [NativeDisableUnsafePtrRestriction]
        private unsafe T* m_Ptr;

        public unsafe T* Ptr => m_Ptr;
        public IntPtr IntPtr { get { unsafe { return (IntPtr)m_Ptr; } } }
        public ref T Value { get { unsafe { return ref *m_Ptr; } } }

        public bool IsCreated => m_IsCreated;

        public UnsafeReference(IntPtr ptr)
        {
            unsafe
            {
                m_Ptr = (T*)ptr.ToPointer();
            }
            m_IsCreated = true;
        }
    }
}
