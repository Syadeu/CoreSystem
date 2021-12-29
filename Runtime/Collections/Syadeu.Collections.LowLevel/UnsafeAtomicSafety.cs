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

namespace Syadeu.Collections.LowLevel
{
#if ENABLE_UNITY_COLLECTIONS_CHECKS
    /// <summary>
    /// ENABLE_UNITY_COLLECTIONS_CHECKS 가 정의되었을때 컴파일됩니다.
    /// </summary>
    public class UnsafeAtomicSafety : IDisposable
    {
        private AtomicSafetyHandle m_SafetyHandle;
        private DisposeSentinel m_DisposeSentinel;

        private bool m_Disposed;

        public bool Disposed => m_Disposed;

        public UnsafeAtomicSafety(int callSiteStackDepth, Allocator allocator)
        {
            DisposeSentinel.Create(out m_SafetyHandle, out m_DisposeSentinel, callSiteStackDepth, allocator);
        }

        public void Dispose()
        {
            if (m_Disposed)
            {
                throw new Exception();
            }

            DisposeSentinel.Dispose(ref m_SafetyHandle, ref m_DisposeSentinel);
            m_Disposed = true;
        }
    }
#endif
}
