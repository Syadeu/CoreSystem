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

using Syadeu.Collections.Buffer.LowLevel;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Syadeu.Collections.Buffer
{
    public struct FixedReference<T> where T : unmanaged
    {
        private UnsafeReference<T> m_Ptr;

        public bool IsCreated => m_Ptr.IsCreated;

        public FixedReference(NativeArray<T> array, int elementIndex)
        {
            unsafe
            {
                T* buffer = (T*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(array);

                m_Ptr = new UnsafeReference<T>(buffer + elementIndex);
            }
        }
    }
}
