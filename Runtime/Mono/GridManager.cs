using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Syadeu;
using Syadeu.Database;
using System;
using Syadeu.Extensions.Logs;
using System.Linq;
using UnityEngine.AI;
using Unity.Mathematics;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Syadeu.Mono
{
    [DisallowMultipleComponent]
    public class GridManager : StaticManager<GridManager>
    {
        public override bool HideInHierarchy => false;

        [SerializeField] private float m_CellSize = 2.5f;
        [SerializeField] private float m_GridHeight = 0;

        private Grid[] m_Grids = new Grid[0];

#if UNITY_EDITOR
        public static Grid[] s_EditorGrids = new Grid[0];
#endif

        [Serializable]
        public struct BinaryWrapper
        {
            public byte[] m_Grid;
            public byte[] m_GridCells;

            internal BinaryWrapper(byte[] grid, byte[] cells)
            {
                m_Grid = grid;
                m_GridCells = cells;
            }

            public Grid ToGrid() => new Grid(in this);
        }
        [Serializable]
        internal struct BinaryGrid
        {
            public Guid Guid;
            public int Idx;

            public int2 GridSize;
            public float CellSize;
            public float Height;

            public string CustomDataType;
            public string CustomData;

            public bool EnableNavMesh;

            public BinaryGrid(in Grid grid)
            {
                Guid = grid.Guid;
                Idx = grid.Idx;

                GridSize = grid.GridSize;
                CellSize = grid.CellSize;
                Height = grid.Height;

                if (grid.CustomData != null)
                {
                    Type t = grid.CustomData.GetType();
                    CustomDataType = t.Name;
                    CustomData = JsonConvert.SerializeObject(grid.CustomData, t, Formatting.None, null);
                }
                else
                {
                    CustomDataType = null;
                    CustomData = null;
                }

                EnableNavMesh = grid.EnableNavMesh;
            }
        }
        [Serializable]
        internal struct BinaryGridCell
        {
            public int ParentIdx;

            public int2 Location;
            public float3 Bounds_Center;
            public float3 Bounds_Size;

            public string CustomDataType;
            public string CustomData;

            public BinaryGridCell(in GridCell gridCell)
            {
                ParentIdx = gridCell.ParentIdx;

                Location = gridCell.Location;
                Bounds_Center = gridCell.Bounds.center;
                Bounds_Size = gridCell.Bounds.size;

                if (gridCell.CustomData != null)
                {
                    Type t = gridCell.CustomData.GetType();
                    CustomDataType = t.Name;
                    CustomData = JsonConvert.SerializeObject(gridCell.CustomData, t, Formatting.None, null);
                }
                else
                {
                    CustomDataType = null;
                    CustomData = null;
                }
            }
        }

        [Serializable]
        public struct Grid : IValidation, IEquatable<Grid>
        {
            internal Guid Guid;
            internal int Idx;

            internal int2 GridSize;
            internal GridCell[] Cells;
            public float CellSize;
            public float Height;

            public object CustomData;

            public readonly bool EnableNavMesh;

            public int Length => Cells.Length;

            internal Grid(int idx, int2 gridSize, float cellSize, float height, bool enableNavMesh, params GridCell[] cells)
            {
                Guid = Guid.NewGuid();
                Idx = idx;

                GridSize = gridSize;
                Cells = cells;
                CellSize = cellSize;
                Height = height;

                CustomData = null;

                EnableNavMesh = enableNavMesh;
            }
            internal Grid(in BinaryGrid grid, in BinaryGridCell[] cells)
            {
                Guid = grid.Guid;
                Idx = grid.Idx;

                GridSize = grid.GridSize;
                GridCell[] convertedCells = new GridCell[cells.Length];
                for (int i = 0; i < cells.Length; i++)
                {
                    convertedCells[i] = new GridCell(in cells[i], grid.EnableNavMesh);
                }
                Cells = convertedCells;
                CellSize = grid.CellSize;
                Height = grid.Height;

                CustomData = JsonConvert.DeserializeObject(grid.CustomData);

                EnableNavMesh = grid.EnableNavMesh;
            }
            internal Grid(in BinaryWrapper wrapper)
            {
                BinaryGrid grid;
                BinaryGridCell[] cells;

                grid = wrapper.m_Grid.ToObjectWithStream<BinaryGrid>();
                cells = wrapper.m_GridCells.ToObjectWithStream<BinaryGridCell[]>();
                //cells = new BinaryGridCell[wrapper.m_GridCells.Length];
                //for (int i = 0; i < cells.Length; i++)
                //{
                //    cells[i] = wrapper.m_GridCells[i].ToObjectWithStream<BinaryGridCell[]>();
                //}

                Guid = grid.Guid;
                Idx = grid.Idx;

                GridSize = grid.GridSize;
                GridCell[] convertedCells = new GridCell[cells.Length];
                for (int i = 0; i < cells.Length; i++)
                {
                    convertedCells[i] = new GridCell(in cells[i], grid.EnableNavMesh);
                }
                Cells = convertedCells;
                CellSize = grid.CellSize;
                Height = grid.Height;

                if (!string.IsNullOrEmpty(grid.CustomData))
                {
                    CustomData = JsonConvert.DeserializeObject(grid.CustomData, Type.GetType(grid.CustomDataType));
                }
                else CustomData = null;

                EnableNavMesh = grid.EnableNavMesh;
            }

            public bool IsValid() => Cells != null;
            public bool Equals(Grid other) => Guid.Equals(other.Guid);
            
            public bool Contains(Vector2Int grid)
            {
                for (int i = 0; i < Cells.Length; i++)
                {
                    if (Cells[i].Location.Equals(grid)) return true;
                }
                return false;
            }

            public ref GridCell GetCell(int idx) => ref Cells[idx];
            public ref GridCell GetCell(Vector2Int grid)
            {
                for (int i = 0; i < Cells.Length; i++)
                {
                    if (Cells[i].Location.Equals(grid)) return ref Cells[i];
                }

                throw new CoreSystemException(CoreSystemExceptionFlag.Mono, $"Out of Range({grid.x},{grid.y}). " +
                    $"해당 좌표계는 이 그리드에 존재하지않습니다.");
            }
            public ref GridCell GetCell(Vector3 worldPosistion)
            {
                for (int i = 0; i < Cells.Length; i++)
                {
                    if (Cells[i].Bounds.Contains(worldPosistion)) return ref Cells[i];
                }

                throw new CoreSystemException(CoreSystemExceptionFlag.Mono, $"Out of Range({worldPosistion.x},{worldPosistion.y},{worldPosistion.z}). " +
                    $"해당 좌표계는 이 그리드에 존재하지않습니다.");
            }

            public object GetCustomData() => CustomData;
            public T GetCustomData<T>() => (T)CustomData;

            public BinaryWrapper Convert()
            {
                byte[] binaryGrid;
                byte[] binaryCells;

                BinaryGrid grid = new BinaryGrid(this);
                binaryGrid = grid.ToBytesWithStream();

                var temp = new BinaryGridCell[Cells.Length];
                for (int i = 0; i < Cells.Length; i++)
                {
                    temp[i] = new BinaryGridCell(in Cells[i]);
                    //binaryCells[i] = cell.ToBytesWithStream();
                }
                binaryCells = temp.ToBytesWithStream();
                return new BinaryWrapper(binaryGrid, binaryCells);
            }
            public static Grid FromBytes(BinaryWrapper wrapper) => new Grid(wrapper);
        }
        [Serializable]
        public struct GridCell : IValidation, IEquatable<GridCell>
        {
            public readonly int ParentIdx;

            public int2 Location;
            public Bounds Bounds;

            public object CustomData;

            private readonly float3[] Verties;
            private readonly float3[] NavMeshVerties;

            // NavMesh
            public bool BlockedByNavMesh
            {
                get
                {
                    ref Grid parent = ref GetGrid(in ParentIdx);

                    if (!parent.EnableNavMesh) return false;
                    for (int i = 0; i < NavMeshVerties.Length; i++)
                    {
                        NavMesh.SamplePosition(NavMeshVerties[i], out NavMeshHit hit, .5f, -1);
                        if (!hit.hit) return true;
                    }
                    return false;
                }
            }

            internal GridCell(int parentIdx, int2 location, Bounds bounds, bool enableNavMesh)
            {
                ParentIdx = parentIdx;

                Location = location;
                Bounds = bounds;

                CustomData = null;

                Verties = new float3[4]
                {
                new float3(bounds.min.x, bounds.min.y, bounds.min.z),
                new float3(bounds.min.x, bounds.min.y, bounds.max.z),
                new float3(bounds.max.x, bounds.min.y, bounds.max.z),
                new float3(bounds.max.x, bounds.min.y, bounds.min.z)
                };

                //ref Grid parent = ref GetGrid(parentIdx);
                if (enableNavMesh)
                {
                    NavMeshVerties = new float3[]
                    {
                        bounds.center,
                        new float3(Bounds.center.x + Bounds.extents.x - .1f, Bounds.center.y, Bounds.center.z),
                        new float3(Bounds.center.x - Bounds.extents.x + .1f, Bounds.center.y, Bounds.center.z),
                        new float3(Bounds.center.x, Bounds.center.y, Bounds.center.z + Bounds.extents.z - .1f),
                        new float3(Bounds.center.x, Bounds.center.y, Bounds.center.z - Bounds.extents.z + .1f)
                    };
                }
                else NavMeshVerties = null;
            }
            internal GridCell(in BinaryGridCell cell, bool enableNavMesh) : this(cell.ParentIdx, cell.Location, new Bounds(cell.Bounds_Center, cell.Bounds_Size), enableNavMesh)
            {
                if (!string.IsNullOrEmpty(cell.CustomData))
                {
                    CustomData = JsonConvert.DeserializeObject(cell.CustomData, Type.GetType(cell.CustomDataType));
                }
                else CustomData = null;
            }

            public bool IsValid() => Verties != null;
            public bool Equals(GridCell other) => Location.Equals(other.Location);
            public bool IsVisable()
            {
                for (int i = 0; i < Verties.Length; i++)
                {
                    if (IsInScreen(in Verties[i])) return true;
                }
                return false;
            }

            public object GetCustomData() => CustomData;
            public T GetCustomData<T>() => (T)CustomData;
        }

        public static ref Grid GetGrid(in int idx)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                for (int i = 0; i < s_EditorGrids.Length; i++)
                {
                    if (s_EditorGrids[i].Idx.Equals(idx)) return ref s_EditorGrids[i];
                }
            }
            else
#endif
            {
                for (int i = 0; i < Instance.m_Grids.Length; i++)
                {
                    if (Instance.m_Grids[i].Idx.Equals(idx)) return ref Instance.m_Grids[i];
                }
            }

            throw new CoreSystemException(CoreSystemExceptionFlag.Mono, $"인덱스 ({idx}) 그리드를 찾을 수 없음");
        }
        public static int CreateGrid(in Bounds bounds, in float gridCellSize, in bool enableNavMesh)
        {
            List<Grid> newGrids;
            Grid grid;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                newGrids = new List<Grid>(s_EditorGrids);
                grid = InternalCreateGrid(newGrids.Count, in bounds, in gridCellSize, in enableNavMesh);

                newGrids.Add(grid);

                s_EditorGrids = newGrids.ToArray();
            }
            else
#endif
            {
                newGrids = new List<Grid>(Instance.m_Grids);
                grid = InternalCreateGrid(newGrids.Count, in bounds, in gridCellSize, in enableNavMesh);

                newGrids.Add(grid);

                Instance.m_Grids = newGrids.ToArray();
            }
            return grid.Idx;
        }
        public static int CreateGrid(in Mesh mesh, in float gridCellSize, in bool enableNavMesh)
        {
            List<Grid> newGrids;
            Grid grid;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                newGrids = new List<Grid>(s_EditorGrids);
                grid = InternalCreateGrid(newGrids.Count, mesh.bounds, in gridCellSize, in enableNavMesh);

                newGrids.Add(grid);

                s_EditorGrids = newGrids.ToArray();
            }
            else
#endif
            {
                newGrids = new List<Grid>(Instance.m_Grids);
                grid = InternalCreateGrid(newGrids.Count, mesh.bounds, in gridCellSize, in enableNavMesh);

                newGrids.Add(grid);

                Instance.m_Grids = newGrids.ToArray();
            }
            return grid.Idx;
        }
        public static int CreateGrid(in Terrain terrain, in float gridCellSize, in bool enableNavMesh)
        {
            List<Grid> newGrids;
            Grid grid;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                newGrids = new List<Grid>(s_EditorGrids);
                grid = InternalCreateGrid(newGrids.Count, terrain.terrainData.bounds, in gridCellSize, in enableNavMesh);

                newGrids.Add(grid);

                s_EditorGrids = newGrids.ToArray();
            }
            else
#endif
            {
                newGrids = new List<Grid>(Instance.m_Grids);
                grid = InternalCreateGrid(newGrids.Count, terrain.terrainData.bounds, in gridCellSize, in enableNavMesh);

                newGrids.Add(grid);

                Instance.m_Grids = newGrids.ToArray();
            }
            return grid.Idx;
        }
        public static void UpdateGrid(in int idx, in Bounds bounds, in float gridCellSize, in bool enableNavMesh)
        {
            Grid newGrid = InternalCreateGrid(in idx, in bounds, in gridCellSize, in enableNavMesh);
            ref Grid target = ref GetGrid(in idx);

            target = newGrid;
        }

        public static ref Grid SetCustomData(int idx, object customData)
        {
            ref Grid grid = ref GetGrid(idx);

            grid.CustomData = customData;
            return ref Instance.m_Grids[idx];
        }

        public static BinaryWrapper[] ExportGrids()
        {
            BinaryWrapper[] wrappers;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                wrappers = new BinaryWrapper[s_EditorGrids.Length];
                for (int i = 0; i < wrappers.Length; i++)
                {
                    wrappers[i] = s_EditorGrids[i].Convert();
                }
            }
            else
#endif
            {
                wrappers = new BinaryWrapper[Instance.m_Grids.Length];
                for (int i = 0; i < wrappers.Length; i++)
                {
                    wrappers[i] = Instance.m_Grids[i].Convert();
                }
            }

            return wrappers;
        }
        public static void ImportGrids(params BinaryWrapper[] wrappers)
        {
            List<Grid> grids;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                grids = s_EditorGrids.ToList();
            }
            else
#endif
            {
                grids = Instance.m_Grids.ToList();
            }

            for (int i = 0; i < wrappers.Length; i++)
            {
                grids.Add(new Grid(wrappers[i]));
            }
        }

        private static bool IsInScreen(in float3 screenPos)
        {
            if (screenPos.y < 0 || screenPos.y > Screen.height ||
                screenPos.x < 0 || screenPos.x > Screen.width || screenPos.z < 0)
            {
                return false;
            }
            return true;
        }
        private static Grid InternalCreateGrid(in int idx, in Bounds bounds, in float gridCellSize, in bool enableNavMesh)
        {
            int xSize = Mathf.FloorToInt(bounds.size.x / gridCellSize);
            int zSize = Mathf.FloorToInt(bounds.size.z / gridCellSize);

            float halfSize = gridCellSize / 2;
            Vector3 cellSize = new Vector3(gridCellSize, .5f, gridCellSize);

            int count = 0;
            GridCell[] cells = new GridCell[xSize * zSize];
            for (int i = 0; i < zSize; i++)
            {
                for (int j = 0; j < xSize; j++)
                {
                    Vector3 center = new Vector3(
                        bounds.min.x + halfSize + (gridCellSize * j), 0,
                        bounds.max.z - halfSize - (gridCellSize * i));

                    cells[count] = new GridCell(idx, new int2(j, i), new Bounds(center, cellSize), enableNavMesh);
                    count++;
                }
            }

            return new Grid(idx, new int2(xSize, zSize), gridCellSize, bounds.size.y, enableNavMesh, cells);
        }
    }
}
