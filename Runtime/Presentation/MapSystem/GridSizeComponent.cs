using Syadeu.Presentation.Components;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;

namespace Syadeu.Presentation.Map
{
    public struct GridSizeComponent : IEntityComponent
    {
        internal PresentationSystemID<GridSystem> m_GridSystem;

        internal FixedList32Bytes<int> m_ObstacleLayers;
        public GridPosition4 positions;

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
        public int[] GetRange(int range, params int[] ignoreLayers)
        {
            GridSystem grid = m_GridSystem.System;

            int[] indices = grid.GetRange(positions[0].index, range, ignoreLayers);
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
        public bool GetPath(int to, List<GridPathTile> path, int maxPathLength)
        {
            return m_GridSystem.System.GetPath(positions[0].index, to, path, maxPathLength);
        }
    }
}
