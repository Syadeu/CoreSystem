using Syadeu.Database;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;

namespace Syadeu.Presentation.Map
{
    public struct GridSizeComponent : IEntityComponent, IValidation
    {
        internal PresentationSystemID<GridSystem> m_GridSystem;
        internal EntityData<IEntityData> m_Parent;

        internal FixedList128Bytes<int> m_ObstacleLayers;
        public FixedList512Bytes<GridPosition> positions;

        public float CellSize => m_GridSystem.System.CellSize;

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

        public FixedList32Bytes<int> GetRange8(in int range)
        {
            GridSystem grid = m_GridSystem.System;

            FixedList32Bytes<int> indices = grid.GetRange8(positions[0].index, in range, m_ObstacleLayers);
            return indices;
        }
        public FixedList64Bytes<int> GetRange16(in int range)
        {
            GridSystem grid = m_GridSystem.System;

            FixedList64Bytes<int> indices = grid.GetRange16(positions[0].index, in range, m_ObstacleLayers);
            return indices;
        }
        public FixedList128Bytes<int> GetRange32(in int range)
        {
            GridSystem grid = m_GridSystem.System;

            FixedList128Bytes<int> indices = grid.GetRange32(positions[0].index, in range, m_ObstacleLayers);
            return indices;
        }
        public FixedList4096Bytes<int> GetRange1024(in int range)
        {
            GridSystem grid = m_GridSystem.System;

            FixedList4096Bytes<int> indices = grid.GetRange1024(positions[0].index, in range, m_ObstacleLayers);
            return indices;
        }
        public void GetRange(ref NativeList<int> list, in int range)
        {
            GridSystem grid = m_GridSystem.System;
            grid.GetRange(ref list, positions[0].index, in range, m_ObstacleLayers);
        }

        public bool HasPath(int to, int maxPathLength)
        {
            return m_GridSystem.System.HasPath(positions[0].index, to, maxPathLength, out _);
        }
        public bool HasPath(int to, int maxPathLength, out int pathCount)
        {
            return m_GridSystem.System.HasPath(positions[0].index, to, maxPathLength, out pathCount);
        }

        //[Obsolete]
        //public bool GetPath(int to, List<GridPathTile> path, int maxPathLength)
        //{
        //    return m_GridSystem.System.GetPath(positions[0].index, to, path, maxPathLength);
        //}

        public bool GetPath64(in int to, ref GridPath32 path, in int maxPathLength, in int maxIteration = 32)
        {
            return m_GridSystem.System.GetPath64(
                positions[0].index, in to, in maxPathLength, ref path, 
                m_Parent.GetAttribute<GridSizeAttribute>().ObstacleLayers, in maxIteration);
        }

        public GridPosition GetGridPosition(in int index)
        {
            return new GridPosition(index, m_GridSystem.System.IndexToLocation(index));
        }

        public bool IsValid() => !m_GridSystem.IsNull() && m_GridSystem.IsValid();
    }
}
