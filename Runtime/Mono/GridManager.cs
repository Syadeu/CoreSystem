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
using Syadeu.Extensions.Logs;

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

        #region Init
        public override bool HideInHierarchy => false;

        private Grid[] m_Grids = new Grid[0];
        private NavMeshQuery m_NavMeshQuery;

#if UNITY_EDITOR
        public static Grid[] s_EditorGrids = new Grid[0];
#endif

        public Camera RenderCameraTarget
        {
            get => RenderManager.Instance.m_MainCamera.Value;
            set => RenderManager.SetCamera(value);
        }
        private readonly ConcurrentQueue<int2> m_DirtyFlags = new ConcurrentQueue<int2>();
        private readonly ConcurrentQueue<int2> m_DirtyFlagsAsync = new ConcurrentQueue<int2>();
        private readonly ConcurrentDictionary<int, GridLambdaDescription<Grid, GridCell>> m_OnDirtyFlagRaised = new ConcurrentDictionary<int, GridLambdaDescription<Grid, GridCell>>();
        private readonly ConcurrentDictionary<int, GridLambdaDescription<Grid, GridCell>> m_OnDirtyFlagRaisedAsync = new ConcurrentDictionary<int, GridLambdaDescription<Grid, GridCell>>();

        public delegate void GridRWAllTagLambdaDescription(in int i, ref GridCell gridCell, in UserTagFlag userTag, in CustomTagFlag customTag);
        public delegate void GridRWUserTagLambdaDescription(in int i, ref GridCell gridCell, in UserTagFlag userTag);
        public delegate void GridRWCustomTagLambdaDescription(in int i, ref GridCell gridCell, in CustomTagFlag customTag);
        public delegate void GridRWLambdaDescription(in int i, ref GridCell gridCell);
        public delegate void GridLambdaDescription<in T, in TA>(T t, TA ta);

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
        internal struct BinaryGridCell
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

            public BinaryGridCell(in GridCell gridCell)
            {
                ParentIdx = gridCell.ParentIdx;
                Idx = gridCell.Idx;

                Location = gridCell.Location;
                Bounds_Center = gridCell.Bounds.center;
                Bounds_Size = gridCell.Bounds.size;

                HasDependency = gridCell.HasDependency;
                DependencyTarget = gridCell.DependencyTarget;
                DependencyChilds = gridCell.DependencyChilds;
                CustomData = gridCell.CustomData;
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

            internal GridCell[] Cells;
            public float CellSize;

            internal object CustomData;

            public readonly bool EnableNavMesh;
            public bool EnableDrawGL;
            public bool EnableDrawIdx;

            public int Length => Cells.Length;

            internal Grid(int idx, int3 gridCenter, int3 gridSize, float cellSize, bool enableNavMesh, params GridCell[] cells)
            {
                Guid = Guid.NewGuid();
                Idx = idx;

                Bounds = new Bounds(
                    new Vector3(gridCenter.x, gridCenter.y, gridCenter.z),
                    new Vector3(gridSize.x, gridSize.y, gridSize.z));
                GridCenter = gridCenter;
                GridSize = gridSize;

                Cells = cells;
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
                Cells = convertedCells;
                CellSize = grid.CellSize;

                CustomData = grid.CustomData;

                EnableNavMesh = grid.EnableNavMesh;
                EnableDrawGL = false;
                EnableDrawIdx = false;
            }

            public bool IsValid() => HasGrid(in Guid);
            public bool Equals(Grid other) => Guid.Equals(other.Guid);

            #endregion

            public bool HasCell(int idx) => idx >= 0 && Cells.Length > idx;
            public bool HasCell(Vector2Int grid)
            {
                for (int i = 0; i < Cells.Length; i++)
                {
                    if (Cells[i].Location.Equals(grid)) return true;
                }
                return false;
            }
            public bool HasCell(Vector3 worldPosition)
            {
                for (int i = 0; i < Cells.Length; i++)
                {
                    if (Cells[i].Bounds.Contains(worldPosition)) return true;
                }
                return false;
            }

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
            public ref GridCell GetCell(int x, int y)
            {
                for (int i = 0; i < Cells.Length; i++)
                {
                    if (Cells[i].Location.x.Equals(x) &&
                        Cells[i].Location.y.Equals(y)) return ref Cells[i];
                }

                throw new CoreSystemException(CoreSystemExceptionFlag.Mono, $"Out of Range({x},{y}). " +
                    $"해당 좌표계는 이 그리드에 존재하지않습니다.");
            }
            public ref GridCell GetCell(Vector3 worldPosition)
            {
                for (int i = 0; i < Cells.Length; i++)
                {
                    if (Cells[i].Bounds.Contains(worldPosition)) return ref Cells[i];
                }

                throw new CoreSystemException(CoreSystemExceptionFlag.Mono, $"Out of Range({worldPosition.x},{worldPosition.y},{worldPosition.z}). " +
                    $"해당 좌표계는 이 그리드에 존재하지않습니다.");
            }
            public BackgroundJob GetCellAsync(Vector3 worldPosition, NativeArray<int> idx, int chunkSize = 2024)
            {
                return BackgroundJob.ParallelFor(Cells, (i, cell) =>
                {
                    if (cell.Bounds.Contains(worldPosition))
                    {
                        idx[0] = i;
                        return true;
                    }
                    return false;
                }, chunkSize);
            }
            public BackgroundJob GetCellAsync(Vector2Int grid, NativeArray<int> idx, int chunkSize = 2024)
            {
                return BackgroundJob.ParallelFor(Cells, (i, cell) =>
                {
                    if (cell.Location.Equals(grid))
                    {
                        idx[0] = i;
                        return true;
                    }
                    return false;
                }, chunkSize);
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
            public readonly GridCell[] GetCells() => Cells;

            #endregion

            #region Lambda Descriptions

            public void For(GridLambdaDescription<int, GridCell> lambdaDescription)
            {
                for (int i = 0; i < Cells.Length; i++)
                {
                    lambdaDescription.Invoke(i, Cells[i]);
                }
            }
            public void For<T>(GridLambdaDescription<int, GridCell> lambdaDescription) where T : struct, ITag
            {
                for (int i = 0; i < Cells.Length; i++)
                {
                    if (!Cells[i].GetCustomData(out T _)) continue;
                    lambdaDescription.Invoke(i, Cells[i]);
                }
            }
            public void For(GridRWLambdaDescription lambdaDescription)
            {
                for (int i = 0; i < Cells.Length; i++)
                {
                    lambdaDescription.Invoke(in i, ref Cells[i]);
                }
            }
            public void For<T>(GridRWLambdaDescription lambdaDescription) where T : struct, ITag
            {
                for (int i = 0; i < Cells.Length; i++)
                {
                    if (!Cells[i].GetCustomData(out T _)) continue;
                    lambdaDescription.Invoke(in i, ref Cells[i]);
                }
            }
            public void For(GridRWUserTagLambdaDescription lambdaDescription)
            {
                for (int i = 0; i < Cells.Length; i++)
                {
                    if (!Cells[i].GetCustomData(out ITag tag))
                    {
                        continue;
                    }

                    lambdaDescription.Invoke(in i, ref Cells[i], tag.UserTag);
                }
            }
            public void For<T>(GridRWUserTagLambdaDescription lambdaDescription) where T : struct, ITag
            {
                for (int i = 0; i < Cells.Length; i++)
                {
                    if (!Cells[i].GetCustomData(out T data))
                    {
                        continue;
                    }

                    lambdaDescription.Invoke(in i, ref Cells[i], data.UserTag);
                }
            }
            public void For(GridRWCustomTagLambdaDescription lambdaDescription)
            {
                for (int i = 0; i < Cells.Length; i++)
                {
                    if (!Cells[i].GetCustomData(out ITag tag))
                    {
                        continue;
                    }

                    lambdaDescription.Invoke(in i, ref Cells[i], tag.CustomTag);
                }
            }
            public void For<T>(GridRWCustomTagLambdaDescription lambdaDescription) where T : struct, ITag
            {
                for (int i = 0; i < Cells.Length; i++)
                {
                    if (!Cells[i].GetCustomData(out T data))
                    {
                        continue;
                    }

                    lambdaDescription.Invoke(in i, ref Cells[i], data.CustomTag);
                }
            }
            public void For(GridRWAllTagLambdaDescription lambdaDescription)
            {
                for (int i = 0; i < Cells.Length; i++)
                {
                    if (!Cells[i].GetCustomData(out ITag tag))
                    {
                        continue;
                    }

                    lambdaDescription.Invoke(in i, ref Cells[i], tag.UserTag, tag.CustomTag);
                }
            }
            public void For<T>(GridRWAllTagLambdaDescription lambdaDescription) where T : struct, ITag
            {
                for (int i = 0; i < Cells.Length; i++)
                {
                    if (!Cells[i].GetCustomData(out T data))
                    {
                        continue;
                    }

                    lambdaDescription.Invoke(in i, ref Cells[i], data.UserTag, data.CustomTag);
                }
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

            public void OnDirtyMarked(GridLambdaDescription<Grid, GridCell> async)
            {
                Instance.m_OnDirtyFlagRaised.TryAdd(Idx, async);
            }
            public void OnDirtyMarkedAsync(GridLambdaDescription<Grid, GridCell> async)
            {
                Instance.m_OnDirtyFlagRaisedAsync.TryAdd(Idx, async);
            }

            public void Dispose()
            {
                for (int i = 0; i < Cells.Length; i++)
                {
                    Cells[i].Dispose();
                }
            }
        }
        [Serializable]
        public struct GridCell : IValidation, IEquatable<GridCell>, IDisposable
        {
            private static readonly object s_LockCell = new object();

            #region Init
            public readonly int2 Idxes;
            public readonly int ParentIdx;
            public readonly int Idx;

            public int2 Location;
            public Bounds Bounds;

            internal bool HasDependency;
            internal int2 DependencyTarget;
            internal int2[] DependencyChilds;
            internal object CustomData;

            private readonly float3[] Verties;
            private readonly float3[] NavMeshVerties;

            public bool Enabled;
            public bool Highlighted;

            public Color NormalColor;
            public Color HighlightColor;
            public Color DisableColor;

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
            // NavMesh
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
                            NavMesh.SamplePosition(NavMeshVerties[i], out NavMeshHit hit, .5f, -1);
                            if (!hit.hit) return true;
                        }
                        else
                        {
                            NavMeshLocation hit = Instance.m_NavMeshQuery.MapLocation(NavMeshVerties[i], Vector3.one, 0, -1);
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
                DependencyChilds = null;
                CustomData = null;

                Verties = new float3[4]
                {
                new float3(bounds.min.x, bounds.min.y, bounds.min.z),
                new float3(bounds.min.x, bounds.min.y, bounds.max.z),
                new float3(bounds.max.x, bounds.min.y, bounds.max.z),
                new float3(bounds.max.x, bounds.min.y, bounds.min.z)
                };

                Enabled = true;
                Highlighted = false;

                NormalColor = new Color(1, 1, 1, .1f);
                HighlightColor = new Color { g = 1, a = .1f };
                DisableColor = new Color { r = 1, a = .1f };

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
            internal GridCell(in BinaryGridCell cell, bool enableNavMesh) : this(cell.ParentIdx, cell.Idx, cell.Location, new Bounds(cell.Bounds_Center, cell.Bounds_Size), enableNavMesh)
            {
                HasDependency = cell.HasDependency;
                DependencyTarget = cell.DependencyTarget;
                DependencyChilds = cell.DependencyChilds;
                CustomData = cell.CustomData;
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
                    ref Grid grid = ref GetGrid(DependencyTarget.x);
                    ref GridCell cell = ref grid.GetCell(DependencyTarget.y);

                    return cell.CustomData;
                }
                else return CustomData;
            }
            public bool GetCustomData<T>(out T value) where T : ITag
            {
                object data;
                if (HasDependency)
                {
                    ref Grid grid = ref GetGrid(DependencyTarget.x);
                    ref GridCell cell = ref grid.GetCell(DependencyTarget.y);

                    data = cell.CustomData;
                }
                else data = CustomData;

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
                        $"해당 객체({data.GetType().Name})는 Serializable 어트리뷰트가 선언되지 않았습니다.");
                }

                if (HasDependency)
                {
                    ref Grid grid = ref GetGrid(DependencyTarget.x);
                    ref GridCell cell = ref grid.GetCell(DependencyTarget.y);

                    cell.CustomData = data;
                }
                else CustomData = data;

                SetDirty();
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
                    List<int2> temp = cell.DependencyChilds.ToList();
                    for (int i = 0; i < temp.Count; i++)
                    {
                        if (temp[i].Equals(Idxes))
                        {
                            temp.RemoveAt(i);
                            break;
                        }
                    }

                    if (temp.Count > 0) cell.DependencyChilds = temp.ToArray();
                    else cell.DependencyChilds = null;
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

                    if (DependencyChilds != null)
                    {
                        for (int i = 0; i < DependencyChilds.Length; i++)
                        {
                            Instance.m_DirtyFlags.Enqueue(DependencyChilds[i]);
                            Instance.m_DirtyFlagsAsync.Enqueue(DependencyChilds[i]);
                        }
                    }
                }
            }

            public void Dispose() { }

            private static void InternalEnableDependency(ref GridCell other, ref Grid grid, ref GridCell cell)
            {
                List<int2> temp;
                lock (s_LockCell)
                {
                    if (cell.DependencyChilds == null) temp = new List<int2>();
                    else temp = cell.DependencyChilds.ToList();

                    temp.Add(other.Idxes);
                    cell.DependencyChilds = temp.ToArray();
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

                        Grid grid = GetGrid(idxes.x);
                        GridCell cell = grid.GetCell(idxes.y);

                        m_OnDirtyFlagRaised[idxes.x].Invoke(grid, cell);
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

                        Grid grid = GetGrid(idxes.x);
                        GridCell cell = grid.GetCell(idxes.y);

                        m_OnDirtyFlagRaisedAsync[idxes.x].Invoke(grid, cell);
                    }
                }

                yield return null;
            }
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
                    if (!RenderManager.Instance.IsInCameraScreen(/*RenderCameraTarget,*/ cell.Bounds.center)) continue;

                    if (!cell.Enabled || cell.BlockedByNavMesh)
                    {
                        GLDrawPlane(cell.Bounds.center, new Vector2(cell.Bounds.size.x, cell.Bounds.size.z), in cell.DisableColor, true);
                    }
                    else
                    {
                        GLDrawPlane(cell.Bounds.center, new Vector2(cell.Bounds.size.x, cell.Bounds.size.z),
                            cell.Highlighted ? cell.HighlightColor : cell.NormalColor, true);
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
                        Handles.Label(cell.Bounds.center, $"{cell.Idx}:({cell.Location.x},{cell.Location.y}):c{cell.DependencyChilds?.Length}");
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
            }

            return count;
        }

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
