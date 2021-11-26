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

using System.Text;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IO.LowLevel.Unsafe;

namespace Syadeu.Collections.IO
{
    public struct NativeFileReader : IValidation
    {
        private ReadHandle m_Handle;
        [NativeDisableUnsafePtrRestriction] private unsafe byte* m_Data;
        private int m_Size;

        public bool IsReadable
        {
            get
            {
                return m_Handle.Status != ReadStatus.InProgress;
            }
        }

        internal unsafe NativeFileReader(ReadHandle handle, byte* data, int size)
        {
            m_Handle = handle;
            m_Data = data;
            m_Size = size;
        }

        [BurstDiscard]
        public string ToString(Encoding encoding)
        {
            unsafe
            {
                return encoding.GetString(m_Data, m_Size);
            }
        }
        //public T Read<T>()
        //{
        //    Encoding.UTF8

        //    MemoryStream memStream = new MemoryStream();
        //    BinaryFormatter binForm = new BinaryFormatter();
        //    memStream.Write(arrBytes, 0, arrBytes.Length);
        //    memStream.Seek(0, SeekOrigin.Begin);
        //    Object obj = (Object)binForm.Deserialize(memStream);
        //}

        public bool IsValid()
        {
            unsafe
            {
                if (m_Data == null) return false;
            }

            return true;
        }
    }
}
