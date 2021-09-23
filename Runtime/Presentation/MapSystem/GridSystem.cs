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

        private KeyValuePair<SceneDataEntity, GridMapAttribute> m_MainGrid;
        private bool m_DrawGrid = false;

        private readonly ConcurrentQueue<GridSizeAttribute> m_WaitForRegister = new ConcurrentQueue<GridSizeAttribute>();

        private readonly Dictionary<Entity<IEntity>, int[]> m_EntityGridIndices = new Dictionary<Entity<IEntity>, int[]>();
        private readonly Dictionary<int, List<Entity<IEntity>>> m_GridEntities = new Dictionary<int, List<Entity<IEntity>>>();

        private GridMapAttribute GridMap => m_MainGrid.Value;

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
            m_EntitySystem.OnEntityCreated += M_EntitySystem_OnEntityCreated;
            m_EntitySystem.OnEntityDestroy += M_EntitySystem_OnEntityDestroy;
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

            UpdateGridLocation(ev.entity, att);
        }
        private void UpdateGridLocation(Entity<IEntity> entity, GridSizeAttribute att)
        {
            if (att.m_CurrentGridIndices.Length != att.m_GridLocations.Length)
            {
                att.m_CurrentGridIndices = new int[att.m_GridLocations.Length];
            }

            bool gridChanged = false;

            int2 p0 = GridMap.Grid.PositionToLocation(entity.transform.position);
            int a0 = GridMap.Grid.LocationToIndex(p0);
            if (!a0.Equals(att.m_CurrentGridIndices[0])) gridChanged = true;

            for (int i = 0; i < att.m_GridLocations.Length; i++)
            {
                if (att.m_GridLocations[i].Equals(int2.zero)) continue;

                int aTemp = GridMap.Grid.LocationToIndex(p0 + att.m_GridLocations[i]);
                if (!aTemp.Equals(att.m_CurrentGridIndices[i + 1]))
                {
                    gridChanged = true;
                }
                att.m_CurrentGridIndices[i + 1] = aTemp;
            }

            if (gridChanged)
            {
                m_EventSystem.PostEvent(Events.OnGridPositionChangedEvent.GetEvent(entity, att.CurrentGridIndices));
                UpdateGridEntity(entity, att.m_CurrentGridIndices);
            }
        }
        private void RemoveGridEntity(Entity<IEntity> entity)
        {
            if (m_EntityGridIndices.TryGetValue(entity, out int[] cachedIndics))
            {
                for (int i = 0; i < cachedIndics.Length; i++)
                {
                    m_GridEntities[cachedIndics[i]].Remove(entity);
                }
                m_EntityGridIndices.Remove(entity);
            }
        }
        private void UpdateGridEntity(Entity<IEntity> entity, int[] indices)
        {
            RemoveGridEntity(entity);
            for (int i = 0; i < indices.Length; i++)
            {
                if (!m_GridEntities.TryGetValue(indices[i], out List<Entity<IEntity>> entities))
                {
                    entities = new List<Entity<IEntity>>();
                    m_GridEntities.Add(indices[i], entities);
                }
                entities.Add(entity);
            }

            m_EntityGridIndices.Add(entity, indices);
        }

        #endregion

        public override void OnDispose()
        {
            m_EntitySystem.OnEntityCreated -= M_EntitySystem_OnEntityCreated;
            m_EntitySystem.OnEntityDestroy -= M_EntitySystem_OnEntityDestroy;
            m_RenderSystem.OnRender -= M_RenderSystem_OnRender;

            m_EventSystem.RemoveEvent<Events.OnTransformChangedEvent>(OnTransformChangedEventHandler);

            m_EntitySystem = null;
            m_RenderSystem = null;
            m_EventSystem = null;
        }
        protected override PresentationResult BeforePresentationAsync()
        {
            if (m_MainGrid.Value != null)
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

                        UpdateGridLocation(entity, att);
                    }
                }
            }
            return base.BeforePresentationAsync();
        }
        
        private void CreateConsoleCommands()
        {
            ConsoleWindow.CreateCommand((cmd) =>
            {
                m_DrawGrid = !m_DrawGrid;
            }, "draw", "grid");
        }

        private void M_EntitySystem_OnEntityCreated(EntityData<IEntityData> obj)
        {
            if (obj.Target is SceneDataEntity sceneData)
            {
                GridMapAttribute gridAtt = sceneData.GetAttribute<GridMapAttribute>();
                if (gridAtt == null) return;

                if (m_MainGrid.Value == null)
                {
                    m_MainGrid = new KeyValuePair<SceneDataEntity, GridMapAttribute>(sceneData, gridAtt);
                }
                else
                {
                    CoreSystem.Logger.LogError(Channel.Presentation,
                        $"Attempt to load grids more then one at SceneDataEntity({sceneData.Name}). This is not allowed.");
                }
            }
            else if (obj.Target is EntityBase entity)
            {
                var gridSizeAtt = entity.GetAttribute<GridSizeAttribute>();
                if (gridSizeAtt == null) return;

                m_WaitForRegister.Enqueue(gridSizeAtt);
            }
        }
        private void M_EntitySystem_OnEntityDestroy(EntityData<IEntityData> obj)
        {
            if (obj.Target is SceneDataEntity sceneData &&
                m_MainGrid.Key != null)
            {
                if (m_MainGrid.Key.Equals(sceneData))
                {
                    m_MainGrid = new KeyValuePair<SceneDataEntity, GridMapAttribute>();
                }
            }

            if (obj.Target is IEntity)
            {
                RemoveGridEntity(obj.As<IEntityData, IEntity>());
            }
        }
        private void M_RenderSystem_OnRender()
        {
            if (m_DrawGrid && m_MainGrid.Value != null)
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
                DrawGridGL(m_MainGrid.Value.Grid, .05f);

                GL.Color(colorRed);
                int[] gridEntities = m_GridEntities.Keys.ToArray();
                DrawOccupiedCells(m_MainGrid.Value.Grid, gridEntities);

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

            //$"from({from})->to({to}) found {path.Count}".ToLog();
            //for (int i = 0; i < path.Count; i++)
            //{
            //    $"{path[i].location} asd".ToLog();
            //}

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
        public int PositionToIndex(float3 position)
        {
            //if (GridMap.Grid == null) throw new System.Exception();
            return GridMap.Grid.PositionToIndex(position);
        }

        public int[] GetRange(int[] idx, int range, params int[] ignoreLayers)
        {
            //if (GridMap.Grid == null) throw new System.Exception();

            // TODO : 임시. 이후 gridsize 에 맞춰서 인덱스 반환
            int[] temp = GridMap.Grid.GetRange(in idx[0], in range);
            for (int i = 0; i < ignoreLayers?.Length; i++)
            {
                temp = GridMap.FilterByLayer(ignoreLayers[i], temp, out _);
            }

            return temp;
        }

        private int GetLowestCost(ref GridPathTile prev, int2 to)
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

    [BurstCompile(CompileSynchronously = true)]
    public struct GridPathfindingJob16 : IJobParallelFor
    {
        const int c_MaxIteration = 64;

        [ReadOnly] private BinaryGrid m_Grid;
        [ReadOnly] private NativeHashSet<int> m_IgnoreIndices;
        [ReadOnly, DeallocateOnJobCompletion] private NativeArray<int2> m_From2TargetsTemp;
        [ReadOnly] private NativeArray<int2>.ReadOnly m_From2Targets;

        [WriteOnly] public NativeArray<GridPath16> m_Results;

        private GridBurstExtensions.Functions m_Functions;

        public GridPathfindingJob16(
            BinaryGrid grid, 
            NativeArray<int2> from2Targets, 
            NativeArray<GridPath16> results,
            GridBurstExtensions.Functions functions,
            NativeHashSet<int> ignoreIndices = default)
        {
            m_Grid = grid;
            m_IgnoreIndices = ignoreIndices;

            m_From2TargetsTemp = from2Targets;
            m_From2Targets = m_From2TargetsTemp.AsReadOnly();

            m_Results = results;

            m_Functions = functions;
        }

        public void Execute(int index)
        {
            int
                from = m_From2Targets[index].x,
                to = m_From2Targets[index].y;

            int2
                fromLocation = m_Grid.IndexToLocation(in from),
                toLocation = m_Grid.IndexToLocation(in to);

            GridPathTile tile = new GridPathTile(from, fromLocation);
            Calculate(m_Functions.p_LocationInt2ToIndex, ref tile, in m_Grid, in m_IgnoreIndices);
            //tile.Calculate(m_Grid, m_IgnoreIndices);

            NativeList<GridPathTile> pathList = new NativeList<GridPathTile>(16, Allocator.Temp);
            pathList.Add(tile);

            int iteration = 0;
            while (
                iteration < c_MaxIteration &&
                pathList.Length < 16 &&
                pathList[pathList.Length - 1].position.index != to)
            {
                GridPathTile lastTileData = pathList[pathList.Length - 1];
                if (lastTileData.IsBlocked())
                {
                    pathList[pathList.Length - 1] = GridPathTile.Empty;
                    pathList.RemoveAtSwapBack(pathList.Length - 1);
                    //count--;

                    if (pathList.Length == 0) break;

                    GridPathTile parentTile = pathList[pathList.Length - 1];
                    parentTile.opened[lastTileData.direction] = false;
                    pathList[pathList.Length - 1] = parentTile;
                }
                else
                {
                    int nextDirection = GetLowestCost(ref lastTileData, toLocation);

                    GridPathTile nextTile = lastTileData.GetNext(nextDirection);
                    Calculate(m_Functions.p_LocationInt2ToIndex, ref nextTile, in m_Grid, in m_IgnoreIndices);
                    //nextTile.Calculate(m_Grid, m_IgnoreIndices);
                    pathList.Add(nextTile);
                }

                iteration++;
            }

            //$"from({from})->to({to}) found {path.Count}".ToLog();
            //for (int i = 0; i < path.Count; i++)
            //{
            //    $"{path[i].location} asd".ToLog();
            //}

            GridPath16 path = new GridPath16();
            path.Initialize(pathList.Length);
            for (int i = 0; i < pathList.Length; i++)
            {
                path[i] = pathList[i];
            }

            path.result = path[pathList.Length - 1].position.index == to ? GridPath16.Result.Success : GridPath16.Result.Failed;

            m_Results[index] = path;
        }

        private int GetLowestCost(ref GridPathTile prev, int2 to)
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

        public void Calculate(
            FunctionPointer<GridBurstExtensions.LocationInt2ToIndex> func, 
            ref GridPathTile pathTile,
            in BinaryGrid grid, in NativeHashSet<int> ignoreLayers = default)
        {
            for (int i = 0; i < 4; i++)
            {
                int2 nextTempLocation = grid.GetDirection(in pathTile.position.location, (Direction)(1 << i));
                if (nextTempLocation.Equals(pathTile.parent.location)) continue;

                int nextTemp = func.Invoke(grid.bounds, grid.cellSize, nextTempLocation);
                //int nextTemp = grid.LocationToIndex(nextTempLocation);
                if (ignoreLayers.IsCreated)
                {
                    if (ignoreLayers.Contains(nextTemp))
                    {
                        pathTile.opened[i] = false;
                        pathTile.openedPositions.RemoveAt(i);
                        continue;
                    }
                }

                pathTile.opened[i] = true;
                pathTile.openedPositions.UpdateAt(i, nextTemp, nextTempLocation);
            }
        }
    }
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

        public bool IsEmpty() => Equals(Empty);
        public bool Equals(GridPathTile other)
            => parent.Equals(other) && direction.Equals(other.direction) && position.Equals(other.position);
    }
}
