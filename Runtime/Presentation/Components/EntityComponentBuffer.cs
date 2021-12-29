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
using Syadeu.Collections.Threading;
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Syadeu.Presentation.Components
{
    [BurstCompatible]
    public struct EntityComponentBuffer : IDisposable
    {
        internal enum BinaryType
        {
            None = 0,

            Add = 0b0001,
            Remove = 0b0010
        }
        [BurstCompatible]
        public struct Writer : IDisposable
        {
            internal UnsafeStream.Writer m_Writer;
            internal AtomicSafeInteger m_Count;
            internal Hash m_CheckSum;

            private bool m_Disposed;

            public bool Diposed => m_Disposed;
            public int Count => m_Count.Value;

            public void Dispose()
            {
                m_Disposed = true;
            }
        }
        public struct Reader
        {
            internal UnsafeStream.Reader m_Reader;
        }

        private bool m_IsCreated;

        private UnsafeStream m_Stream;
        private AtomicSafeInteger m_Count;
        private AtomicSafeReference<Hash> m_CheckSum;

        public bool IsCreated => m_IsCreated;

        internal EntityComponentBuffer(int bufferCount)
        {
            m_Stream = new UnsafeStream(bufferCount, AllocatorManager.Persistent);
            m_Count = 0;
            m_CheckSum = new AtomicSafeReference<Hash>(0);

            m_IsCreated = true;
        }

        public Writer Begin()
        {
#if DEBUG_MODE
            if (m_CheckSum.Value != 0)
            {
                CoreSystem.Logger.LogError(Channel.Component,
                    $"This ECB has already opened writer. ECB cannot have more than 2 writers at the same time. " +
                    $"This is not allowed.");

                throw new InvalidOperationException("Component buffer error. See error log.");
            }
#endif
            Writer wr = new Writer
            {
                m_Writer = m_Stream.AsWriter(),
                m_Count = 0,
                m_CheckSum = Hash.NewHash()
            };
            m_CheckSum.Value ^= wr.m_CheckSum;

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
            m_CheckSum.Value ^= wr.m_CheckSum;

            wr.Dispose();
        }

        internal bool TryReadAdded(out UnsafeStream.Reader rdr)
        {
            if (!m_Stream.IsCreated)
            {
                rdr = default(UnsafeStream.Reader);
                "?? err".ToLog();
                return false;
            }

            rdr = m_Stream.AsReader();

            return true;
        }

        public void Dispose()
        {
#if DEBUG_MODE
            if (m_CheckSum.Value != 0)
            {
                CoreSystem.Logger.LogError(Channel.Component,
                    $"This ECB has un-finshed writing job. This is not allowed.");
            }
#endif
            m_Stream.Dispose();

            m_IsCreated = false;
        }
    }
}
