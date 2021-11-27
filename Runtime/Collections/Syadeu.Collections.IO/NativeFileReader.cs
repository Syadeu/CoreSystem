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

using Unity.Collections.LowLevel.Unsafe;
using Unity.IO.LowLevel.Unsafe;

namespace Syadeu.Collections.IO
{
    public struct NativeFileReader : INativeReader, IValidation
    {
        private ReadHandle m_Handle;
        [NativeDisableUnsafePtrRestriction] private unsafe byte* m_Data;
        private long m_Size;

        public unsafe byte* Data => m_Data;
        public long Size => m_Size;

        public bool IsReadable
        {
            get
            {
                return m_Handle.Status != ReadStatus.InProgress;
            }
        }

        public void Initialize(ReadHandle handle, ReadCommand cmd)
        {
            m_Handle = handle;
            unsafe
            {
                m_Data = (byte*)cmd.Buffer;
            }
            m_Size = cmd.Size;
        }

        public bool IsValid()
        {
            unsafe
            {
                if (m_Data == null) return false;
            }

            return true;
        }
    }
    public interface INativeReader : IValidation
    {
        unsafe byte* Data { get; }
        long Size { get; }

        bool IsReadable { get; }

        void Initialize(ReadHandle handle, ReadCommand cmd);
    }
}
