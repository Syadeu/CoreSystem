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

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Syadeu.Presentation.Components
{
    public struct EntityComponentBuffer
    {
        private bool m_IsCreated;

        private UnsafeStream m_AddedStream, m_RemovedStream;
        private UnsafeStream.Writer m_CurrentAddWriter, m_CurrentRemoveWriter;

        public bool IsCreated => m_IsCreated;

        public EntityComponentBuffer(int bufferCount)
        {
            m_AddedStream = new UnsafeStream(bufferCount, AllocatorManager.Temp);
            m_RemovedStream = new UnsafeStream(bufferCount, AllocatorManager.Temp);

            m_CurrentAddWriter = m_AddedStream.AsWriter();
            m_CurrentRemoveWriter = m_RemovedStream.AsWriter();

            m_IsCreated = true;
        }
    }
}
