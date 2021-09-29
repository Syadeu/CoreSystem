using Syadeu.Presentation.Components;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;

namespace Syadeu.Presentation.Map
{
    public struct GridSizeComponent : IEntityComponent
    {
        internal PresentationSystemID<GridSystem> m_GridSystem;

        internal FixedList32Bytes<int> m_ObstacleLayers;
        public FixedList32Bytes<GridPosition> positions;

        public float3 IndexToPosition(in int index)
        {
            return m_GridSystem.System.IndexToPosition(index);
        }

        public bool IsInIndex(int index)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                if (positions[i].index == index) return true;
            }
            return false;
        }

        [Obsolete("Use GetRange Instead")]
        public int[] GetRange(int range, params int[] ignoreLayers)
        {
            GridSystem grid = m_GridSystem.System;

            int[] indices = grid.GetRange(positions[0].index, range, ignoreLayers);
            return indices;
        }

        public FixedList128Bytes<int> GetRange128(in int range)
        {
            GridSystem grid = m_GridSystem.System;

            FixedList128Bytes<int> indices = grid.GetRange128(positions[0].index, in range, m_ObstacleLayers);
            return indices;
        }
        public FixedList64Bytes<int> GetRange64(in int range)
        {
            GridSystem grid = m_GridSystem.System;

            FixedList64Bytes<int> indices = grid.GetRange64(positions[0].index, in range, m_ObstacleLayers);
            return indices;
        }
        public FixedList32Bytes<int> GetRange32(in int range)
        {
            GridSystem grid = m_GridSystem.System;

            FixedList32Bytes<int> indices = grid.GetRange32(positions[0].index, in range, m_ObstacleLayers);
            return indices;
        }

        public bool HasPath(int to, int maxPathLength)
        {
            return m_GridSystem.System.HasPath(positions[0].index, to, maxPathLength, out _);
        }
        public bool HasPath(int to, int maxPathLength, out int pathCount)
        {
            return m_GridSystem.System.HasPath(positions[0].index, to, maxPathLength, out pathCount);
        }

        [Obsolete]
        public bool GetPath(int to, List<GridPathTile> path, int maxPathLength)
        {
            return m_GridSystem.System.GetPath(positions[0].index, to, path, maxPathLength);
        }

        public bool GetPath64(in int to, ref FixedList64Bytes<GridPathTile> path, in int maxPathLength, in int maxIteration = 32)
        {
            return m_GridSystem.System.GetPath64(positions[0].index, in to, in maxPathLength, ref path, in maxIteration);
        }
    }
}
