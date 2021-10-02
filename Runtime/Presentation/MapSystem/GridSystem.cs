using Syadeu.Database;
using Syadeu.Mono;
using Syadeu.Presentation.Components;
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
        public float CellSize => m_MainGrid.CellSize;
        public Mesh CellMesh => m_MainGrid.CellMesh;

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

            GridPosition p0 = GridMap.GetGridPosition(entity.transform.position);
            for (int i = 0; i < att.m_GridLocations.Length; i++)
            {
                GridPosition aTemp = GridMap.Add(p0, att.m_GridLocations[i]);
                if (!aTemp.Equals(component.positions[i]))
                {
                    gridChanged = true;
                }

                component.positions[i] = aTemp;
            }

            //int2 p0 = GridMap.Grid.PositionToLocation(entity.transform.position);
            //for (int i = 0; i < att.m_GridLocations.Length; i++)
            //{
            //    int aTemp = GridMap.Grid.LocationToIndex(p0 + att.m_GridLocations[i]);
            //    if (!aTemp.Equals(component.positions[i].index))
            //    {
            //        gridChanged = true;
            //    }

            //    component.positions[i] = new GridPosition(aTemp, p0 + att.m_GridLocations[i]);
            //}

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
                m_MainGrid.DrawGridGL(.05f);

                GL.Color(colorRed);
                int[] gridEntities = m_GridEntities.Keys.ToArray();
                m_MainGrid.DrawOccupiedCells(gridEntities);

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

        public bool HasPath(
            [NoAlias] int from, [NoAlias] int to, [NoAlias] int maxPathLength, 
            out int pathFound, [NoAlias] int maxIteration = 32)
        {
            int2
                fromLocation = GridMap.GetLocation(in from),
                toLocation = GridMap.GetLocation(in to);

            GridPathTile tile = new GridPathTile(from, fromLocation);
            tile.Calculate(GridMap, GridMap.ObstacleLayer);

            unsafe
            {
                GridPathTile* path = stackalloc GridPathTile[512];
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
                        nextTile.Calculate(GridMap, GridMap.ObstacleLayer);

                        path[pathFound] = (nextTile);
                        pathFound++;
                    }

                    iteration++;
                }

                return path[pathFound - 1].position.index == to;
            }
        }
        public bool GetPath32(in int from, in int to, in int maxPathLength, ref GridPath32 path, in NativeHashSet<int> ignoreIndices = default, in int maxIteration = 32)
        {
            int2
                fromLocation = GridMap.GetLocation(in from),
                toLocation = GridMap.GetLocation(in to);

            GridPathTile tile = new GridPathTile(from, fromLocation);
            tile.Calculate(GridMap, GridMap.ObstacleLayer, ignoreIndices);

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
                        nextTile.Calculate(GridMap, GridMap.ObstacleLayer, ignoreIndices);
                        
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

        public IReadOnlyList<Entity<IEntity>> GetEntitiesAt(in int index)
        {
            if (m_GridEntities.TryGetValue(index, out List<Entity<IEntity>> entities))
            {
                return entities;
            }
            return Array.Empty<Entity<IEntity>>();
        }

        #region Utils

        public float3 IndexToPosition(int idx)
        {
            return GridMap.GetPosition(idx);
        }
        public int2 IndexToLocation(int idx)
        {
            //if (GridMap.Grid == null) throw new System.Exception();
            return GridMap.GetLocation(idx);
        }

        public int PositionToIndex(float3 position)
        {
            return GridMap.GetIndex(position);
        }

        #endregion

        #region Get Range

        [Obsolete]
        public int[] GetRange(int idx, int range, params int[] ignoreLayers)
            => GridMap.GetRange(idx, range, ignoreLayers);

        public FixedList32Bytes<int> GetRange8(in int idx, in int range, in FixedList128Bytes<int> ignoreLayers)
            => GridMap.GetRange8(in idx, in range, in ignoreLayers);
        public FixedList64Bytes<int> GetRange16(in int idx, in int range, in FixedList128Bytes<int> ignoreLayers)
            => GridMap.GetRange16(in idx, in range, in ignoreLayers);
        public FixedList128Bytes<int> GetRange32(in int idx, in int range, in FixedList128Bytes<int> ignoreLayers)
            => GridMap.GetRange32(in idx, in range, in ignoreLayers);
        public FixedList4096Bytes<int> GetRange1024(in int idx, in int range, in FixedList128Bytes<int> ignoreLayers)
            => GridMap.GetRange1024(in idx, in range, in ignoreLayers);
        public void GetRange(ref NativeList<int> list, in int idx, in int range, in FixedList128Bytes<int> ignoreLayers)
            => GridMap.GetRange(ref list, in idx, in range, in ignoreLayers);

        #endregion

        #region UI

        public Entity<IEntity> PlaceUICell(GridPosition position, float heightOffset = .25f)
        {
            Entity<IEntity> entity = 
                m_EntitySystem.CreateEntity(
                    GridMap.m_CellUIPrefab, 
                    IndexToPosition(position.index) + new float3(0, heightOffset, 0), 
                    quaternion.EulerZXY(new float3(90, 0, 0) * Mathf.Deg2Rad), 
                    1);

            return entity;
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
}
