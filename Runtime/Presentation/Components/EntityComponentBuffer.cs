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

using Syadeu.Collections;
using Syadeu.Collections.Threading;
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Syadeu.Presentation.Components
{
    public struct EntityComponentBuffer : IDisposable
    {
        internal enum BinaryType
        {
            None = 0,

            Add = 0b0001,
            Remove = 0b0010
        }
        public struct Writer : IDisposable
        {
            internal UnsafeStream.Writer m_Writer;
            internal AtomicSafeInteger m_Count;
            private bool m_Disposed;

            public void Dispose()
            {
                m_Disposed = true;
            }
        }

        private bool m_IsCreated;

        private UnsafeStream m_Stream;
        private AtomicSafeInteger m_Count;

        public bool IsCreated => m_IsCreated;

        internal EntityComponentBuffer(int bufferCount)
        {
            m_Stream = new UnsafeStream(bufferCount, AllocatorManager.Temp);
            m_Count = 0;

            m_IsCreated = true;
        }

        public Writer Begin()
        {
            Writer wr = new Writer
            {
                m_Writer = m_Stream.AsWriter(),
                m_Count = 0
            };
            wr.m_Writer.BeginForEachIndex(m_Count.Value);

            return wr;
        }
        public void Add<T>(ref Writer wr, in InstanceID entity, ref T data) where T : unmanaged, IEntityComponent
        {
            wr.m_Writer.Write((int)BinaryType.Add);
            wr.m_Writer.Write(entity);
            wr.m_Writer.Write(data);

            wr.m_Count.Value += 3;
        }
        public void Remove(ref Writer wr, in InstanceID entity)
        {
            wr.m_Writer.Write((int)BinaryType.Remove);
            wr.m_Writer.Write(entity);

            wr.m_Count.Value += 2;
        }
        public void End(ref Writer wr)
        {
            wr.m_Writer.EndForEachIndex();
            m_Count.Value += wr.m_Count.Value;

            wr.Dispose();
        }

        internal bool TryReadAdded(out UnsafeStream.Reader rdr)
        {
            rdr = m_Stream.AsReader();
            if (rdr.Count() == 0) return false;

            return true;
        }

        public void Dispose()
        {
            m_Stream.Dispose();

            m_IsCreated = false;
            "ecb disposed".ToLog();
        }
    }
}
