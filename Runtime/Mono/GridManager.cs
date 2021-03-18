using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Syadeu;
using Syadeu.Database;
using System;
using Syadeu.Extensions.Logs;
using System.Linq;

namespace Syadeu.Mono
{
    [DisallowMultipleComponent]
    public class GridManager : StaticManager<GridManager>
    {
        public override bool HideInHierarchy => false;

        [SerializeField] private float m_GridSize = 2.5f;
        [SerializeField] private float m_GridHeight = 0;

        private Grid[] m_Grids = new Grid[0];

#if UNITY_EDITOR
        public static Grid[] m_EditorGrids = new Grid[0];
#endif

        [Serializable]
        public struct Grid : IValidation, IEquatable<Grid>
        {
            private Guid Guid;
            internal int Idx;

            public GridCell[] Cells;
            public float Height;

            public object CustomData;

            internal Grid(int idx, float height, params GridCell[] cells)
            {
                Guid = Guid.NewGuid();
                Idx = idx;

                Cells = cells;
                Height = height;

                CustomData = null;
            }

            public bool IsValid() => Cells != null;
            public bool Equals(Grid other) => Guid.Equals(other.Guid);

            public ref GridCell SetCustomData(int i, object customData)
            {
                ref GridCell cell = ref Cells[i];

                cell.CustomData = customData;
                return ref Cells[i];
            }
        }
        [Serializable]
        public struct GridCell : IValidation, IEquatable<GridCell>
        {
            public Vector2Int Grid;
            public Bounds Bounds;

            public object CustomData;

            private readonly Vector3[] Verties;

            internal GridCell(Vector2Int grid, Bounds bounds)
            {
                Grid = grid;
                Bounds = bounds;

                CustomData = null;

                Verties = new Vector3[4]
                {
                bounds.min,
                new Vector3(bounds.min.x, bounds.min.y, bounds.max.z),
                new Vector3(bounds.max.x, bounds.min.y, bounds.max.z),
                new Vector3(bounds.max.x, bounds.min.y, bounds.min.z)
                };
            }

            public bool IsValid() => Verties != null;
            public bool Equals(GridCell other) => Grid.Equals(other.Grid);
            public bool IsVisable()
            {
                for (int i = 0; i < Verties.Length; i++)
                {
                    if (IsInScreen(in Verties[i])) return true;
                }
                return false;
            }
        }

        public static ref Grid GetGrid(in int idx) => ref Instance.m_Grids[idx];
        public static int CreateGrid(in Bounds bounds, in float gridSize)
        {
            List<Grid> newGrids;
            Grid grid;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                newGrids = new List<Grid>(m_EditorGrids);
                grid = InternalCreateGrid(newGrids.Count, bounds, gridSize);

                newGrids.Add(grid);

                m_EditorGrids = newGrids.ToArray();

                return grid.Idx;
            }
            else
#endif
            {
                newGrids = new List<Grid>(Instance.m_Grids);
                grid = InternalCreateGrid(newGrids.Count, bounds, gridSize);

                newGrids.Add(grid);

                Instance.m_Grids = newGrids.ToArray();

                return grid.Idx;
            }
        }
        public static void UpdateGrid(in int idx, in Bounds bounds, in float gridSize)
        {
            Grid newGrid = InternalCreateGrid(idx, bounds, gridSize);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                m_EditorGrids[idx] = newGrid;
            }
            else
#endif
            {
                Instance.m_Grids[idx] = newGrid;
            }
        }
        public static ref Grid SetCustomData(int idx, object customData)
        {
            ref Grid grid = ref GetGrid(idx);

            grid.CustomData = customData;
            return ref Instance.m_Grids[idx];
        }

        public static byte[] ExportGrids() => Instance.m_Grids.ToBytesWithStream();
        public static void ImportGrids(in byte[] bytes) => Instance.m_Grids = bytes.ToObjectWithStream<Grid[]>();

        private static bool IsInScreen(in Vector3 screenPos)
        {
            if (screenPos.y < 0 || screenPos.y > Screen.height ||
                screenPos.x < 0 || screenPos.x > Screen.width || screenPos.z < 0)
            {
                return false;
            }
            return true;
        }
        private static Grid InternalCreateGrid(int idx, Bounds bounds, float gridSize)
        {
            int xSize = Mathf.FloorToInt(bounds.size.x / gridSize);
            int zSize = Mathf.FloorToInt(bounds.size.z / gridSize);

            float halfSize = gridSize / 2;
            Vector3 cellSize = new Vector3(gridSize, .5f, gridSize);

            int count = 0;
            GridCell[] cells = new GridCell[xSize * zSize];
            for (int i = 0; i < zSize; i++)
            {
                for (int j = 0; j < xSize; j++)
                {
                    Vector3 center = new Vector3(
                        bounds.min.x + halfSize + (gridSize * j), 0,
                        bounds.max.z - halfSize - (gridSize * i));

                    cells[count] = new GridCell(new Vector2Int(j, i), new Bounds(center, cellSize));
                    count++;
                }
            }

            return new Grid(idx, bounds.size.y, cells);
        }
    }
}
