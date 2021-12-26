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

        private bool m_IsCreated;

        private UnsafeStream m_Stream;
        private UnsafeStream.Writer m_CurrentWriter;

        public bool IsCreated => m_IsCreated;

        internal EntityComponentBuffer(int bufferCount)
        {
            m_Stream = new UnsafeStream(bufferCount, AllocatorManager.Temp);

            m_CurrentWriter = m_Stream.AsWriter();

            m_CurrentWriter.BeginForEachIndex(0);

            m_IsCreated = true;
        }
        internal void EndOfWriting()
        {
            m_CurrentWriter.EndForEachIndex();
        }

        public void Add<T>(in InstanceID entity, ref T data) where T : unmanaged, IEntityComponent
        {
            m_CurrentWriter.Write((int)BinaryType.Add);
            m_CurrentWriter.Write(entity);
            m_CurrentWriter.Write(data);
        }
        public void Remove(in InstanceID entity)
        {
            m_CurrentWriter.Write((int)BinaryType.Remove);
            m_CurrentWriter.Write(entity);
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
