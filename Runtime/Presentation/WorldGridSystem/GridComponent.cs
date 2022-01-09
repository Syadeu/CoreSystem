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
using Unity.Collections;
using Unity.Mathematics;

namespace Syadeu.Presentation.Grid
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// 위치가 바뀐 경우 <seealso cref="OnGridLocationChangedEvent"/> 이벤트를 발생시킵니다.
    /// </remarks>
    public struct GridComponent : IEntityComponent
    {
        public enum Obstacle
        {
            None,

            Block
        }

        internal FixedList512Bytes<GridIndex> m_Indices;
        private int3 m_FixedSize;
        private Alignment m_SizeAlignment;
        private Obstacle m_ObstacleType;

        public FixedList512Bytes<GridIndex> Indices => m_Indices;
        public int3 FixedSize { get => m_FixedSize; set => m_FixedSize = value; }
        public Alignment SizeAlignment { get => m_SizeAlignment; set => m_SizeAlignment = value; }
        public Obstacle ObstacleType { get => m_ObstacleType; set => m_ObstacleType = value; }

        public bool IsMyIndex(GridIndex index)
        {
            for (int i = 0; i < m_Indices.Length; i++)
            {
                if (m_Indices[i].Equals(index)) return true;
            }
            return false;
        }
    }
}
