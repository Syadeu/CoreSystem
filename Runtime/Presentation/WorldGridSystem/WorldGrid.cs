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
using Syadeu.Presentation.Grid.LowLevel;
using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.Grid
{
    [BurstCompatible]
    internal unsafe struct WorldGrid
    {
        internal readonly short m_CheckSum;

        private AABB m_AABB;
        private float m_CellSize;

        public int length
        {
            get
            {
                float3 size = m_AABB.size;
                int
                    xSize = Convert.ToInt32(math.floor(size.x / m_CellSize)),
                    zSize = Convert.ToInt32(math.floor(size.z / m_CellSize));
                return xSize * zSize;
            }
        }
        public AABB aabb { get => m_AABB; set => m_AABB = value; }
        public float cellSize { get => m_CellSize; set => m_CellSize = value; }
        public int3 gridSize
        {
            get
            {
                float3 size = m_AABB.size;
                return new int3(
                    Convert.ToInt32(math.floor(size.x / m_CellSize)),
                    Convert.ToInt32(math.floor(size.y / m_CellSize)),
                    Convert.ToInt32(math.floor(size.z / m_CellSize)));
            }
        }

        internal WorldGrid(AABB aabb, float cellSize)
        {
            this = default(WorldGrid);

            m_CheckSum = CollectionUtility.CreateHashInt16();

            m_AABB = aabb;
            m_CellSize = cellSize;
        }

        #region Index

        public int3 PositionToLocation(in float3 position)
        {
            int3 location;
            unsafe
            {
                BurstGridMathematics.positionToLocation(in m_AABB, in m_CellSize, in position, &location);
            }
            return location;
        }
        public ulong PositionToIndex(in float3 position)
        {
            ulong index;
            unsafe
            {
                BurstGridMathematics.positionToIndex(in m_AABB, in m_CellSize, in position, &index);
            }
            return index;
        }
        public ulong LocationToIndex(in int3 location)
        {
            ulong index;
            unsafe
            {
                BurstGridMathematics.locationToIndex(in location, &index);
            }
            return index;
        }
        public float3 LocationToPosition(in int3 location)
        {
            float3 position;
            unsafe
            {
                BurstGridMathematics.locationToPosition(in m_AABB, in m_CellSize, in location, &position);
            }
            return position;
        }
        public int3 IndexToLocation(in ulong index)
        {
            int3 location;
            unsafe
            {
                BurstGridMathematics.indexToLocation(in index, &location);
            }
            return location;
        }
        public float3 IndexToPosition(in ulong index)
        {
            float3 position;
            unsafe
            {
                BurstGridMathematics.indexToPosition(in m_AABB, in m_CellSize, in index, &position);
            }
            return position;
        }

        public FixedList512Bytes<GridIndex> AABBToIndices(in AABB aabb)
        {
            FixedList4096Bytes<ulong> temp;
            unsafe
            {
                BurstGridMathematics.aabbToIndices(in m_AABB, in m_CellSize, aabb, &temp);
            }
            FixedList512Bytes<GridIndex> list = new FixedList512Bytes<GridIndex>();

            for (int i = 0; i < temp.Length; i++)
            {
                list.Add(new GridIndex(this, temp[i]));
            }

            return list;
        }

        #endregion

        public bool Contains(in ulong index)
        {
            bool result;
            BurstGridMathematics.containIndex(in m_AABB, in m_CellSize, in index, &result);
            return result;
        }
        public bool Contains(in int3 location)
        {
            bool result;
            BurstGridMathematics.containLocation(in m_AABB, in m_CellSize, in location, &result);
            return result;
        }
        public bool Contains(in float3 position)
        {
            return m_AABB.Contains(position);
        }
        public bool Contains(in AABB aabb)
        {
            return m_AABB.Contains(aabb.min) && m_AABB.Contains(aabb.max);
        }
    }

    [BurstCompatible]
    public readonly struct GridIndex : IEquatable<GridIndex>
    {
        private readonly short m_CheckSum;
        private readonly ulong m_Index;

        public ulong Index => m_Index;
        public int3 Location
        {
            get
            {
                int3 location;
                unsafe
                {
                    BurstGridMathematics.indexToLocation(in m_Index, &location);
                }
                return location;
            }
        }

        internal GridIndex(WorldGrid grid, ulong index)
        {
            m_CheckSum = grid.m_CheckSum;
            m_Index = index;
        }
        internal GridIndex(short checkSum, ulong index)
        {
            m_CheckSum = checkSum;
            m_Index = index;
        }

        public bool Equals(GridIndex other) => m_Index.Equals(other.m_Index) && m_CheckSum.Equals(m_CheckSum);
    }
}
