using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

#if CORESYSTEM_UNSAFE
using System.Runtime.InteropServices;
#endif

using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;
using Unity.Collections;
using Unity.Mathematics;

using Syadeu;
using Syadeu.Database;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace Syadeu.Mono
{
    [DisallowMultipleComponent]
    [StaticManagerIntializeOnLoad]
    public sealed class GridManager : StaticManager<GridManager>
    {
        private static readonly object s_LockManager = new object();
        private static readonly object s_LockDependency = new object();

        #region Init
        public override bool HideInHierarchy => false;

        private Grid[] m_Grids = new Grid[0];
        private readonly ConcurrentDictionary<int, object> m_GridObjects = new ConcurrentDictionary<int, object>();
        private readonly ConcurrentDictionary<int2, object> m_CellObjects = new ConcurrentDictionary<int2, object>();
        private readonly ConcurrentDictionary<int2, List<int2>> m_CellDependency = new ConcurrentDictionary<int2, List<int2>>();
        private NavMeshQuery m_NavMeshQuery;

#if UNITY_EDITOR
        public static Grid[] s_EditorGrids = new Grid[0];
        public static Dictionary<int, object> s_EditorGridObjects = new Dictionary<int, object>();
        public static Dictionary<int2, object> s_EditorCellObjects = new Dictionary<int2, object>();
        public static Dictionary<int2, List<int2>> s_EditorCellDependency = new Dictionary<int2, List<int2>>();
#endif
        private readonly ConcurrentQueue<int2> m_DirtyFlags = new ConcurrentQueue<int2>();
        private readonly ConcurrentQueue<int2> m_DirtyFlagsAsync = new ConcurrentQueue<int2>();
        private readonly ConcurrentDictionary<int, GridLambdaWriteAllDescription<Grid, GridCell>> m_OnDirtyFlagRaised = new ConcurrentDictionary<int, GridLambdaWriteAllDescription<Grid, GridCell>>();
        private readonly ConcurrentDictionary<int, GridLambdaWriteAllDescription<Grid, GridCell>> m_OnDirtyFlagRaisedAsync = new ConcurrentDictionary<int, GridLambdaWriteAllDescription<Grid, GridCell>>();

        public delegate void GridLambdaWriteAllDescription<T, TA>(ref T t, ref TA ta);
        public delegate void GridLambdaRefRevDescription<T, TA>(ref T t, in TA ta);
        public delegate void GridLambdaRefDescription<T, TA>(in T t, ref TA ta);
        public delegate void GridLambdaDescription<T, TA>(in T t, in TA ta);

        public static Color 
            NormalColor = new Color(1, 1, 1, .1f),
            HighlightColor = new Color { g = 1, a = .1f },
            DisableColor = new Color { r = 1, a = .1f };

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
            public byte[] ToBinary() => this.ToBytesWithStream();

            public Grid ToGrid() => new Grid(in this);
        }
        [Serializable]
        internal struct BinaryGrid
        {
            public Guid Guid;
            public int Idx;

            public int3 GridCenter;
            public int3 GridSize;
            public float CellSize;

            public object CustomData;

            public bool EnableNavMesh;

            public BinaryGrid(in Grid grid)
            {
                Guid = grid.Guid;
                Idx = grid.Idx;

                GridCenter = grid.GridCenter;
                GridSize = grid.GridSize;
                CellSize = grid.CellSize;

#if UNITY_EDITOR
                if (IsMainthread() && !Application.isPlaying)
                {
                    if (s_EditorGridObjects.ContainsKey(grid.Idx))
                    {
                        CustomData = s_EditorGridObjects[grid.Idx];
                    }
                    else CustomData = null;
                }
                else
#endif
                {
                    if (Instance.m_GridObjects.ContainsKey(grid.Idx))
                    {
                        CustomData = Instance.m_GridObjects[grid.Idx];
                    }
                    else CustomData = null;
                }

                EnableNavMesh = grid.EnableNavMesh;
            }
        }
        [Serializable]
#if CORESYSTEM_UNSAFE
        unsafe internal struct BinaryGridCell
#else
        internal struct BinaryGridCell
#endif
        {
            public int ParentIdx;
            public int Idx;

            public int2 Location;
            public float3 Bounds_Center;
            public float3 Bounds_Size;

            public bool HasDependency;
            public int2 DependencyTarget;
            public int2[] DependencyChilds;
            public object CustomData;

#if CORESYSTEM_UNSAFE
            public BinaryGridCell(GridCell* gridCell)
            {
                ParentIdx = (*gridCell).ParentIdx;
                Idx = (*gridCell).Idx;

                Location = (*gridCell).Location;
                Bounds_Center = (*gridCell).Bounds.center;
                Bounds_Size = (*gridCell).Bounds.size;

                HasDependency = (*gridCell).HasDependency;
                DependencyTarget = (*gridCell).DependencyTarget;
#else
            public BinaryGridCell(GridCell gridCell)
            {
                ParentIdx = gridCell.ParentIdx;
                Idx = gridCell.Idx;

                ParentIdx = gridCell.ParentIdx;
                Idx = gridCell.Idx;

                Location = gridCell.Location;
                Bounds_Center = gridCell.Bounds.center;
                Bounds_Size = gridCell.Bounds.size;

                HasDependency = gridCell.HasDependency;
                DependencyTarget = gridCell.DependencyTarget;
#endif
#if UNITY_EDITOR
                if (IsMainthread() && !Application.isPlaying)
                {
#if CORESYSTEM_UNSAFE
                    if (s_EditorCellObjects.ContainsKey((*gridCell).Idxes))
                    {
                        CustomData = s_EditorCellObjects[(*gridCell).Idxes];
                    }
                    else CustomData = null;

                    if (s_EditorCellDependency.ContainsKey((*gridCell).Idxes))
                    {
                        DependencyChilds = s_EditorCellDependency[(*gridCell).Idxes].ToArray();
                    }
                    else DependencyChilds = null;
#else
                    if (s_EditorCellObjects.ContainsKey(gridCell.Idxes))
                    {
                        CustomData = s_EditorCellObjects[gridCell.Idxes];
                    }
                    else CustomData = null;

                    if (s_EditorCellDependency.ContainsKey(gridCell.Idxes))
                    {
                        DependencyChilds = s_EditorCellDependency[gridCell.Idxes].ToArray();
                    }
                    else DependencyChilds = null;
#endif
                }
                else
#endif
                {
#if CORESYSTEM_UNSAFE
                    if (Instance.m_CellObjects.ContainsKey((*gridCell).Idxes))
                    {
                        CustomData = Instance.m_CellObjects[(*gridCell).Idxes];
                    }
                    else CustomData = null;

                    if (Instance.m_CellDependency.ContainsKey((*gridCell).Idxes))
                    {
                        DependencyChilds = Instance.m_CellDependency[(*gridCell).Idxes].ToArray();
                    }
                    else DependencyChilds = null;
#else
                    if (Instance.m_CellObjects.ContainsKey(gridCell.Idxes))
                    {
                        CustomData = Instance.m_CellObjects[gridCell.Idxes];
                    }
                    else CustomData = null;

                    if (Instance.m_CellDependency.ContainsKey(gridCell.Idxes))
                    {
                        DependencyChilds = Instance.m_CellDependency[gridCell.Idxes].ToArray();
                    }
                    else DependencyChilds = null;
#endif
                }
            }
        }
        [Serializable] 
        public struct Grid : IValidation, IEquatable<Grid>, IDisposable
        {
            #region Init

            public readonly Guid Guid;
            public readonly int Idx;

            internal Bounds Bounds;
            internal int3 GridCenter;
            internal int3 GridSize;

            public readonly float CellSize;
#if CORESYSTEM_UNSAFE
            unsafe internal GridCell* Cells;
            public readonly int Length;
#else
            internal GridCell[] Cells;
            public int Length => Cells.Length;
#endif

            public bool EnableNavMesh;
            public bool EnableDrawGL;
            public bool EnableDrawIdx;

            internal Grid(int idx, int3 gridCenter, int3 gridSize, Bounds bounds, float cellSize, bool enableNavMesh, params GridCell[] cells)
            {
                Guid = Guid.NewGuid();
                Idx = idx;

                //Bounds = new Bounds(
                //    new Vector3(gridCenter.x, gridCenter.y, gridCenter.z),
                //    new Vector3(gridSize.x, gridSize.y, gridSize.z));
                Bounds = bounds;
                //$"{Bounds.center} :: {Bounds.size}".ToLog();
                GridCenter = gridCenter;
                GridSize = gridSize;

#if CORESYSTEM_UNSAFE
                Length = cells.Length;
                unsafe
                {
                    int cellLen = sizeof(GridCell);
                    Cells = (GridCell*)Marshal.AllocHGlobal(cellLen * Length);

                    fixed (GridCell* p = cells)
                    {
                        for (int i = 0; i < Length; i++)
                        {
                            Buffer.MemoryCopy(p + i, Cells + i, cellLen, cellLen);
                        }
                    }
                }
#else
                Cells = cells;
#endif
                CellSize = cellSize;

                //CustomData = null;

                EnableNavMesh = enableNavMesh;
                EnableDrawGL = false;
                EnableDrawIdx = false;
            }
            internal Grid(in BinaryWrapper wrapper)
            {
                BinaryGrid grid;
                BinaryGridCell[] cells;
                 
                grid = wrapper.m_Grid;
                cells = wrapper.m_GridCells;

                Guid = grid.Guid;
                Idx = grid.Idx;

                Bounds = new Bounds(
                    new Vector3(grid.GridCenter.x, grid.GridCenter.y, grid.GridCenter.z),
                    new Vector3(grid.GridSize.x, grid.GridSize.y, grid.GridSize.z));
                GridCenter = grid.GridCenter;
                GridSize = grid.GridSize;

                GridCell[] convertedCells = new GridCell[cells.Length];
                for (int i = 0; i < cells.Length; i++)
                {
                    convertedCells[i] = new GridCell(in cells[i]);
                }
#if CORESYSTEM_UNSAFE
                Length = convertedCells.Length;
                unsafe
                {
                    int cellLen = sizeof(GridCell);
                    Cells = (GridCell*)Marshal.AllocHGlobal(cellLen * Length);

                    fixed (GridCell* p = convertedCells)
                    {
                        for (int i = 0; i < Length; i++)
                        {
                            Buffer.MemoryCopy(p + i, Cells + i, cellLen, cellLen);
                        }
                    }
                }
#else
                Cells = convertedCells;
#endif
                CellSize = grid.CellSize;

                //CustomData = grid.CustomData;
                if (grid.CustomData != null)
                {
#if UNITY_EDITOR
                    if (IsMainthread() && !Application.isPlaying)
                    {
                        s_EditorGridObjects.Add(grid.Idx, grid.CustomData);
                    }
                    else
#endif
                    {
                        Instance.m_GridObjects.TryAdd(grid.Idx, grid.CustomData);
                    }
                }

                EnableNavMesh = grid.EnableNavMesh;
                EnableDrawGL = false;
                EnableDrawIdx = false;
            }

            public bool IsValid() => HasGrid(in Guid);
            public bool Equals(Grid other) => Guid.Equals(other.Guid);

            #endregion

            #region Has

            public bool HasCell(int idx) => idx >= 0 && Length > idx;
            public bool HasCell(Vector2Int location) => HasCell(location.x, location.y);
            public bool HasCell(int2 location) => HasCell(location.x, location.y);
            public bool HasCell(int x, int y)
            {
                if (x < 0 || y < 0 ||
                    x > GridSize.x || y > GridSize.z) return false;

                int idx = (GridSize.z * y) + x;
                if (idx >= Length) return false;
                return true;
            }
            public bool HasCell(Vector3 worldPosition)
            {
                if (worldPosition.y <= Bounds.extents.y)
                {
                    GridCell first;
#if CORESYSTEM_UNSAFE
                    unsafe
                    {
                        first = *Cells;
                    }
#else
                    first = Cells[0];
#endif

                    int x = Math.Abs(Convert.ToInt32((worldPosition.x - first.Bounds.center.x) / CellSize));
                    int y = Math.Abs(Convert.ToInt32((worldPosition.z - first.Bounds.center.z) / CellSize));

                    int idx = (GridSize.z * y) + x;
                    if (idx < Length)
                    {
                        return true;
                    }
                }

                return false;
            }

            #endregion

            #region Gets

#if CORESYSTEM_UNSAFE
            unsafe public GridCell* GetCellPointer(int idx)
            {
                return Cells + idx;
            }
            unsafe public GridCell* GetCellPointer(Vector2Int location)
            {
                int idx = (GridSize.z * location.y) + location.x;
                if (idx >= Length) throw new CoreSystemException(CoreSystemExceptionFlag.Mono, $"Out of Range({location.x},{location.y}). " +
                     $"해당 좌표계는 이 그리드에 존재하지않습니다.");

                return Cells + idx;
            }
            unsafe public GridCell* GetCellPointer(int2 location)
            {
                int idx = (GridSize.z * location.y) + location.x;
                if (idx >= Length) throw new CoreSystemException(CoreSystemExceptionFlag.Mono, $"Out of Range({location.x},{location.y}). " +
                     $"해당 좌표계는 이 그리드에 존재하지않습니다.");

                return Cells + idx;
            }
            unsafe public GridCell* GetCellPointer(int x, int y)
            {
                int idx = (GridSize.z * y) + x;
                if (idx >= Length) throw new CoreSystemException(CoreSystemExceptionFlag.Mono, $"Out of Range({x},{y}). " +
                     $"해당 좌표계는 이 그리드에 존재하지않습니다.");

                return Cells + idx;
            }
            unsafe public GridCell* GetCellPointer(Vector3 worldPosition)
            {
                if (worldPosition.y <= Bounds.extents.y)
                {
                    GridCell first = *Cells;

                    int x = Math.Abs(Convert.ToInt32((worldPosition.x - first.Bounds.center.x) / CellSize));
                    int y = Math.Abs(Convert.ToInt32((worldPosition.z - first.Bounds.center.z) / CellSize));

                    int idx = (GridSize.z * y) + x;
                    if (idx < Length)
                    {
                        return Cells + idx;
                    }
                }

                throw new CoreSystemException(CoreSystemExceptionFlag.Mono, $"Out of Range({worldPosition.x},{worldPosition.y},{worldPosition.z}). " +
                    $"해당 좌표계는 이 그리드에 존재하지않습니다.");
            }
#endif

            public ref GridCell GetCell(int idx)
            {
                if (idx >= Length)
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Mono, $"Out of Range({idx}). " +
                        $"해당 좌표계는 이 그리드에 존재하지않습니다.");
                }
#if CORESYSTEM_UNSAFE
                unsafe
#endif
                {
                    return ref Cells[idx];
                }
            }
            public ref GridCell GetCell(Vector2Int location)
            {
                int idx = ToCellIndex(location);
                if (idx >= Length ||
                    location.x < 0 || location.y < 0 ||
                    location.x > GridSize.x || location.y > GridSize.z) throw new CoreSystemException(CoreSystemExceptionFlag.Mono, $"Out of Range({location.x},{location.y}). " +
                    $"해당 좌표계는 이 그리드에 존재하지않습니다.");

#if CORESYSTEM_UNSAFE
                unsafe
#endif
                {
                    return ref Cells[idx];
                }
            }
            public ref GridCell GetCell(int2 location)
            {
                int idx = ToCellIndex(location);
                if (idx >= Length || location.x < 0 || location.y < 0 ||
                    location.x > GridSize.x || location.y > GridSize.z) throw new CoreSystemException(CoreSystemExceptionFlag.Mono, $"Out of Range({location.x},{location.y}). " +
                    $"해당 좌표계는 이 그리드에 존재하지않습니다.");

#if CORESYSTEM_UNSAFE
                unsafe
#endif
                {
                    return ref Cells[idx];
                }
            }
            public ref GridCell GetCell(int x, int y)
            {
                int idx = ToCellIndex(x, y);
                if (idx >= Length || x < 0 || y < 0 ||
                    x > GridSize.x || y > GridSize.z) throw new CoreSystemException(CoreSystemExceptionFlag.Mono, $"Out of Range({x},{y}). " +
                     $"해당 좌표계는 이 그리드에 존재하지않습니다.");
#if CORESYSTEM_UNSAFE
                unsafe
#endif
                {
                    return ref Cells[idx];
                }
            }
            public ref GridCell GetCell(Vector3 worldPosition)
            {
                if (worldPosition.y <= Bounds.extents.y)
                {
                    int idx = ToCellIndex(worldPosition);
                    if (idx < Length)
                    {
#if CORESYSTEM_UNSAFE
                        unsafe
#endif
                        {
                            return ref Cells[idx];
                        }
                    }
                }

                throw new CoreSystemException(CoreSystemExceptionFlag.Mono, $"Out of Range({worldPosition.x},{worldPosition.y},{worldPosition.z}). " +
                    $"해당 좌표계는 이 그리드에 존재하지않습니다.");
            }
            
            public IReadOnlyList<int> GetCells(UserTagFlag userTag)
            {
                List<int> indexes = new List<int>();
#if CORESYSTEM_UNSAFE
                unsafe
#endif
                {
                    for (int i = 0; i < Length; i++)
                    {
                        if (!Cells[i].GetCustomData(out ITag tag))
                        {
                            continue;
                        }
                        if (userTag.HasFlag(tag.UserTag)) indexes.Add(i);
                    }
                }
                
                return indexes;
            }
            public IReadOnlyList<int> GetCells(CustomTagFlag customTag)
            {
                List<int> indexes = new List<int>();
#if CORESYSTEM_UNSAFE
                unsafe
#endif
                {
                    for (int i = 0; i < Length; i++)
                    {
                        if (!Cells[i].GetCustomData(out ITag tag))
                        {
                            continue;
                        }
                        if (customTag.HasFlag(tag.CustomTag)) indexes.Add(i);
                    }
                }
                
                return indexes;
            }

            public GridRange GetRange(int idx, int range)
            {
                if (range <= 0) throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
                    "range 는 0 보다 커야됩니다.");

                List<int> targets = new List<int>();
                // 왼쪽 아래 부터 탐색 시작
                int startIdx = idx - range + (GridSize.z * range);

                int height = ((range * 2) + 1);
                for (int yGrid = 0; yGrid < height; yGrid++)
                {
                    for (int xGrid = 0; xGrid < height; xGrid++)
                    {
                        int temp = startIdx - (yGrid * GridSize.z) + xGrid;
                        
                        if (HasCell(temp)) targets.Add(temp);
                        if (temp >= temp - (temp % GridSize.x) + GridSize.x - 1) break;
                    }
                }

#if CORESYSTEM_UNSAFE_INTERNAL
                unsafe
                {
                    return new GridRange(Cells, targets.ToArray());
                }
#else
                return new GridRange(Idx, targets.ToArray());
#endif
            }
            public GridRange GetRange(int x, int y, int range)
            {
                if (range <= 0) throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
                    "range 는 0 보다 커야됩니다.");

                List<int> targets = new List<int>();
                // 왼쪽 아래 부터 탐색 시작
                int startIdx = (GridSize.z * (y + range)) + x - range;

                int height = ((range * 2) + 1);
                for (int yGrid = 0; yGrid < height; yGrid++)
                {
                    for (int xGrid = 0; xGrid < height; xGrid++)
                    {
                        int temp = startIdx - (yGrid * GridSize.z) + xGrid;
                        if (HasCell(temp)) targets.Add(temp);
                        if (temp >= (GridSize.z * (y - yGrid + 2)) + GridSize.x - 1) break;
                    }
                }

#if CORESYSTEM_UNSAFE_INTERNAL
                unsafe
                {
                    return new GridRange(Cells, targets.ToArray());
                }
#else
                return new GridRange(Idx, targets.ToArray());
#endif
            }
            public GridRange GetRange(int2 location, int range)
            {
                if (range <= 0) throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
                    "range 는 0 보다 커야됩니다.");

                List<int> targets = new List<int>();
                // 왼쪽 아래 부터 탐색 시작
                int startIdx = (GridSize.z * (location.y + range)) + location.x - range;

                int height = ((range * 2) + 1);
                for (int yGrid = 0; yGrid < height; yGrid++)
                {
                    for (int xGrid = 0; xGrid < height; xGrid++)
                    {
                        int temp = startIdx - (yGrid * GridSize.z) + xGrid;
                        if (HasCell(temp)) targets.Add(temp);
                        if (temp >= (GridSize.z * (location.y - yGrid + 2)) + GridSize.x - 1) break;
                    }
                }

#if CORESYSTEM_UNSAFE_INTERNAL
                unsafe
                {
                    return new GridRange(Cells, targets.ToArray());
                }
#else
                return new GridRange(Idx, targets.ToArray());
#endif
            }

            public int ToCellIndex(Vector3 worldPosition)
            {
                GridCell first;
#if CORESYSTEM_UNSAFE
                unsafe
                {
                    first = *Cells;
                }
#else
                first = Cells[0];
#endif

                int x = Math.Abs(Convert.ToInt32((worldPosition.x - first.Bounds.center.x) / CellSize));
                int y = Math.Abs(Convert.ToInt32((worldPosition.z - first.Bounds.center.z) / CellSize));
                return ToCellIndex(x, y);
            }
            public int ToCellIndex(Vector2Int location) => ToCellIndex(location.x, location.y);
            public int ToCellIndex(int2 location) => ToCellIndex(location.x, location.y);
            public int ToCellIndex(int x, int y) => (GridSize.z * y) + x;

            #endregion

            #region Lambda Descriptions

            public void For(GridLambdaDescription<int, GridCell> lambdaDescription)
            {
#if CORESYSTEM_UNSAFE
                unsafe
#endif
                {
                    for (int i = 0; i < Length; i++)
                    {
                        lambdaDescription.Invoke(in i, in Cells[i]);
                    }
                }
            }
            public void For(GridLambdaDescription<Grid, GridCell> lambdaDescription)
            {
#if CORESYSTEM_UNSAFE
                unsafe
#endif
                {
                    for (int i = 0; i < Length; i++)
                    {
                        lambdaDescription.Invoke(in this, in Cells[i]);
                    }
                }
            }
            public void For(GridLambdaRefDescription<Grid, GridCell> lambdaDescription)
            {
#if CORESYSTEM_UNSAFE
                unsafe
#endif
                {
                    for (int i = 0; i < Length; i++)
                    {
                        lambdaDescription.Invoke(in this, ref Cells[i]);
                    }
                }
            }
            public void For(GridLambdaWriteAllDescription<Grid, GridCell> lambdaDescription)
            {
#if CORESYSTEM_UNSAFE
                unsafe
#endif
                {
                    for (int i = 0; i < Length; i++)
                    {
                        lambdaDescription.Invoke(ref this, ref Cells[i]);
                    }
                }
            }
            public void For<T>(GridLambdaRefDescription<int, T> lambdaDescription) where T : struct, ITag
            {
#if CORESYSTEM_UNSAFE
                unsafe
#endif
                {
                    for (int i = 0; i < Length; i++)
                    {
                        if (!Cells[i].GetCustomData(out T data)) continue;
                        lambdaDescription.Invoke(in i, ref data);
                    }
                }
            }
            public void For<T>(GridLambdaDescription<int, T> lambdaDescription) where T : struct, ITag
            {
#if CORESYSTEM_UNSAFE
                unsafe
#endif
                {
                    for (int i = 0; i < Length; i++)
                    {
                        if (!Cells[i].GetCustomData(out T data)) continue;
                        lambdaDescription.Invoke(in i, in data);
                    }
                }
            }
            public void For(GridLambdaRefDescription<int, GridCell> lambdaDescription)
            {
#if CORESYSTEM_UNSAFE
                unsafe
#endif
                {
                    for (int i = 0; i < Length; i++)
                    {
                        lambdaDescription.Invoke(in i, ref Cells[i]);
                    }
                }
            }

#if CORESYSTEM_UNSAFE
            public ParallelLoopResult ParallelFor(GridLambdaDescription<int, GridCell> lambdaDescription)
            {
                unsafe
                {
                    GridCell* other = Cells;
                    return Parallel.For(0, Length, (i) =>
                    {
                        lambdaDescription.Invoke(in i, in other[i]);
                    });
                }
            }
            public ParallelLoopResult ParallelFor(GridLambdaRefDescription<int, GridCell> lambdaDescription)
            {
                unsafe
                {
                    GridCell* other = Cells;
                    return Parallel.For(0, Length, (i) =>
                    {
                        lambdaDescription.Invoke(in i, ref other[i]);
                    });
                }
            }
            public ParallelLoopResult ParallelFor<T>(GridLambdaDescription<int, T> lambdaDescription) where T : struct, ITag
            {
                unsafe
                {
                    GridCell* other = Cells;
                    return Parallel.For(0, Length, (i) =>
                    {
                        if (!other[i].GetCustomData(out T data)) return;
                        lambdaDescription.Invoke(in i, in data);
                    });
                }
            }
            public ParallelLoopResult ParallelFor<T>(GridLambdaRefDescription<int, T> lambdaDescription) where T : struct, ITag
            {
                unsafe
                {
                    GridCell* other = Cells;
                    return Parallel.For(0, Length, (i) =>
                    {
                        if (!other[i].GetCustomData(out T data)) return;
                        lambdaDescription.Invoke(in i, ref data);
                    });
                }
            }
#endif

            #endregion

            #region Custom Data
            public object GetCustomData()
            {
#if UNITY_EDITOR
                if (IsMainthread() && !Application.isPlaying)
                {
                    if (s_EditorGridObjects.TryGetValue(Idx, out object value)) return value;
                }
                else
#endif
                {
                    if (Instance.m_GridObjects.TryGetValue(Idx, out object value)) return value;
                }

                return null;
            }
            public bool GetCustomData<T>(out T value) where T : ITag
            {
                object data = GetCustomData();
                if (data != null && data is T t)
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

#if UNITY_EDITOR
                if (IsMainthread() && !Application.isPlaying)
                {
                    if (s_EditorGridObjects.ContainsKey(Idx)) s_EditorGridObjects[Idx] = data;
                    else s_EditorGridObjects.Add(Idx, data);
                }
                else
#endif
                {
                    if (Instance.m_GridObjects.ContainsKey(Idx)) Instance.m_GridObjects[Idx] = data;
                    else Instance.m_GridObjects.TryAdd(Idx, data);
                }
            }
            public void RemoveCustomData()
            {
#if UNITY_EDITOR
                if (IsMainthread() && !Application.isPlaying)
                {
                    if (s_EditorGridObjects.ContainsKey(Idx)) s_EditorGridObjects.Remove(Idx);
                }
                else
#endif
                {
                    if (Instance.m_GridObjects.ContainsKey(Idx)) Instance.m_GridObjects.TryRemove(Idx, out _);
                }
            }
            #endregion

            #region Binary
            public BinaryWrapper ConvertToWrapper()
            {
                var temp = new BinaryGridCell[Length];
#if CORESYSTEM_UNSAFE
                unsafe
#endif
                {
                    for (int i = 0; i < Length; i++)
                    {
#if CORESYSTEM_UNSAFE
                        temp[i] = new BinaryGridCell(Cells + i);
#else
                        temp[i] = new BinaryGridCell(Cells[i]);
#endif
                    }
                }
                
                return new BinaryWrapper(new BinaryGrid(this), temp);
            }
            public static Grid FromBytes(BinaryWrapper wrapper) => new Grid(wrapper);
            public static Grid FromBytes(byte[] bytes) => BinaryWrapper.ToWrapper(bytes).ToGrid();

            #endregion

            public void OnDirtyMarked(GridLambdaWriteAllDescription<Grid, GridCell> async)
            {
                Instance.m_OnDirtyFlagRaised.TryAdd(Idx, async);
            }
            public void OnDirtyMarkedAsync(GridLambdaWriteAllDescription<Grid, GridCell> async)
            {
                Instance.m_OnDirtyFlagRaisedAsync.TryAdd(Idx, async);
            }

            public void Dispose()
            {
#if CORESYSTEM_UNSAFE
                unsafe
                {
                    Marshal.FreeHGlobal((IntPtr)Cells);
                }
#endif
            }
        }
        [Serializable]
        public struct GridCell : IValidation, IEquatable<GridCell>, IDisposable
        {
            #region Init
            public readonly int2 Idxes;
            public readonly int ParentIdx;
            public readonly int Idx;

            public readonly int2 Location;
            public readonly Bounds Bounds;

            private bool m_Enabled;

            public bool IsRoot
            {
                get
                {
                    if (!HasDependency) return true;

                    return false;
                }
            }
            public bool HasDependency { get; internal set; }
            public int2 DependencyTarget { get; internal set; }
            public bool HasDependencyChilds
            {
                get
                {
#if UNITY_EDITOR
                    if (IsMainthread() && !Application.isPlaying)
                    {
                        return s_EditorCellDependency.ContainsKey(Idxes);
                    }
                    else
#endif
                    {
                        return Instance.m_CellDependency.ContainsKey(Idxes);
                    }
                }
            }

            private readonly float3[] Verties
            {
                get
                {
                    return new float3[4]
                    {
                        new float3(Bounds.min.x, Bounds.min.y, Bounds.min.z),
                        new float3(Bounds.min.x, Bounds.min.y, Bounds.max.z),
                        new float3(Bounds.max.x, Bounds.min.y, Bounds.max.z),
                        new float3(Bounds.max.x, Bounds.min.y, Bounds.min.z)
                    };
                }
            }
            private float3[] NavMeshVerties
            {
                get
                {
                    return new float3[]
                    {
                        Bounds.center,
                        new float3(Bounds.center.x + Bounds.extents.x - .01f, Bounds.center.y, Bounds.center.z),
                        new float3(Bounds.center.x - Bounds.extents.x + .01f, Bounds.center.y, Bounds.center.z),
                        new float3(Bounds.center.x, Bounds.center.y, Bounds.center.z + Bounds.extents.z - .01f),
                        new float3(Bounds.center.x, Bounds.center.y, Bounds.center.z - Bounds.extents.z + .01f)
                    };
                }
            }

            public bool Enabled
            {
                get
                {
                    if (HasDependency) return GetDependencyTarget().Enabled;

                    if (BlockedByNavMesh || !m_Enabled)
                    {
                        return false;
                    }
                    return true;
                }
                set
                {
                    if (HasDependency)
                    {
                        ref var temp = ref GetDependencyTarget();
                        temp.Enabled = value;
                    }
                    else m_Enabled = value;
                }
            }
            public bool Highlighted;

            public Color Color
            {
                get
                {
                    if (HasDependency)
                    {
                        ref GridCell cell = ref GetDependencyTarget();
                        return cell.Color;
                    }

                    if (!Enabled)
                    {
                        return DisableColor;
                    }
                    else
                    {
                        if (Highlighted)
                        {
                            return HighlightColor;
                        }
                        else
                        {
                            return NormalColor;
                        }
                    }
                }
            }
            public bool BlockedByNavMesh
            {
                get
                {
                    ref Grid parent = ref GetGrid(in ParentIdx);

                    if (!parent.EnableNavMesh) return false;
                    for (int i = 0; i < NavMeshVerties.Length; i++)
                    {
                        if (IsMainthread())
                        {
                            if (!NavMesh.SamplePosition(NavMeshVerties[i], out NavMeshHit hit, .25f, -1))
                            {
                                return true;
                            }
                            if (!hit.hit) return true;
                        }
                        else
                        {
                            NavMeshLocation hit = Instance.m_NavMeshQuery.MapLocation(NavMeshVerties[i], Vector3.one * .25f, 0, -1);
                            if (!Instance.m_NavMeshQuery.IsValid(hit.polygon)) return true;
                        }
                    }
                    return false;
                }
            }
            
            internal GridCell(int parentIdx, int idx, int2 location, Bounds bounds)
            {
                Idxes = new int2(parentIdx, idx);
                ParentIdx = parentIdx;
                Idx = idx;

                Location = location;
                Bounds = bounds;

                HasDependency = false;
                DependencyTarget = -1;
                m_Enabled = true;
                Highlighted = false;
            }
            internal GridCell(in BinaryGridCell cell) : this(cell.ParentIdx, cell.Idx, cell.Location, new Bounds(cell.Bounds_Center, cell.Bounds_Size))
            {
                HasDependency = cell.HasDependency;
                DependencyTarget = cell.DependencyTarget;
                //DependencyChilds = cell.DependencyChilds;
                //CustomData = cell.CustomData;
                int2 loc = new int2(cell.ParentIdx, cell.Idx);
#if UNITY_EDITOR
                if (IsMainthread() && !Application.isPlaying)
                {
                    if (s_EditorCellObjects.ContainsKey(loc))
                    {
                        s_EditorCellObjects[loc] = cell.CustomData;
                    }
                    else s_EditorCellObjects.Add(loc, cell.CustomData);

                    if (s_EditorCellDependency.ContainsKey(loc))
                    {
                        if (cell.DependencyChilds == null)
                        {
                            s_EditorCellDependency[loc].Clear();
                        }
                        else s_EditorCellDependency[loc] = cell.DependencyChilds.ToList();
                    }
                    else
                    {
                        if (cell.DependencyChilds == null)
                        {
                            s_EditorCellDependency.Add(loc, new List<int2>());
                        }
                        else s_EditorCellDependency.Add(loc, cell.DependencyChilds.ToList());
                    }
                }
                else
#endif
                {
                    if (Instance.m_CellObjects.ContainsKey(loc))
                    {
                        Instance.m_CellObjects[loc] = cell.CustomData;
                    }
                    else Instance.m_CellObjects.TryAdd(loc, cell.CustomData);

                    if (Instance.m_CellDependency.ContainsKey(loc))
                    {
                        if (cell.DependencyChilds == null)
                        {
                            Instance.m_CellDependency[loc].Clear();
                        }
                        else Instance.m_CellDependency[loc] = cell.DependencyChilds.ToList();
                    }
                    else
                    {
                        if (cell.DependencyChilds == null)
                        {
                            Instance.m_CellDependency.TryAdd(loc, new List<int2>());
                        }
                        else Instance.m_CellDependency.TryAdd(loc, cell.DependencyChilds.ToList());
                    }
                }
            }

            public bool IsValid() => Verties != null;
            public bool Equals(GridCell other) => Location.Equals(other.Location);
            public bool IsVisible()
            {
                if (RenderManager.IsInCameraScreen(Bounds.center)) return true;
                return false;
            }
            #endregion

            public bool HasCell(Direction direction)
            {
                ref Grid grid = ref GetGrid(ParentIdx);
                
                int2 target = Location;
                if (direction.HasFlag(Direction.Up)) target.y -= 1;
                if (direction.HasFlag(Direction.Down)) target.y += 1;
                if (direction.HasFlag(Direction.Left)) target.x -= 1;
                if (direction.HasFlag(Direction.Right)) target.x += 1;

                return grid.HasCell(target);
            }
            //public bool HasCell(Direction direction, bool hasCustomData)
            //{
            //    ref Grid grid = ref GetGrid(ParentIdx);
                
            //    int2 target = Location;
            //    if (direction.HasFlag(Direction.Up)) target.y -= 1;
            //    if (direction.HasFlag(Direction.Down)) target.y += 1;
            //    if (direction.HasFlag(Direction.Left)) target.x += 1;
            //    if (direction.HasFlag(Direction.Right)) target.x -= 1;

            //    int idx = (grid.GridSize.z * target.y) + target.x;
            //    if (idx >= grid.Length) return false;

            //    ref GridCell cell = ref grid.GetCell(idx);
            //    return cell.GetCustomData() != null == hasCustomData;
            //}
            //public bool HasCell<T>(Direction direction, bool hasCustomData)
            //{
            //    ref Grid grid = ref GetGrid(ParentIdx);
                
            //    int2 target = Location;
            //    if (direction.HasFlag(Direction.Up)) target.y -= 1;
            //    if (direction.HasFlag(Direction.Down)) target.y += 1;
            //    if (direction.HasFlag(Direction.Left)) target.x += 1;
            //    if (direction.HasFlag(Direction.Right)) target.x -= 1;

            //    int idx = (grid.GridSize.z * target.y) + target.x;
            //    if (idx >= grid.Length) return false;

            //    ref GridCell cell = ref grid.GetCell(idx);
            //    object data = cell.GetCustomData();
            //    return (data != null && data is T) == hasCustomData;
            //}
            public ref GridCell FindCell(Direction direction)
            {
                if (!HasCell(direction)) throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
                    $"{direction} 에 그리드셀이 존재하지 않습니다.");

                ref Grid grid = ref GetGrid(ParentIdx);

                int2 target = Location;
                if (direction.HasFlag(Direction.Up)) target.y -= 1;
                if (direction.HasFlag(Direction.Down)) target.y += 1;
                if (direction.HasFlag(Direction.Left)) target.x -= 1;
                if (direction.HasFlag(Direction.Right)) target.x += 1;

                return ref grid.GetCell(target);
            }
            public int FindCellIdx(Direction direction)
            {
                int2 target = Location;
                if (direction.HasFlag(Direction.Up)) target.y -= 1;
                if (direction.HasFlag(Direction.Down)) target.y += 1;
                if (direction.HasFlag(Direction.Left)) target.x -= 1;
                if (direction.HasFlag(Direction.Right)) target.x += 1;

                return GetGrid(ParentIdx).ToCellIndex(target);
            }

            #region Custom Data

            public object GetCustomData()
            {
                if (HasDependency)
                {
#if UNITY_EDITOR
                    if (IsMainthread() && !Application.isPlaying)
                    {
                        if (s_EditorCellObjects.TryGetValue(DependencyTarget, out var data))
                        {
                            return data;
                        }
                    }
                    else
#endif
                    {
                        if (Instance.m_CellObjects.TryGetValue(DependencyTarget, out var data))
                        {
                            return data;
                        }
                    }
                }
                else
                {
#if UNITY_EDITOR
                    if (IsMainthread() && !Application.isPlaying)
                    {
                        if (s_EditorCellObjects.TryGetValue(Idxes, out var data))
                        {
                            return data;
                        }
                    }
                    else
#endif
                    {
                        if (Instance.m_CellObjects.TryGetValue(Idxes, out var data))
                        {
                            return data;
                        }
                    }
                }

                return null;
            }
            public bool GetCustomData<T>(out T value) where T : ITag
            {
                if (HasDependency)
                {
#if UNITY_EDITOR
                    if (IsMainthread() && !Application.isPlaying)
                    {
                        if (s_EditorCellObjects.TryGetValue(DependencyTarget, out var data) &&
                            data is T t)
                        {
                            value = t;
                            return true;
                        }
                    }
                    else
#endif
                    {
                        if (Instance.m_CellObjects.TryGetValue(DependencyTarget, out var data) &&
                            data is T t)
                        {
                            value = t;
                            return true;
                        }
                    }
                }
                else
                {
#if UNITY_EDITOR
                    if (IsMainthread() && !Application.isPlaying)
                    {
                        if (s_EditorCellObjects.TryGetValue(Idxes, out var data) &&
                            data is T t)
                        {
                            value = t;
                            return true;
                        }
                    }
                    else
#endif
                    {
                        if (Instance.m_CellObjects.TryGetValue(Idxes, out var data) &&
                            data is T t)
                        {
                            value = t;
                            return true;
                        }
                    }
                }

                value = default;
                return false;
            }
            public void SetCustomData<T>(T data) where T : struct, ITag
            {
                if (data.GetType().GetCustomAttribute<SerializableAttribute>() == null)
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
                        $"해당 객체({data.GetType().Name})는 Serializable 어트리뷰트가 선언되지 않았습니다.");
                }

                InternalSetCustomData(data);
            }
            private void InternalSetCustomData(object data)
            {
                if (HasDependency)
                {
#if UNITY_EDITOR
                    if (IsMainthread() && !Application.isPlaying)
                    {
                        if (s_EditorCellObjects.ContainsKey(DependencyTarget))
                        {
                            s_EditorCellObjects[DependencyTarget] = data;
                        }
                        else
                        {
                            s_EditorCellObjects.Add(DependencyTarget, data);
                        }
                    }
                    else
#endif
                    {
                        if (Instance.m_CellObjects.ContainsKey(DependencyTarget))
                        {
                            Instance.m_CellObjects[DependencyTarget] = data;
                        }
                        else
                        {
                            Instance.m_CellObjects.TryAdd(DependencyTarget, data);
                        }
                    }
                }
                else
                {
#if UNITY_EDITOR
                    if (IsMainthread() && !Application.isPlaying)
                    {
                        if (s_EditorCellObjects.ContainsKey(Idxes))
                        {
                            s_EditorCellObjects[Idxes] = data;
                        }
                        else
                        {
                            s_EditorCellObjects.Add(Idxes, data);
                        }
                    }
                    else
#endif
                    {
                        if (Instance.m_CellObjects.ContainsKey(Idxes))
                        {
                            Instance.m_CellObjects[Idxes] = data;
                        }
                        else
                        {
                            Instance.m_CellObjects.TryAdd(Idxes, data);
                        }
                    }
                }

                SetDirty();
            }
            public void RemoveCustomData()
            {
                if (HasDependency)
                {
#if UNITY_EDITOR
                    if (IsMainthread() && !Application.isPlaying)
                    {
                        s_EditorCellObjects.Remove(DependencyTarget);
                    }
                    else
#endif
                    {
                        Instance.m_CellObjects.TryRemove(DependencyTarget, out _);
                    }
                }
                else
                {
#if UNITY_EDITOR
                    if (IsMainthread() && !Application.isPlaying)
                    {
                        s_EditorCellObjects.Remove(Idxes);
                    }
                    else
#endif
                    {
                        Instance.m_CellObjects.TryRemove(Idxes, out _);
                    }
                }

                SetDirty();
            }
            public void MoveCustomData(int2 gridNCellIdxes) => MoveCustomData(gridNCellIdxes.x, gridNCellIdxes.y);
            public void MoveCustomData(int gridIdx, int cellIdx)
            {
                if (HasDependency || HasDependencyChilds)
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
                        "이 셀은 다른 셀과 연결되있어서 커스텀 데이터를 옮길 수 없습니다.");
                }

                object data = GetCustomData();
                RemoveCustomData();

                GetGrid(gridIdx).GetCell(cellIdx).InternalSetCustomData(data);
            }

            #endregion

            #region Dependency

            public ref GridCell GetDependencyTarget()
            {
                if (!HasDependency)
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
                        $"이 그리드({Location.x}, {Location.y} : {ParentIdx}: {Idx}) 는 루트 셀이 없습니다.");
                }

                ref var temp = ref GetGrid(DependencyTarget.x);
                return ref temp.GetCell(DependencyTarget.y);
            }

            public bool HasTargetDependency(in int2 targetGridIdxes)
            {
                List<int2> temp;
#if UNITY_EDITOR
                if (IsMainthread() && !Application.isPlaying)
                {
                    if (!s_EditorCellDependency.TryGetValue(Idxes, out temp))
                    {
                        return false;
                    }
                }
                else
#endif
                {
                    if (!Instance.m_CellDependency.TryGetValue(Idxes, out temp))
                    {
                        return false;
                    }
                }

                for (int i = 0; i < temp.Count; i++)
                {
                    if (temp[i].Equals(targetGridIdxes)) return true;
                }
                return false;
            }
            /// <summary>
            /// 해당 셀에 영향받는 셀임을 선언합니다.<br/>
            /// 커스텀 데이터는 해당 셀로 override 됩니다.
            /// </summary>
            /// <param name="gridIdx"></param>
            /// <param name="cellIdx"></param>
            public void EnableDependency(in int gridIdx, in int cellIdx)
            {
                if (HasDependency) throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
                    $"{Idxes} 그리드셀은 Dependency 가 이미 있는데 또 추가하려함. 먼저 DisableDependency()을 호출하여 초기화하세요.");

                ref Grid grid = ref GetGrid(gridIdx);
                ref GridCell cell = ref grid.GetCell(cellIdx);

                InternalEnableDependency(ref this, ref grid, ref cell);
            }
            public void EnableDependency(in int2 idxes)
            {
                if (HasDependency) throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
                    $"{Idxes} 그리드셀은 Dependency 가 이미 있는데 또 추가하려함. 먼저 DisableDependency()을 호출하여 초기화하세요.");

                ref Grid grid = ref GetGrid(idxes.x);
                ref GridCell cell = ref grid.GetCell(idxes.y);

                InternalEnableDependency(ref this, ref grid, ref cell);
            }
            public void EnableDependency(in int gridIdx, in Vector2Int location)
            {
                if (HasDependency) throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
                    $"{Idxes} 그리드셀은 Dependency 가 이미 있는데 또 추가하려함. 먼저 DisableDependency()을 호출하여 초기화하세요.");

                ref Grid grid = ref GetGrid(gridIdx);
                ref GridCell cell = ref grid.GetCell(location);

                InternalEnableDependency(ref this, ref grid, ref cell);
            }
            public void EnableDependency(in int gridIdx, in int x, in int y)
            {
                if (HasDependency) throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
                    $"{Idxes} 그리드셀은 Dependency 가 이미 있는데 또 추가하려함. 먼저 DisableDependency()을 호출하여 초기화하세요.");

                ref Grid grid = ref GetGrid(gridIdx);
                ref GridCell cell = ref grid.GetCell(x, y);

                InternalEnableDependency(ref this, ref grid, ref cell);
            }
            public void DisableDependency()
            {
                if (!HasDependency) throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
                    $"{Idxes} 그리드셀은 루트 셀이 없는데 삭제하려함");

                //ref GridCell cell = ref GetDependencyTarget();

                List<int2> temp;
#if UNITY_EDITOR
                if (IsMainthread() && !Application.isPlaying)
                {
                    temp = s_EditorCellDependency[DependencyTarget];
                }
                else
#endif
                {
                    temp = Instance.m_CellDependency[DependencyTarget];
                }
                lock (s_LockDependency)
                {
                    for (int i = 0; i < temp.Count; i++)
                    {
                        if (temp[i].Equals(Idxes))
                        {
                            temp.RemoveAt(i);
                            i--;
                            continue;
                        }
                    }
                }

                HasDependency = false;
                DependencyTarget = -1;
                SetDirty();
            }
            public void DisableAllChildsDependency()
            {
                if (!IsRoot) throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
                    $"{Idxes} 그리드셀은 부모가 아닌데 자식 dependency를 삭제하려함");

                List<int2> targetList;
#if UNITY_EDITOR
                if (IsMainthread() && !Application.isPlaying)
                {
                    targetList = s_EditorCellDependency[Idxes];
                    
                    for (int i = targetList.Count - 1; i >= 0; i--)
                    {
                        ref Grid grid = ref GetGrid(targetList[i].x);
                        ref GridCell targetCell = ref grid.GetCell(targetList[i].y);
                        targetCell.DisableDependency();
                    }

                    s_EditorCellDependency.Remove(Idxes);
                }
                else
#endif
                {
                    targetList = Instance.m_CellDependency[Idxes];

                    //targetList = Instance.m_CellDependency[Idxes];
                    for (int i = targetList.Count - 1; i >= 0; i--)
                    {
                        ref Grid grid = ref GetGrid(targetList[i].x);
                        ref GridCell targetCell = ref grid.GetCell(targetList[i].y);
                        targetCell.DisableDependency();
                    }

                    if (!Instance.m_CellDependency.TryRemove(Idxes, out targetList))
                    {
                        throw new Exception("Thread overloaded?");
                    }
                }

                SetDirty();
            }

            #endregion

            public void SetDirty()
            {
#if UNITY_EDITOR
                if (IsMainthread() && !Application.isPlaying) return;
#endif
                if (HasDependency)
                {
                    ref GridCell cell = ref GetDependencyTarget();
                    cell.SetDirty();
                }
                else
                {
                    Instance.m_DirtyFlags.Enqueue(Idxes);
                    Instance.m_DirtyFlagsAsync.Enqueue(Idxes);

                    List<int2> temp;
#if UNITY_EDITOR
                    if (IsMainthread() && !Application.isPlaying)
                    {
                        if (s_EditorCellDependency.ContainsKey(Idxes))
                        {
                            temp = s_EditorCellDependency[Idxes];
                        }
                        else temp = null;
                    }
                    else
#endif
                    {
                        if (Instance.m_CellDependency.ContainsKey(Idxes))
                        {
                            temp = Instance.m_CellDependency[Idxes];
                        }
                        else temp = null;
                    }

                    if (temp != null)
                    {
                        for (int i = 0; i < temp.Count; i++)
                        {
                            Instance.m_DirtyFlags.Enqueue(temp[i]);
                            Instance.m_DirtyFlagsAsync.Enqueue(temp[i]);
                        }
                    }
                }
            }

            public void Dispose() { }

            private static void InternalEnableDependency(ref GridCell other, ref Grid grid, ref GridCell cell)
            {
                if (cell.HasDependency)
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
                        $"자식 셀({cell.Idxes}::{cell.Location})은 부모로 삼을 수 없습니다.");
                }

#if UNITY_EDITOR
                if (IsMainthread() && !Application.isPlaying)
                {
                    if (!s_EditorCellDependency.ContainsKey(cell.Idxes))
                    {
                        s_EditorCellDependency.Add(cell.Idxes, new List<int2>());
                    }

                    s_EditorCellDependency[cell.Idxes].Add(other.Idxes);
                }
                else
#endif
                {
                    lock (s_LockDependency)
                    {
                        if (!Instance.m_CellDependency.ContainsKey(cell.Idxes))
                        {
                            Instance.m_CellDependency.TryAdd(cell.Idxes, new List<int2>());
                        }
                    }

                    Instance.m_CellDependency[cell.Idxes].Add(other.Idxes);
                }

                other.DependencyTarget = cell.Idxes;
                other.HasDependency = true;

                cell.SetDirty();
            }

#if CORESYSTEM_UNSAFE
            public static GridCell FromPointer(IntPtr intPtr) => (GridCell)GCHandle.FromIntPtr(intPtr).Target;
#endif
        }

        public struct GridRange : IDisposable
        {
            private int[] m_Targets;

            public int Length => m_Targets.Length;
#if CORESYSTEM_UNSAFE_INTERNAL
            unsafe private GridCell* m_Pointer;

            unsafe internal GridRange(GridCell* pointer, params int[] targets)
            {
                m_Pointer = pointer;
                m_Targets = targets;
            }
            unsafe public GridRange(GridCell* pointer, int length)
            {
                m_Targets = new int[length];
                m_Pointer = pointer;
            }

            unsafe public GridCell* this[int i]
            {
                get
                {
                    return m_Pointer + m_Targets[i];
                }
            }

            public void AddAt(int i, int cellIdx)
            {
                m_Targets[i] = cellIdx;
            }
#else
            private int m_GridIdx;
            internal GridRange(int gridIdx, params int[] targets)
            {
                m_GridIdx = gridIdx;
                m_Targets = targets;
            }
            public ref GridCell this[int i]
            {
                get
                {
                    return ref GridManager.GetGrid(m_GridIdx).GetCell(m_Targets[i]);
                }
            }
#endif
            public void Dispose() { }
        }

        public override void OnInitialize()
        {
#if UNITY_EDITOR
            ClearEditorGrids();
#endif
            m_NavMeshQuery = new NavMeshQuery(NavMeshWorld.GetDefaultWorld(), Allocator.Persistent, 256);

            StartUnityUpdate(UnityUpdate());
            StartBackgroundUpdate(BackgroundUpdate());
        }

        private void OnDestroy()
        {
            m_NavMeshQuery.Dispose();

            ClearGrids();
        }
        private IEnumerator UnityUpdate()
        {
            while (true)
            {
                if (m_DirtyFlags.Count > 0)
                {
                    int dirtyCount = m_DirtyFlags.Count;
                    for (int i = 0; i < dirtyCount; i++)
                    {
                        if (!m_DirtyFlags.TryDequeue(out int2 idxes) ||
                            !m_OnDirtyFlagRaised.ContainsKey(idxes.x)) continue;

                        ExecuteDirtyFlag(idxes, m_OnDirtyFlagRaised[idxes.x]);
                    }
                }

                yield return null;
            }
        }
        private IEnumerator BackgroundUpdate()
        {
            while (true)
            {
                if (m_DirtyFlagsAsync.Count > 0)
                {
                    int dirtyCount = m_DirtyFlagsAsync.Count;
                    for (int i = 0; i < dirtyCount; i++)
                    {
                        if (!m_DirtyFlagsAsync.TryDequeue(out int2 idxes) ||
                            !m_OnDirtyFlagRaisedAsync.ContainsKey(idxes.x)) continue;

                        ExecuteDirtyFlag(idxes, m_OnDirtyFlagRaisedAsync[idxes.x]);
                    }
                }

                yield return null;
            }
        }
        private void ExecuteDirtyFlag(int2 idxes, GridLambdaWriteAllDescription<Grid, GridCell> gridLambda)
        {
            ref Grid grid = ref GetGrid(idxes.x);
            ref GridCell cell = ref grid.GetCell(idxes.y);
            gridLambda.Invoke(ref grid, ref cell);
        }
        private void OnRenderObject()
        {
            GLSetMaterial();
            for (int i = 0; i < m_Grids.Length; i++)
            {
                ref Grid grid = ref m_Grids[i];
                if (!grid.EnableDrawGL) continue;

                for (int a = 0; a < grid.Length; a++)
                {
                    ref var cell = ref grid.GetCell(a);
                    if (!RenderManager.IsInCameraScreen(cell.Bounds.center)) continue;

                    if (!cell.Enabled || cell.BlockedByNavMesh)
                    {
                        GLDrawPlane(cell.Bounds.center, new Vector2(cell.Bounds.size.x, cell.Bounds.size.z), in DisableColor, true);
                    }
                    else
                    {
                        GLDrawPlane(cell.Bounds.center, new Vector2(cell.Bounds.size.x, cell.Bounds.size.z),
                            cell.Highlighted ? HighlightColor : NormalColor, true);
                    }
                }
            }
        }
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            for (int i = 0; i < s_EditorGrids.Length; i++)
            {
                ref Grid grid = ref s_EditorGrids[i];
                if (!grid.EnableDrawGL) continue;

                int drawIdxCount = 0;
                for (int a = 0; a < grid.Length; a++)
                {
                    ref var cell = ref grid.GetCell(a);
                    if (!cell.IsVisible())
                    {
                        continue;
                    }

                    Gizmos.color = cell.Color;
                    Gizmos.DrawCube(cell.Bounds.center, new Vector3(cell.Bounds.size.x, .1f, cell.Bounds.size.z));
                    Gizmos.DrawWireCube(cell.Bounds.center, new Vector3(cell.Bounds.size.x, .1f, cell.Bounds.size.z));

                    if (grid.EnableDrawIdx)
                    {
                        if (drawIdxCount > 300) continue;

                        string locTxt = $"{cell.Idx}:({cell.Location.x},{cell.Location.y})";
                        if (Application.isPlaying)
                        {
                            if (m_CellDependency.ContainsKey(cell.Idxes))
                            {
                                locTxt += $"{m_CellDependency[cell.Idxes].Count}";
                            }
                        }
                        else
                        {
                            if (s_EditorCellDependency.ContainsKey(cell.Idxes))
                            {
                                locTxt += $"\n{s_EditorCellDependency[cell.Idxes].Count}";
                            }
                        }

                        Handles.Label(cell.Bounds.center, locTxt);
                        drawIdxCount++;
                    }
                }
            }
        }
#endif

        #endregion

        #region Grid Methods
        public static int ClearGrids()
        {
            int count = 0;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                count += ClearEditorGrids();
            }
#endif
            count += Instance.m_Grids.Length;
            if (count == 0) return count;

            for (int i = 0; i < Instance.m_Grids.Length; i++)
            {
                Instance.m_Grids[i].Dispose();
            }
            Instance.m_Grids = new Grid[0];
            Instance.m_GridObjects.Clear();
            Instance.m_CellObjects.Clear();
            Instance.m_CellDependency.Clear();

            return count;
        }
#if UNITY_EDITOR
        public static int ClearEditorGrids()
        {
            int count = s_EditorGrids.Length;
            if (count == 0) return count;

            for (int i = 0; i < s_EditorGrids.Length; i++)
            {
                s_EditorGrids[i].Dispose();
            }
            s_EditorGrids = new Grid[0];
            s_EditorGridObjects.Clear();
            s_EditorCellObjects.Clear();
            s_EditorCellDependency.Clear();
            return count;
        }
#endif

        public static int Length => Instance.m_Grids.Length;

        public static bool HasGrid(in Guid guid)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                for (int i = 0; i < s_EditorGrids.Length; i++)
                {
                    if (s_EditorGrids[i].Guid.Equals(guid)) return true;
                }
            }
            else
#endif
            {
                for (int i = 0; i < Instance.m_Grids.Length; i++)
                {
                    if (Instance.m_Grids[i].Guid.Equals(guid)) return true;
                }
            }
            return false;
        }
        public static bool HasGrid(in int idx)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                for (int i = 0; i < s_EditorGrids.Length; i++)
                {
                    if (s_EditorGrids[i].Idx.Equals(idx)) return true;
                }
            }
            else
#endif
            {
                for (int i = 0; i < Instance.m_Grids.Length; i++)
                {
                    if (Instance.m_Grids[i].Idx.Equals(idx)) return true;
                }
            }
            return false;
        }
        public static bool HasGrid(in Vector3 worldPosition)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                for (int i = 0; i < s_EditorGrids.Length; i++)
                {
                    if (s_EditorGrids[i].Bounds.Contains(worldPosition)) return true;
                }
            }
            else
#endif
            {
                for (int i = 0; i < Instance.m_Grids.Length; i++)
                {
                    if (Instance.m_Grids[i].Bounds.Contains(worldPosition)) return true;
                }
            }
            return false;
        }

        public static ref Grid GetGrid(in Guid guid)
        {
#if UNITY_EDITOR
            if (IsMainthread() && !Application.isPlaying)
            {
                for (int i = 0; i < s_EditorGrids.Length; i++)
                {
                    if (s_EditorGrids[i].Guid.Equals(guid)) return ref s_EditorGrids[i];
                }
            }
            else
#endif
            {
                for (int i = 0; i < Instance.m_Grids.Length; i++)
                {
                    if (Instance.m_Grids[i].Guid.Equals(guid)) return ref Instance.m_Grids[i];
                }
            }

            throw new CoreSystemException(CoreSystemExceptionFlag.Mono, $"Guid ({guid}) 그리드를 찾을 수 없음");
        }
        public static ref Grid GetGrid(in int idx)
        {
#if UNITY_EDITOR
            if (IsMainthread() && !Application.isPlaying)
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
        public static ref Grid GetGrid(in Vector3 worldPosition)
        {
#if UNITY_EDITOR
            if (IsMainthread() && !Application.isPlaying)
            {
                for (int i = 0; i < s_EditorGrids.Length; i++)
                {
                    if (s_EditorGrids[i].Bounds.Contains(worldPosition)) return ref s_EditorGrids[i];
                }
            }
            else
#endif
            {
                for (int i = 0; i < Instance.m_Grids.Length; i++)
                {
                    if (Instance.m_Grids[i].Bounds.Contains(worldPosition)) return ref Instance.m_Grids[i];
                }
            }

            throw new CoreSystemException(CoreSystemExceptionFlag.Mono, $"포지션 ({worldPosition}) 그리드를 찾을 수 없음");
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
                lock (s_LockManager)
                {
                    newGrids = new List<Grid>(Instance.m_Grids);
                    grid = InternalCreateGrid(newGrids.Count, in bounds, in gridCellSize, in enableNavMesh);

                    newGrids.Add(grid);

                    Instance.m_Grids = newGrids.ToArray();
                }
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
                lock (s_LockManager)
                {
                    newGrids = new List<Grid>(Instance.m_Grids);
                    grid = InternalCreateGrid(newGrids.Count, mesh.bounds, in gridCellSize, in enableNavMesh);

                    newGrids.Add(grid);

                    Instance.m_Grids = newGrids.ToArray();
                }
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
                lock (s_LockManager)
                {
                    newGrids = new List<Grid>(Instance.m_Grids);
                    grid = InternalCreateGrid(newGrids.Count, terrain.terrainData.bounds, in gridCellSize, in enableNavMesh);

                    newGrids.Add(grid);

                    Instance.m_Grids = newGrids.ToArray();
                }
            }
            return grid.Idx;
        }
        public static void UpdateGrid(in int idx, in Bounds bounds, in float gridCellSize, in bool enableNavMesh, in bool drawGL = false, in bool drawIdx = false)
        {
            Grid newGrid = InternalCreateGrid(in idx, in bounds, in gridCellSize, in enableNavMesh);
            newGrid.EnableDrawGL = drawGL;
            newGrid.EnableDrawIdx = drawIdx;
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

        private static Grid InternalCreateGrid(in int parentIdx, in Bounds bounds, in float gridCellSize, in bool enableNavMesh)
        {
            int xSize = Mathf.FloorToInt(bounds.size.x / gridCellSize);
            int zSize = Mathf.FloorToInt(bounds.size.z / gridCellSize);

            float halfSize = gridCellSize / 2;
            Vector3 cellSize = new Vector3(gridCellSize, bounds.size.y, gridCellSize);

            int count = 0;
            GridCell[] cells = new GridCell[xSize * zSize];
            for (int i = 0; i < zSize; i++)
            {
                for (int j = 0; j < xSize; j++)
                {
                    Vector3 center = new Vector3(
                        bounds.min.x + halfSize + (gridCellSize * j), bounds.center.y,
                        bounds.max.z - halfSize - (gridCellSize * i));

                    cells[count] = new GridCell(parentIdx, count, new int2(j, i), new Bounds(center, cellSize));
                    count++;
                }
            }

            return new Grid(parentIdx, 
                new int3(bounds.center), 
                new int3(xSize, Mathf.RoundToInt(bounds.size.y), zSize), 
                bounds,
                gridCellSize, enableNavMesh, cells);
        }
    }
}
