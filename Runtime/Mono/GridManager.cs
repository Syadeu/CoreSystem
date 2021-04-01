﻿//#undef CORESYSTEM_UNSAFE

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;
using Unity.Collections;
using Unity.Mathematics;

using Syadeu;
using Syadeu.Database;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEditorInternal;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Syadeu.Mono
{
    [DisallowMultipleComponent]
    [StaticManagerIntializeOnLoad]
    public class GridManager : StaticManager<GridManager>
    {
        private static readonly object s_LockManager = new object();
        private static readonly object s_LockCell = new object();

        #region Init
        public override bool HideInHierarchy => false;

        private Grid[] m_Grids = new Grid[0];
        private Dictionary<int2, object> m_CellObjects = new Dictionary<int2, object>();
        private Dictionary<int2, List<int2>> m_CellDependency = new Dictionary<int2, List<int2>>();
        private NavMeshQuery m_NavMeshQuery;

#if UNITY_EDITOR
        public static Grid[] s_EditorGrids = new Grid[0];
        public static Dictionary<int2, object> s_EditorCellObjects = new Dictionary<int2, object>();
        public static Dictionary<int2, List<int2>> s_EditorCellDependency = new Dictionary<int2, List<int2>>();
#endif

        public Camera RenderCameraTarget
        {
            get => RenderManager.Instance.m_MainCamera.Value;
            set => RenderManager.SetCamera(value);
        }
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

                CustomData = grid.CustomData;

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

#if CORESYSTEM_UNSAFE
            unsafe internal GridCell* Cells;
#else
            internal GridCell[] Cells;
#endif
            public float CellSize;

            internal object CustomData;

            public bool EnableNavMesh;
            public bool EnableDrawGL;
            public bool EnableDrawIdx;

#if CORESYSTEM_UNSAFE
            public int Length;
#else
            public int Length => Cells.Length;
#endif

            internal Grid(int idx, int3 gridCenter, int3 gridSize, float cellSize, bool enableNavMesh, params GridCell[] cells)
            {
                Guid = Guid.NewGuid();
                Idx = idx;

                Bounds = new Bounds(
                    new Vector3(gridCenter.x, gridCenter.y, gridCenter.z),
                    new Vector3(gridSize.x, gridSize.y, gridSize.z));
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

                CustomData = null;

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
                    convertedCells[i] = new GridCell(in cells[i], grid.EnableNavMesh);
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

                CustomData = grid.CustomData;

                EnableNavMesh = grid.EnableNavMesh;
                EnableDrawGL = false;
                EnableDrawIdx = false;
            }

            public bool IsValid() => HasGrid(in Guid);
            public bool Equals(Grid other) => Guid.Equals(other.Guid);

            #endregion

            #region Has

            public bool HasCell(int idx) => idx >= 0 && Length > idx;
            public bool HasCell(Vector2Int location)
            {
                int idx = (GridSize.z * location.y) + location.x;
                if (idx >= Length) return false;
                return true;
            }
            public bool HasCell(int2 location)
            {
                int idx = (GridSize.z * location.y) + location.x;
                if (idx >= Length) return false;
                return true;
            }
            public bool HasCell(int x, int y)
            {
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
                int idx = (GridSize.z * location.y) + location.x;
                if (idx >= Length) throw new CoreSystemException(CoreSystemExceptionFlag.Mono, $"Out of Range({location.x},{location.y}). " +
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
                int idx = (GridSize.z * location.y) + location.x;
                if (idx >= Length) throw new CoreSystemException(CoreSystemExceptionFlag.Mono, $"Out of Range({location.x},{location.y}). " +
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
                int idx = (GridSize.z * y) + x;
                if (idx >= Length) throw new CoreSystemException(CoreSystemExceptionFlag.Mono, $"Out of Range({x},{y}). " +
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
            public void RemoveCustomData()
            {
                CustomData = null;
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

            public int2 Location;
            public Bounds Bounds;

            internal bool HasDependency;
            internal int2 DependencyTarget;

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

            public bool Enabled;
            public bool Highlighted;

            public Color Color
            {
                get
                {
                    if (HasDependency)
                    {
                        ref Grid grid = ref GetGrid(DependencyTarget.x);
                        ref GridCell cell = ref grid.GetCell(DependencyTarget.y);
                        return cell.Color;
                    }

                    if (BlockedByNavMesh || !Enabled)
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
                            NavMesh.SamplePosition(NavMeshVerties[i], out NavMeshHit hit, .25f, -1);
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

            internal GridCell(int parentIdx, int idx, int2 location, Bounds bounds, bool enableNavMesh)
            {
                Idxes = new int2(parentIdx, idx);
                ParentIdx = parentIdx;
                Idx = idx;

                Location = location;
                Bounds = bounds;

                HasDependency = false;
                DependencyTarget = int2.zero;
                //DependencyChilds = null;
                //CustomData = null;

                //Verties = new float3[4]
                //{
                //    new float3(bounds.min.x, bounds.min.y, bounds.min.z),
                //    new float3(bounds.min.x, bounds.min.y, bounds.max.z),
                //    new float3(bounds.max.x, bounds.min.y, bounds.max.z),
                //    new float3(bounds.max.x, bounds.min.y, bounds.min.z)
                //};

                Enabled = true;
                Highlighted = false;

                //NormalColor = new Color(1, 1, 1, .1f);
                //HighlightColor = new Color { g = 1, a = .1f };
                //DisableColor = new Color { r = 1, a = .1f };

                //if (enableNavMesh)
                //{
                //    NavMeshVerties = new float3[]
                //    {
                //        bounds.center,
                //        new float3(Bounds.center.x + Bounds.extents.x - .1f, Bounds.center.y, Bounds.center.z),
                //        new float3(Bounds.center.x - Bounds.extents.x + .1f, Bounds.center.y, Bounds.center.z),
                //        new float3(Bounds.center.x, Bounds.center.y, Bounds.center.z + Bounds.extents.z - .1f),
                //        new float3(Bounds.center.x, Bounds.center.y, Bounds.center.z - Bounds.extents.z + .1f)
                //    };
                //}
                //else NavMeshVerties = null;
            }
            internal GridCell(in BinaryGridCell cell, bool enableNavMesh) : this(cell.ParentIdx, cell.Idx, cell.Location, new Bounds(cell.Bounds_Center, cell.Bounds_Size), enableNavMesh)
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
                    else Instance.m_CellObjects.Add(loc, cell.CustomData);

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
                            Instance.m_CellDependency.Add(loc, new List<int2>());
                        }
                        else Instance.m_CellDependency.Add(loc, cell.DependencyChilds.ToList());
                    }
                }
            }

            public bool IsValid() => Verties != null;
            public bool Equals(GridCell other) => Location.Equals(other.Location);
            public bool IsVisable()
            {
                for (int i = 0; i < Verties.Length; i++)
                {
                    if (RenderManager.Instance.IsInCameraScreen(/*Instance.RenderCameraTarget, */Verties[i])) return true;
                }
                return false;
            }
            #endregion

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
                            Instance.m_CellObjects.Add(DependencyTarget, data);
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
                            Instance.m_CellObjects.Add(Idxes, data);
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
                        Instance.m_CellObjects.Remove(DependencyTarget);
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
                        Instance.m_CellObjects.Remove(Idxes);
                    }
                }
            }
            public void MoveCustomData(int2 gridNCellIdxes) => MoveCustomData(gridNCellIdxes.x, gridNCellIdxes.y);
            public void MoveCustomData(int gridIdx, int cellIdx)
            {
                object data = GetCustomData();
                RemoveCustomData();

                GetGrid(gridIdx).GetCell(cellIdx).InternalSetCustomData(data);
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
                    $"{Idxes} 그리드셀은 Dependency 가 없는데 삭제하려함");

                ref Grid grid = ref GetGrid(DependencyTarget.x);
                ref GridCell cell = ref grid.GetCell(DependencyTarget.y);

                lock (s_LockCell)
                {
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

                    for (int i = 0; i < temp.Count; i++)
                    {
                        if (temp[i].Equals(Idxes))
                        {
                            temp.RemoveAt(i);
                            break;
                        }
                    }
                }

                HasDependency = false;
            }

            public void SetDirty()
            {
                if (IsMainthread() && !Application.isPlaying) return;

                if (HasDependency)
                {
                    ref Grid grid = ref GetGrid(DependencyTarget.x);
                    ref GridCell cell = ref grid.GetCell(DependencyTarget.y);

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
                        if (s_EditorCellDependency.ContainsKey(DependencyTarget))
                        {
                            temp = s_EditorCellDependency[DependencyTarget];
                        }
                        else temp = null;
                    }
                    else
#endif
                    {
                        if (Instance.m_CellDependency.ContainsKey(DependencyTarget))
                        {
                            temp = Instance.m_CellDependency[DependencyTarget];
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
                lock (s_LockCell)
                {
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
                        if (!Instance.m_CellDependency.ContainsKey(cell.Idxes))
                        {
                            Instance.m_CellDependency.Add(cell.Idxes, new List<int2>());
                        }

                        Instance.m_CellDependency[cell.Idxes].Add(other.Idxes);
                    }
                }

                other.HasDependency = true;
                other.DependencyTarget = new int2(grid.Idx, cell.Idx);
            }
        }

        public override void OnInitialize()
        {
            m_NavMeshQuery = new NavMeshQuery(NavMeshWorld.GetDefaultWorld(), Allocator.Persistent, 256);

            StartUnityUpdate(UnityUpdate());
            StartBackgroundUpdate(BackgroundUpdate());
        }
        private void OnDestroy()
        {
            m_NavMeshQuery.Dispose();
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
                    if (!RenderManager.Instance.IsInCameraScreen(cell.Bounds.center)) continue;

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

                for (int a = 0; a < grid.Length; a++)
                {
                    ref var cell = ref grid.GetCell(a);

                    Gizmos.color = cell.Color;
                    Gizmos.DrawCube(cell.Bounds.center, new Vector3(cell.Bounds.size.x, .1f, cell.Bounds.size.z));
                    Gizmos.DrawWireCube(cell.Bounds.center, new Vector3(cell.Bounds.size.x, .1f, cell.Bounds.size.z));

                    if (grid.EnableDrawIdx)
                    {
                        string locTxt = $"{cell.Idx}:({cell.Location.x},{cell.Location.y})";
                        //if (Application.isPlaying)
                        //{
                        //    locTxt += $"{m_CellDependency[cell.Idxes].Count}";
                        //}
                        //else
                        {
                            locTxt += $"\n{s_EditorCellDependency[cell.Idxes].Count}";
                        }
                        Handles.Label(cell.Bounds.center, locTxt);
                    }
                }
            }
        }
#endif

        #endregion

        #region Grid Methods
        public static int ClearGrids()
        {
            int count;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                count = s_EditorGrids.Length;
                for (int i = 0; i < s_EditorGrids.Length; i++)
                {
                    s_EditorGrids[i].Dispose();
                }
                s_EditorGrids = new Grid[0];
                s_EditorCellObjects.Clear();
                s_EditorCellDependency.Clear();
            }
            else
#endif
            {
                count = Instance.m_Grids.Length;
                for (int i = 0; i < Instance.m_Grids.Length; i++)
                {
                    Instance.m_Grids[i].Dispose();
                }
                Instance.m_Grids = new Grid[0];
                Instance.m_CellObjects.Clear();
                Instance.m_CellDependency.Clear();
            }

            return count;
        }
#if UNITY_EDITOR
        public static int ClearEditorGrids()
        {
            int count = s_EditorGrids.Length;
            for (int i = 0; i < s_EditorGrids.Length; i++)
            {
                s_EditorGrids[i].Dispose();
            }
            s_EditorGrids = new Grid[0];
            s_EditorCellObjects.Clear();
            s_EditorCellDependency.Clear();
            return count;
        }
#endif

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

                    cells[count] = new GridCell(parentIdx, count, new int2(j, i), new Bounds(center, cellSize), enableNavMesh);
                    count++;
                }
            }

            return new Grid(parentIdx, 
                new int3(bounds.center), 
                new int3(xSize, Mathf.RoundToInt(bounds.size.y), zSize), 
                gridCellSize, enableNavMesh, cells);
        }
    }
}
