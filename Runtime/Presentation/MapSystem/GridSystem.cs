using Syadeu.Database;
using Syadeu.Mono;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Proxy;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.Map
{
    public sealed class GridSystem : PresentationSystemEntity<GridSystem>
    {
        public override bool EnableBeforePresentation => true;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private EntitySystem m_EntitySystem;
        private Render.RenderSystem m_RenderSystem;
        private Events.EventSystem m_EventSystem;

        private GridMapAttribute m_MainGrid;
        private bool m_DrawGrid = false;

        private readonly ConcurrentQueue<GridSizeAttribute> m_WaitForRegister = new ConcurrentQueue<GridSizeAttribute>();

        private readonly Dictionary<Entity<IEntity>, int[]> m_EntityGridIndices = new Dictionary<Entity<IEntity>, int[]>();
        private readonly Dictionary<int, List<Entity<IEntity>>> m_GridEntities = new Dictionary<int, List<Entity<IEntity>>>();

        private GridMapAttribute GridMap => m_MainGrid;
        public float CellSize => m_MainGrid.Grid.cellSize;

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            CreateConsoleCommands();

            return base.OnInitialize();
        }
        protected override PresentationResult OnInitializeAsync()
        {
            RequestSystem<EntitySystem>(Bind);
            RequestSystem<Render.RenderSystem>(Bind);
            RequestSystem<Events.EventSystem>(Bind);

            return base.OnInitializeAsync();
        }

        #region Binds

        private void Bind(EntitySystem other)
        {
            m_EntitySystem = other;
        }
        private void Bind(Render.RenderSystem other)
        {
            m_RenderSystem = other;
            m_RenderSystem.OnRender += M_RenderSystem_OnRender;
        }
        private void Bind(Events.EventSystem other)
        {
            m_EventSystem = other;

            m_EventSystem.AddEvent<Events.OnTransformChangedEvent>(OnTransformChangedEventHandler);
        }

        #endregion

        #region Event Handlers

        private void OnTransformChangedEventHandler(Events.OnTransformChangedEvent ev)
        {
            if (GridMap == null) return;

            GridSizeAttribute att = ev.entity.GetAttribute<GridSizeAttribute>();
            if (att == null) return;

            UpdateGridLocation(ev.entity, att, true);
        }
        private void UpdateGridLocation(Entity<IEntity> entity, GridSizeAttribute att, bool postEvent)
        {
            ref GridSizeComponent component = ref entity.GetComponent<GridSizeComponent>();

            bool gridChanged = false;

            int2 p0 = GridMap.Grid.PositionToLocation(entity.transform.position);
            for (int i = 0; i < att.m_GridLocations.Length; i++)
            {
                int aTemp = GridMap.Grid.LocationToIndex(p0 + att.m_GridLocations[i]);
                if (!aTemp.Equals(component.positions[i].index))
                {
                    gridChanged = true;
                }

                component.positions[i] = new GridPosition(aTemp, p0 + att.m_GridLocations[i]);
            }

            if (gridChanged)
            {
                if (postEvent)
                {
                    m_EventSystem.PostEvent(Events.OnGridPositionChangedEvent.GetEvent(entity, component.positions));
                }
                
                UpdateGridEntity(entity, in component.positions);
            }
        }
        private void RemoveGridEntity(Entity<IEntity> entity)
        {
            if (m_EntityGridIndices.TryGetValue(entity, out int[] cachedIndics))
            {
                for (int i = 0; i < cachedIndics.Length; i++)
                {
                    if (!m_GridEntities.ContainsKey(cachedIndics[i]))
                    {
                        $"{cachedIndics[i]} notfound?".ToLog();
                        continue;
                    }

                    m_GridEntities[cachedIndics[i]].Remove(entity);
                }
                m_EntityGridIndices.Remove(entity);
            }
        }
        private void UpdateGridEntity(Entity<IEntity> entity, in FixedList512Bytes<GridPosition> indices)
        {
            RemoveGridEntity(entity);

            int[] clone = new int[indices.Length];
            for (int i = 0; i < indices.Length; i++)
            {
                clone[i] = indices[i].index;
            }

            for (int i = 0; i < clone.Length; i++)
            {
                if (!m_GridEntities.TryGetValue(clone[i], out List<Entity<IEntity>> entities))
                {
                    entities = new List<Entity<IEntity>>();
                    m_GridEntities.Add(clone[i], entities);
                }
                entities.Add(entity);

                $"{entity.Name} :: {clone[i]}".ToLog();
            }

            m_EntityGridIndices.Add(entity, clone);
        }

        #endregion

        public override void OnDispose()
        {
            m_RenderSystem.OnRender -= M_RenderSystem_OnRender;

            m_EventSystem.RemoveEvent<Events.OnTransformChangedEvent>(OnTransformChangedEventHandler);

            m_EntitySystem = null;
            m_RenderSystem = null;
            m_EventSystem = null;
        }
        protected override PresentationResult BeforePresentation()
        {
            if (m_MainGrid != null)
            {
                int waitForRegisterCount = m_WaitForRegister.Count;
                if (waitForRegisterCount > 0)
                {
                    for (int i = 0; i < waitForRegisterCount; i++)
                    {
                        if (!m_WaitForRegister.TryDequeue(out var att)) continue;

                        Entity<IEntity> entity = att.Parent.As<IEntityData, IEntity>();
                        if (att.m_FixedToCenter)
                        {
                            ITransform tr = entity.transform;
                            float3 pos = tr.position;

                            tr.position = IndexToPosition(PositionToIndex(pos));
                        }
                        ref GridSizeComponent component = ref entity.GetComponent<GridSizeComponent>();
                        component.m_GridSystem = SystemID;

                        UpdateGridLocation(entity, att, false);
                    }
                }
            }
            return base.BeforePresentation();
        }
        
        private void CreateConsoleCommands()
        {
            ConsoleWindow.CreateCommand((cmd) =>
            {
                m_DrawGrid = !m_DrawGrid;
            }, "draw", "grid");
        }

        private void M_RenderSystem_OnRender()
        {
            if (m_DrawGrid && m_MainGrid != null)
            {
                GL.PushMatrix();
                float3x3 rotmat = new float3x3(quaternion.identity);
                float4x4 mat = new float4x4(rotmat, float3.zero);
                GL.MultMatrix(mat);

                GridExtensions.DefaultMaterial.SetPass(0);
                Color
                    colorWhite = Color.white,
                    colorRed = Color.red;
                colorWhite.a = .7f; colorRed.a = .5f;
                GL.Begin(GL.QUADS);

                GL.Color(colorWhite);
                DrawGridGL(m_MainGrid.Grid, .05f);

                GL.Color(colorRed);
                int[] gridEntities = m_GridEntities.Keys.ToArray();
                DrawOccupiedCells(m_MainGrid.Grid, gridEntities);

                //GL.Color(Color.black);
                //var temp = m_EntityGridIndices.Keys.ToArray();
                //for (int i = 0; i < temp.Length; i++)
                //{
                //    var indices = GetRange(m_EntityGridIndices[temp[i]][0], 6);
                //    foreach (var item in indices)
                //    {
                //        DrawCell(m_MainGrid.Value.Grid, item);
                //    }
                //}

                GL.End();
                GL.PopMatrix();
            }

            static void DrawGridGL(BinaryGrid grid, float thickness)
            {
                const float yOffset = .2f;
                int2 gridSize = grid.gridSize;

                Vector3 minPos = grid.IndexToPosition(0);
                minPos.x -= grid.cellSize * .5f;
                minPos.z += grid.cellSize * .5f;

                Vector3 maxPos = grid.LocationToPosition(gridSize);
                maxPos.x -= grid.cellSize * .5f;
                maxPos.z += grid.cellSize * .5f;

                var xTemp = new Vector3(thickness * .5f, 0, 0);
                var yTemp = new Vector3(0, 0, thickness * .5f);

                for (int y = 0; y < gridSize.y + 1; y++)
                {
                    for (int x = 0; x < gridSize.x + 1; x++)
                    {
                        Vector3
                            p1 = new Vector3(
                                minPos.x,
                                minPos.y + yOffset,
                                minPos.z - (grid.cellSize * y)),
                            p2 = new Vector3(
                                maxPos.x,
                                minPos.y + yOffset,
                                minPos.z - (grid.cellSize * y)),
                            p3 = new Vector3(
                                minPos.x + (grid.cellSize * x),
                                minPos.y + yOffset,
                                minPos.z),
                            p4 = new Vector3(
                                minPos.x + (grid.cellSize * x),
                                minPos.y + yOffset,
                                maxPos.z)
                            ;

                        GL.Vertex(p1 - yTemp); GL.Vertex(p2 - yTemp);
                        GL.Vertex(p2 + yTemp); GL.Vertex(p1 + yTemp);

                        GL.Vertex(p3 - xTemp); GL.Vertex(p4 - xTemp);
                        GL.Vertex(p4 + xTemp); GL.Vertex(p3 + xTemp);
                    }
                }
            }
            static void DrawOccupiedCells(BinaryGrid grid, int[] gridEntities)
            {
                float sizeHalf = grid.cellSize * .5f;

                for (int i = 0; i < gridEntities.Length; i++)
                {
                    Vector3
                            cellPos = grid.IndexToPosition(gridEntities[i]),
                            p1 = new Vector3(cellPos.x - sizeHalf, cellPos.y + .1f, cellPos.z - sizeHalf),
                            p2 = new Vector3(cellPos.x - sizeHalf, cellPos.y + .1f, cellPos.z + sizeHalf),
                            p3 = new Vector3(cellPos.x + sizeHalf, cellPos.y + .1f, cellPos.z + sizeHalf),
                            p4 = new Vector3(cellPos.x + sizeHalf, cellPos.y + .1f, cellPos.z - sizeHalf);

                    GL.Vertex(p1);
                    GL.Vertex(p2);
                    GL.Vertex(p3);
                    GL.Vertex(p4);
                }
            }
            static void DrawCell(BinaryGrid grid, in int index)
            {
                float sizeHalf = grid.cellSize * .5f;
                Vector3
                    cellPos = grid.IndexToPosition(index),
                    p1 = new Vector3(cellPos.x - sizeHalf, cellPos.y + .1f, cellPos.z - sizeHalf),
                    p2 = new Vector3(cellPos.x - sizeHalf, cellPos.y + .1f, cellPos.z + sizeHalf),
                    p3 = new Vector3(cellPos.x + sizeHalf, cellPos.y + .1f, cellPos.z + sizeHalf),
                    p4 = new Vector3(cellPos.x + sizeHalf, cellPos.y + .1f, cellPos.z - sizeHalf);

                GL.Vertex(p1);
                GL.Vertex(p2);
                GL.Vertex(p3);
                GL.Vertex(p4);
            }
        }
        #endregion

        #region Register

        public void RegisterGrid(GridMapAttribute gridMap)
        {
            if (m_MainGrid == null)
            {
                m_MainGrid = gridMap;
            }
            else
            {
                CoreSystem.Logger.LogError(Channel.Presentation,
                    $"Attempt to load grids more then one at SceneDataEntity({gridMap.Parent.Name}). This is not allowed.");
            }
        }
        public void UnregisterGrid(GridMapAttribute gridMap)
        {
            if (m_MainGrid.Equals(gridMap))
            {
                m_MainGrid = null;
            }
        }

        public void RegisterGridSize(GridSizeAttribute gridSize)
        {
            m_WaitForRegister.Enqueue(gridSize);
        }
        public void UnregisterGridSize(GridSizeAttribute gridSize)
        {
            RemoveGridEntity(gridSize.Parent.As<IEntityData, IEntity>());
        }

        #endregion

        public void SetObstacleLayers(params int[] layers)
        {
            GridMap.SetObstacleLayers(layers);
        }
        public void AddObstacleLayers(params int[] layers)
        {
            GridMap.AddObstacleLayers(layers);
        }
        public int[] GetLayer(in int layer) => GridMap.GetLayer(in layer);

        public bool HasPath([NoAlias] int from, [NoAlias] int to, [NoAlias] int maxPathLength, out int pathFound, [NoAlias] int maxIteration = 32)
        {
            int2
                fromLocation = GridMap.Grid.IndexToLocation(in from),
                toLocation = GridMap.Grid.IndexToLocation(in to);

            GridPathTile tile = new GridPathTile(from, fromLocation);
            tile.Calculate(GridMap.Grid, GridMap.ObstacleLayer);

            unsafe
            {
                GridPathTile* path = stackalloc GridPathTile[maxPathLength];
                path[0] = tile;

                pathFound = 1;
                int iteration = 0;
                while (
                    iteration < maxIteration &&
                    pathFound < maxPathLength &&
                    path[pathFound - 1].position.index != to)
                {
                    GridPathTile lastTileData = path[pathFound - 1];
                    if (lastTileData.IsBlocked())
                    {
                        //path.RemoveAt(path.Count - 1);
                        pathFound--;

                        if (pathFound == 0) break;

                        GridPathTile parentTile = path[pathFound - 1];
                        parentTile.opened[lastTileData.direction] = false;
                        path[pathFound - 1] = parentTile;
                    }
                    else
                    {
                        int nextDirection = GetLowestCost(ref lastTileData, toLocation);

                        GridPathTile nextTile = lastTileData.GetNext(nextDirection);
                        nextTile.Calculate(GridMap.Grid, GridMap.ObstacleLayer);

                        path[pathFound] = (nextTile);
                        pathFound++;
                    }

                    iteration++;
                }

                return path[pathFound - 1].position.index == to;
            }
        }
        public bool GetPath64(in int from, in int to, in int maxPathLength, ref GridPath32 path, in NativeHashSet<int> ignoreIndices = default, in int maxIteration = 32)
        {
            int2
                fromLocation = GridMap.Grid.IndexToLocation(in from),
                toLocation = GridMap.Grid.IndexToLocation(in to);

            GridPathTile tile = new GridPathTile(from, fromLocation);
            tile.Calculate(GridMap.Grid, GridMap.ObstacleLayer, ignoreIndices);

            path.Clear();

            unsafe
            {
                GridPathTile* list = stackalloc GridPathTile[512];
                list[0] = tile;

                int iteration = 0; int count = 1;
                while (
                    iteration < maxIteration &&
                    count < maxPathLength &&
                    list[count - 1].position.index != to)
                {
                    GridPathTile lastTileData = list[count - 1];
                    if (lastTileData.IsBlocked())
                    {
                        //path.RemoveAt(path.Length - 1);
                        count -= 1;

                        if (path.Length == 0) break;

                        GridPathTile parentTile = list[count - 1];
                        parentTile.opened[lastTileData.direction] = false;
                        list[count - 1] = parentTile;
                    }
                    else
                    {
                        int nextDirection = GetLowestCost(ref lastTileData, in toLocation);

                        GridPathTile nextTile = lastTileData.GetNext(nextDirection);
                        nextTile.Calculate(GridMap.Grid, GridMap.ObstacleLayer, ignoreIndices);
                        
                        list[count] = (nextTile);
                        count += 1;
                    }

                    iteration++;
                }

                for (int i = 0; i < count; i++)
                {
                    path.Add(new GridTile()
                    {
                        index = list[i].position.index,
                        parent = list[i].parent.index
                    });
                }

                return list[count - 1].position.index == to;
            }
        }

        [Obsolete]
        public bool GetPath(int from, int to, List<GridPathTile> path, int maxPathLength, int maxIteration = 32)
        {
            int2
                fromLocation = GridMap.Grid.IndexToLocation(in from),
                toLocation = GridMap.Grid.IndexToLocation(in to);

            GridPathTile tile = new GridPathTile(from, fromLocation);
            tile.Calculate(GridMap.Grid, GridMap.ObstacleLayer);

            if (path == null)
            {
                path = new List<GridPathTile>()
                {
                    tile
                };
            }
            else
            {
                path.Clear();
                path.Add(tile);
            }

            int iteration = 0;
            while (
                iteration < maxIteration &&
                path.Count < maxPathLength &&
                path[path.Count - 1].position.index != to)
            {
                GridPathTile lastTileData = path[path.Count - 1];
                if (lastTileData.IsBlocked())
                {
                    path.RemoveAt(path.Count - 1);

                    if (path.Count == 0) break;

                    GridPathTile parentTile = path[path.Count - 1];
                    parentTile.opened[lastTileData.direction] = false;
                    path[path.Count - 1] = parentTile;
                }
                else
                {
                    int nextDirection = GetLowestCost(ref lastTileData, toLocation);

                    GridPathTile nextTile = lastTileData.GetNext(nextDirection);
                    nextTile.Calculate(GridMap.Grid, GridMap.ObstacleLayer);
                    path.Add(nextTile);
                }
                
                iteration++;
            }

            return path[path.Count - 1].position.index == to;
        }

        public IReadOnlyList<Entity<IEntity>> GetEntitiesAt(in int index)
        {
            if (m_GridEntities.TryGetValue(index, out List<Entity<IEntity>> entities))
            {
                return entities;
            }
            return Array.Empty<Entity<IEntity>>();
        }
        public IReadOnlyList<Entity<IEntity>> GetEntitiesAt(in int2 location)
        {
            int index = LocationToIndex(in location);
            return GetEntitiesAt(index);
        }

        #region Utils

        public float3 IndexToPosition(int idx)
        {
            //if (GridMap.Grid == null) throw new System.Exception();
            return GridMap.Grid.IndexToPosition(idx);
        }
        public int2 IndexToLocation(int idx)
        {
            //if (GridMap.Grid == null) throw new System.Exception();
            return GridMap.Grid.IndexToLocation(idx);
        }
        public int LocationToIndex(in int2 location)
        {
            //if (GridMap.Grid == null)
            //{
            //    CoreSystem.Logger.LogError(Channel.Presentation,
            //        "Grid not found.");
            //    return 0;
            //}

            return GridMap.Grid.LocationToIndex(in location);
        }
        public float3 LocationToPosition(in int2 location)
        {
            return GridMap.Grid.LocationToPosition(location);
        }
        public int PositionToIndex(float3 position)
        {
            //if (GridMap.Grid == null) throw new System.Exception();
            return GridMap.Grid.PositionToIndex(position);
        }

        #endregion

        #region Get Range

        [Obsolete]
        public int[] GetRange(int idx, int range, params int[] ignoreLayers)
        {
            //if (GridMap.Grid == null) throw new System.Exception();

            // TODO : 임시. 이후 gridsize 에 맞춰서 인덱스 반환
            int[] temp = GridMap.Grid.GetRange(in idx, in range);
            for (int i = 0; i < ignoreLayers?.Length; i++)
            {
                temp = GridMap.FilterByLayer(ignoreLayers[i], temp, out _);
            }

            return temp;
        }

        public FixedList32Bytes<int> GetRange8(in int idx, in int range, in FixedList128Bytes<int> ignoreLayers)
        {
            FixedList32Bytes<int> temp = GridMap.Grid.GetRange8(in idx, in range);
            for (int i = 0; i < ignoreLayers.Length; i++)
            {
                temp = GridMap.FilterByLayer32(ignoreLayers[i], in temp);
            }

            return temp;
        }
        public FixedList64Bytes<int> GetRange16(in int idx, in int range, in FixedList128Bytes<int> ignoreLayers)
        {
            FixedList64Bytes<int> temp = GridMap.Grid.GetRange16(in idx, in range);
            for (int i = 0; i < ignoreLayers.Length; i++)
            {
                temp = GridMap.FilterByLayer64(ignoreLayers[i], in temp);
            }

            return temp;
        }
        public FixedList128Bytes<int> GetRange32(in int idx, in int range, in FixedList128Bytes<int> ignoreLayers)
        {
            FixedList128Bytes<int> temp = GridMap.Grid.GetRange32(in idx, in range);
            for (int i = 0; i < ignoreLayers.Length; i++)
            {
                temp = GridMap.FilterByLayer128(ignoreLayers[i], in temp);
            }

            return temp;
        }
        public FixedList4096Bytes<int> GetRange1024(in int idx, in int range, in FixedList128Bytes<int> ignoreLayers)
        {
            FixedList4096Bytes<int> temp = GridMap.Grid.GetRange1024(in idx, in range);
            for (int i = 0; i < ignoreLayers.Length; i++)
            {
                temp = GridMap.FilterByLayer1024(ignoreLayers[i], in temp);
            }

            return temp;
        }
        public void GetRange(ref NativeList<int> list, in int idx, in int range, in FixedList128Bytes<int> ignoreLayers)
        {
            GridMap.Grid.GetRange(ref list, in idx, in range);
            for (int i = 0; i < ignoreLayers.Length; i++)
            {
                GridMap.FilterByLayer(ignoreLayers[i], ref list);
            }
        }

        #endregion

        private int GetLowestCost(ref GridPathTile prev, in int2 to)
        {
            int lowest = -1;
            int cost = int.MaxValue;

            for (int i = 0; i < 4; i++)
            {
                if (!prev.opened[i]) continue;

                int tempCost = prev.GetCost(i, to);

                if (tempCost < cost)
                {
                    lowest = i;
                    cost = tempCost;
                }
            }

            return lowest;
        }
        private int GetSqrMagnitude(int index) => GetSqrMagnitude(IndexToLocation(index));
        private static int GetSqrMagnitude(int2 location) => (location.x * location.x) + (location.y * location.y);
    }

    [BurstCompatible]
    public struct GridTile
    {
        public int parent;
        public int index;
    }

    [BurstCompatible]
    public struct GridPathTile : IEquatable<GridPathTile>
    {
        public static readonly GridPathTile Empty = new GridPathTile(-1, -1);

        public GridPosition parent;
        public int direction;
        public GridPosition position;

        public bool4 opened;
        public GridPosition4 openedPositions;
        private int4 costs;

        public GridPathTile(int index, int2 location)
        {
            this.parent = GridPosition.Empty;
            this.direction = -1;
            this.position = new GridPosition(index, location);

            opened = false;
            openedPositions = GridPosition4.Empty;
            costs = -1;
        }
        public GridPathTile(GridPosition parent, GridPosition position, int direction)
        {
            this.parent = parent;
            this.direction = direction;
            this.position = position;

            opened = false;
            openedPositions = GridPosition4.Empty;
            costs = -1;
        }

        public int GetCost(int direction, int2 to)
        {
            if (costs[direction] < 0)
            {
                int2 temp = openedPositions.location[direction] - to;
                costs[direction] = (temp.x * temp.x) + (temp.y * temp.y);
            }

            return costs[direction];
        }
        public GridPathTile GetNext(int direction)
        {
            return new GridPathTile(position, openedPositions[direction], direction);
        }
        public bool IsRoot() => parent.IsEmpty();
        public bool IsBlocked()
        {
            for (int i = 0; i < 4; i++)
            {
                if (opened[i]) return false;
            }
            return true;
        }
        public void Calculate(in BinaryGrid grid, in NativeHashSet<int> ignoreLayers = default)
        {
            for (int i = 0; i < 4; i++)
            {
                int2 nextTempLocation = grid.GetDirection(in position.location, (Direction)(1 << i));
                if (nextTempLocation.Equals(parent.location)) continue;

                //int nextTemp = GridBurstExtensions.p_LocationInt2ToIndex.Invoke(grid.bounds, grid.cellSize, nextTempLocation);
                int nextTemp = grid.LocationToIndex(nextTempLocation);
                if (ignoreLayers.IsCreated)
                {
                    if (ignoreLayers.Contains(nextTemp))
                    {
                        opened[i] = false;
                        openedPositions.RemoveAt(i);
                        continue;
                    }
                }

                opened[i] = true;
                openedPositions.UpdateAt(i, nextTemp, nextTempLocation);
            }
        }
        public void Calculate(in BinaryGrid grid, in NativeHashSet<int> ignoreLayers, in NativeHashSet<int> additionalIgnore = default)
        {
            for (int i = 0; i < 4; i++)
            {
                int2 nextTempLocation = grid.GetDirection(in position.location, (Direction)(1 << i));
                if (nextTempLocation.Equals(parent.location)) continue;

                //int nextTemp = GridBurstExtensions.p_LocationInt2ToIndex.Invoke(grid.bounds, grid.cellSize, nextTempLocation);
                int nextTemp = grid.LocationToIndex(nextTempLocation);
                if (ignoreLayers.IsCreated)
                {
                    if (ignoreLayers.Contains(nextTemp))
                    {
                        opened[i] = false;
                        openedPositions.RemoveAt(i);
                        continue;
                    }
                }

                if (additionalIgnore.IsCreated)
                {
                    if (additionalIgnore.Contains(nextTemp))
                    {
                        opened[i] = false;
                        openedPositions.RemoveAt(i);
                        continue;
                    }
                }

                opened[i] = true;
                openedPositions.UpdateAt(i, nextTemp, nextTempLocation);
            }
        }

        public bool IsEmpty() => Equals(Empty);
        public bool Equals(GridPathTile other)
            => parent.Equals(other) && direction.Equals(other.direction) && position.Equals(other.position);
    }
}
