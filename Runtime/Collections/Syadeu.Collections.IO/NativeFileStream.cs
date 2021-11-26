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

using System;
using System.IO;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IO.LowLevel.Unsafe;

namespace Syadeu.Collections.IO
{
    public struct NativeFileStream : IValidation, IDisposable
    {
        [NativeDisableUnsafePtrRestriction] private unsafe ReadCommand* m_Command;
        private ReadHandle m_Handle;

        [BurstDiscard]
        public NativeFileReader ReadAsync(string path)
        {
            unsafe
            {
                m_Command = (ReadCommand*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<ReadCommand>(), UnsafeUtility.AlignOf<ReadCommand>(), Allocator.Persistent);

                m_Command->Offset = 0;
                m_Command->Size = new FileInfo(path).Length;
                m_Command->Buffer = (byte*)UnsafeUtility.Malloc(m_Command->Size, 16, Allocator.Persistent);

                m_Handle = AsyncReadManager.Read(path, m_Command, 1);

                return new NativeFileReader(m_Handle, (byte*)m_Command->Buffer, (int)m_Command->Size);
            }
        }
        [BurstDiscard]
        public void Close()
        {
#if DEBUG_MODE
            if (!IsValid())
            {
                UnityEngine.Debug.LogError("Collection is not valid.");
                return;
            }
#endif
            unsafe
            {
                if (m_Command->Buffer != null)
                {
                    m_Handle.Dispose();
                    UnsafeUtility.Free(m_Command->Buffer, Allocator.Persistent);
                }

                UnsafeUtility.Free(m_Command, Allocator.Persistent);
            }
        }
        void IDisposable.Dispose()
        {
#if DEBUG_MODE
            if (!IsValid())
            {
                UnityEngine.Debug.LogError("Collection is not valid.");
                return;
            }
#endif
            unsafe
            {
                if (m_Command->Buffer != null)
                {
                    m_Handle.Dispose();
                    UnsafeUtility.Free(m_Command->Buffer, Allocator.Persistent);
                }
                
                UnsafeUtility.Free(m_Command, Allocator.Persistent);
            }
        }

        public bool IsValid()
        {
            unsafe
            {
                if (m_Command == null) return false;
            }

            return true;
        }
    }
}
