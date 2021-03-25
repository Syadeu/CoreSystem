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
using System.Reflection;

namespace Syadeu.Mono
{
    [DisallowMultipleComponent]
    public class GridManager : StaticManager<GridManager>
    {
        #region Init
        public override bool HideInHierarchy => false;

        //[SerializeField] private bool m_DrawGridAtRuntime = false;
        [SerializeField] private float m_CellSize = 2.5f;
        [SerializeField] private float m_GridHeight = 0;

        private Grid[] m_Grids = new Grid[0];
        //private LineRenderer m_LineRenderer;

#if UNITY_EDITOR
        public static Grid[] s_EditorGrids = new Grid[0];
#endif

        public delegate void GridRWAllTagLambdaDescription(in int i, ref GridCell gridCell, in UserTagFlag userTag, in CustomTagFlag customTag);
        public delegate void GridRWUserTagLambdaDescription(in int i, ref GridCell gridCell, in UserTagFlag userTag);
        public delegate void GridRWCustomTagLambdaDescription(in int i, ref GridCell gridCell, in CustomTagFlag customTag);
        public delegate void GridRWLambdaDescription(in int i, ref GridCell gridCell);
        public delegate void GridLambdaDescription(in int i, in GridCell gridCell);

        [Serializable]
        public struct BinaryWrapper
        {
            internal BinaryGrid m_Grid;
            internal BinaryGridCell[] m_GridCells;

            internal BinaryWrapper(BinaryGrid grid, BinaryGridCell[] cells)
            {
                m_Grid = grid;
                m_GridCells = cells;
            }
            public static BinaryWrapper ToWrapper(byte[] bytes) => bytes.ToObjectWithStream<BinaryWrapper>();

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

            public object CustomData;

            public bool EnableNavMesh;

            public BinaryGrid(in Grid grid)
            {
                Guid = grid.Guid;
                Idx = grid.Idx;

                GridSize = grid.GridSize;
                CellSize = grid.CellSize;
                Height = grid.Height;

                CustomData = grid.CustomData;

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

            public object CustomData;

            public BinaryGridCell(in GridCell gridCell)
            {
                ParentIdx = gridCell.ParentIdx;

                Location = gridCell.Location;
                Bounds_Center = gridCell.Bounds.center;
                Bounds_Size = gridCell.Bounds.size;

                CustomData = gridCell.CustomData;
            }
        }
        [Serializable]
        public struct Grid : IValidation, IEquatable<Grid>
        {
            #region Init

            internal Guid Guid;
            internal int Idx;

            internal int2 GridSize;
            internal GridCell[] Cells;
            public float CellSize;
            public float Height;

            internal object CustomData;

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
            internal Grid(in BinaryWrapper wrapper)
            {
                BinaryGrid grid;
                BinaryGridCell[] cells;

                //grid = wrapper.m_Grid.ToObjectWithStream<BinaryGrid>();
                //cells = wrapper.m_GridCells.ToObjectWithStream<BinaryGridCell[]>();
                grid = wrapper.m_Grid;
                cells = wrapper.m_GridCells;

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

                CustomData = grid.CustomData;

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

            #endregion

            #region Gets

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
            public IReadOnlyList<int> GetCells(UserTagFlag userTag)
            {
                List<int> indexes = new List<int>();
                for (int i = 0; i < Cells.Length; i++)
                {
                    if (!Cells[i].GetCustomData(out ITag tag))
                    {
                        continue;
                    }
                    if (userTag.HasFlag(tag.UserTag)) indexes.Add(i);
                }
                return indexes;
            }
            public IReadOnlyList<int> GetCells(CustomTagFlag customTag)
            {
                List<int> indexes = new List<int>();
                for (int i = 0; i < Cells.Length; i++)
                {
                    if (!Cells[i].GetCustomData(out ITag tag))
                    {
                        continue;
                    }
                    if (customTag.HasFlag(tag.CustomTag)) indexes.Add(i);
                }
                return indexes;
            }

            #endregion

            #region Lambda Descriptions

            public Grid For(GridLambdaDescription lambdaDescription)
            {
                for (int i = 0; i < Cells.Length; i++)
                {
                    lambdaDescription.Invoke(in i, in Cells[i]);
                }

                return this;
            }
            public Grid For<T>(GridLambdaDescription lambdaDescription) where T : struct, ITag
            {
                for (int i = 0; i < Cells.Length; i++)
                {
                    if (!Cells[i].GetCustomData(out T _)) continue;
                    lambdaDescription.Invoke(in i, in Cells[i]);
                }

                return this;
            }
            public Grid For(GridRWLambdaDescription lambdaDescription)
            {
                for (int i = 0; i < Cells.Length; i++)
                {
                    lambdaDescription.Invoke(in i, ref Cells[i]);
                }

                return this;
            }
            public Grid For<T>(GridRWLambdaDescription lambdaDescription) where T : struct, ITag
            {
                for (int i = 0; i < Cells.Length; i++)
                {
                    if (!Cells[i].GetCustomData(out T _)) continue;
                    lambdaDescription.Invoke(in i, ref Cells[i]);
                }

                return this;
            }
            public Grid For(GridRWUserTagLambdaDescription lambdaDescription)
            {
                for (int i = 0; i < Cells.Length; i++)
                {
                    if (!Cells[i].GetCustomData(out ITag tag))
                    {
                        continue;
                    }

                    lambdaDescription.Invoke(in i, ref Cells[i], tag.UserTag);
                }

                return this;
            }
            public Grid For<T>(GridRWUserTagLambdaDescription lambdaDescription) where T : struct, ITag
            {
                for (int i = 0; i < Cells.Length; i++)
                {
                    if (!Cells[i].GetCustomData(out T data))
                    {
                        continue;
                    }

                    lambdaDescription.Invoke(in i, ref Cells[i], data.UserTag);
                }

                return this;
            }
            public Grid For(GridRWCustomTagLambdaDescription lambdaDescription)
            {
                for (int i = 0; i < Cells.Length; i++)
                {
                    if (!Cells[i].GetCustomData(out ITag tag))
                    {
                        continue;
                    }

                    lambdaDescription.Invoke(in i, ref Cells[i], tag.CustomTag);
                }

                return this;
            }
            public Grid For<T>(GridRWCustomTagLambdaDescription lambdaDescription) where T : struct, ITag
            {
                for (int i = 0; i < Cells.Length; i++)
                {
                    if (!Cells[i].GetCustomData(out T data))
                    {
                        continue;
                    }

                    lambdaDescription.Invoke(in i, ref Cells[i], data.CustomTag);
                }

                return this;
            }
            public Grid For(GridRWAllTagLambdaDescription lambdaDescription)
            {
                for (int i = 0; i < Cells.Length; i++)
                {
                    if (!Cells[i].GetCustomData(out ITag tag))
                    {
                        continue;
                    }

                    lambdaDescription.Invoke(in i, ref Cells[i], tag.UserTag, tag.CustomTag);
                }

                return this;
            }
            public Grid For<T>(GridRWAllTagLambdaDescription lambdaDescription) where T : struct, ITag
            {
                for (int i = 0; i < Cells.Length; i++)
                {
                    if (!Cells[i].GetCustomData(out T data))
                    {
                        continue;
                    }

                    lambdaDescription.Invoke(in i, ref Cells[i], data.UserTag, data.CustomTag);
                }

                return this;
            }

            #endregion

            #region Custom Data
            public object GetCustomData() => CustomData;
            public bool GetCustomData<T>(out T value) where T : ITag
            {
                if (CustomData != null && CustomData is T t)
                {
                    value = t;
                    return true;
                }
                value = default;
                return false;
            }
            public void SetCustomData<T>(T data) where T : struct, ITag
            {
                if (data.GetType().GetCustomAttribute<SerializableAttribute>() == null)
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Mono, 
                        "해당 객체는 Serializable 어트리뷰트가 선언되지 않았습니다.");
                }

                CustomData = data;
            }
            #endregion

            #region Binary
            public BinaryWrapper ConvertToWrapper()
            {
                var temp = new BinaryGridCell[Cells.Length];
                for (int i = 0; i < Cells.Length; i++)
                {
                    temp[i] = new BinaryGridCell(in Cells[i]);
                }
                return new BinaryWrapper(new BinaryGrid(this), temp);
            }
            public static Grid FromBytes(BinaryWrapper wrapper) => new Grid(wrapper);
            public static Grid FromBytes(byte[] bytes) => BinaryWrapper.ToWrapper(bytes).ToGrid();
            #endregion
        }
        [Serializable]
        public struct GridCell : IValidation, IEquatable<GridCell>
        {
            #region Init
            internal readonly int ParentIdx;

            public int2 Location;
            public Bounds Bounds;

            internal object CustomData;

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
                CustomData = cell.CustomData;
            }

            public readonly Grid GetParent() => GetGrid(in ParentIdx);
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
            #endregion

            public object GetCustomData() => CustomData;
            public bool GetCustomData<T>(out T value) where T : ITag
            {
                if (CustomData != null && CustomData is T t)
                {
                    value = t;
                    return true;
                }
                value = default;
                return false;
            }
            public void SetCustomData<T>(T data) where T : struct, ITag
            {
                if (data.GetType().GetCustomAttribute<SerializableAttribute>() == null)
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
                        "해당 객체는 Serializable 어트리뷰트가 선언되지 않았습니다.");
                }

                CustomData = data;
            }
        }

        int tempIdx = -1;
        Color red = new Color(1, 0, 0, .2f);
        Color blue = new Color(0, 0, 1, .2f);
        Color green = new Color(0, 1, 1, .2f);
        public override void OnStart()
        {
            tempIdx = CreateGrid(new Bounds(new Vector3(1.25f, 0, 0), new Vector3(22.5f, 10, 40)), m_CellSize, true);
        }
        private void OnRenderObject()
        {
            if (tempIdx >= 0)
            {
                ref Grid grid = ref GetGrid(in tempIdx);
                for (int i = 0; i < grid.Length; i++)
                {
                    ref var cell = ref grid.GetCell(i);

                    if (cell.BlockedByNavMesh)
                    {
                        GLDrawBounds(in cell.Bounds, red);
                    }
                    else GLDrawBounds(in cell.Bounds, i % 2 == 0 ? green : blue);
                }
            }
        }

        #endregion

        #region Grid Methods
        public static int ClearGrids()
        {
            int count;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                count = s_EditorGrids.Length;
                s_EditorGrids = new Grid[0];
            }
            else
#endif
            {
                count = Instance.m_Grids.Length;
                Instance.m_Grids = new Grid[0];
            }

            return count;
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
        
        public static BinaryWrapper[] ExportGrids()
        {
            BinaryWrapper[] wrappers;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                wrappers = new BinaryWrapper[s_EditorGrids.Length];
                for (int i = 0; i < wrappers.Length; i++)
                {
                    wrappers[i] = s_EditorGrids[i].ConvertToWrapper();
                }
            }
            else
#endif
            {
                wrappers = new BinaryWrapper[Instance.m_Grids.Length];
                for (int i = 0; i < wrappers.Length; i++)
                {
                    wrappers[i] = Instance.m_Grids[i].ConvertToWrapper();
                }
            }

            return wrappers;
        }
        public static void ImportGrids(params Grid[] grids)
        {
            List<Grid> temp;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                temp = s_EditorGrids.ToList();
            }
            else
#endif
            {
                temp = Instance.m_Grids.ToList();
            }

            for (int i = 0; i < grids.Length; i++)
            {
                temp.Add(grids[i]);
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                s_EditorGrids = temp.ToArray();
            }
            else
#endif
            {
                Instance.m_Grids = temp.ToArray();
            }
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

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                s_EditorGrids = grids.ToArray();
            }
            else
#endif
            {
                Instance.m_Grids = grids.ToArray();
            }
        }

        #endregion

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
