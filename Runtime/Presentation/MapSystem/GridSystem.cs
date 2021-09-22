using Syadeu.Database;
using Syadeu.Mono;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Proxy;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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

        private KeyValuePair<SceneDataEntity, GridMapAttribute> m_MainGrid;
        private bool m_DrawGrid = false;

        private readonly ConcurrentQueue<GridSizeAttribute> m_WaitForRegister = new ConcurrentQueue<GridSizeAttribute>();

        private readonly Dictionary<Entity<IEntity>, int[]> m_EntityGridIndices = new Dictionary<Entity<IEntity>, int[]>();
        private readonly Dictionary<int, List<Entity<IEntity>>> m_GridEntities = new Dictionary<int, List<Entity<IEntity>>>();

        public GridMapAttribute GridMap => m_MainGrid.Value;

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

            return base.OnInitializeAsync();
        }
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
        public override void OnDispose()
        {
            m_EntitySystem.OnEntityCreated -= M_EntitySystem_OnEntityCreated;
            m_EntitySystem.OnEntityDestroy -= M_EntitySystem_OnEntityDestroy;
            m_RenderSystem.OnRender -= M_RenderSystem_OnRender;
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

                        att.GridSystem = this;
                        att.UpdateGridCell();

                        if (att.m_FixedToCenter)
                        {
                            Entity<IEntity> entity = att.Parent.As<IEntityData, IEntity>();
                            ITransform tr = entity.transform;
                            float3 pos = tr.position;

                            tr.position = IndexToPosition(PositionToIndex(pos));
                        }
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

            static void DrawGridGL(ManagedGrid grid, float thickness)
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
            static void DrawOccupiedCells(ManagedGrid grid, int[] gridEntities)
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
            static void DrawCell(ManagedGrid grid, in int index)
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
            int2 toLocation = GridMap.Grid.IndexToLocation(in to);

            GridPathTile tile = new GridPathTile(from);
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
                path[path.Count - 1].location != to)
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

            return path[path.Count - 1].location == to;
        }

        public void RemoveGridEntity(Entity<IEntity> entity)
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
        public void UpdateGridEntity(Entity<IEntity> entity, int[] indices)
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

        public IReadOnlyList<Entity<IEntity>> GetEntitiesAt(in int index)
        {
            if (m_GridEntities.TryGetValue(index, out List<Entity<IEntity>> entities))
            {
                return entities;
            }
            return Array.Empty<Entity<IEntity>>();
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

        public int[] GetRange(int idx, int range)
        {
            //if (GridMap.Grid == null) throw new System.Exception();
            return GridMap.Grid.GetRange(in idx, in range);
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

    public struct GridPathfindingJob16 : IJobParallelFor
    {
        [ReadOnly] private ManagedGrid m_Grid;
        [ReadOnly] private NativeHashSet<int> m_IgnoreLayers;
        [ReadOnly, DeallocateOnJobCompletion] private NativeArray<int2> m_From2TargetsTemp;
        [ReadOnly] private NativeArray<int2>.ReadOnly m_From2Targets;

        [WriteOnly] public NativeList<GridPath16>.ParallelWriter m_Results;

        public GridPathfindingJob16(ManagedGrid grid, NativeArray<int2> from2Targets, NativeList<GridPath16>.ParallelWriter results, NativeHashSet<int> ignoreLayers = default)
        {
            m_Grid = grid;
            m_IgnoreLayers = ignoreLayers;

            m_From2TargetsTemp = from2Targets;
            m_From2Targets = m_From2TargetsTemp.AsReadOnly();

            m_Results = results;
        }

        public void Execute(int index)
        {
            throw new NotImplementedException();
        }
    }
    public struct GridPath16
    {
        public GridPathTile a, b, c, d;
        public GridPathTile e, f, g, h;
        public GridPathTile i, j, k, l;
        public GridPathTile m, n, o, p;

        public GridPathTile this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return a;
                    case 1: return b;
                    case 2: return c;
                    case 3: return d;
                    case 4: return e;
                    case 5: return f;
                    case 6: return g;
                    case 7: return h;
                    case 8: return i;
                    case 9: return j;
                    case 10: return k;
                    case 11: return l;
                    case 12: return m;
                    case 13: return n;
                    case 14: return o;
                    case 15: return p;
                }

                throw new IndexOutOfRangeException();
            }
            set
            {
                switch (index)
                {
                    case 0: { a = value; return; };
                    case 1: { b = value; return; };
                    case 2: { c = value; return; };
                    case 3: { d = value; return; };
                    case 4: { e = value; return; };
                    case 5: { f = value; return; };
                    case 6: { g = value; return; };
                    case 7: { h = value; return; };
                    case 8: { i = value; return; };
                    case 9: { j = value; return; };
                    case 10: { k = value; return; };
                    case 11: { l = value; return; };
                    case 12: { m = value; return; };
                    case 13: { n = value; return; };
                    case 14: { o = value; return; };
                    case 15: { p = value; return; };
                }

                throw new IndexOutOfRangeException();
            }
        }
        public int Length => 16;

    }
    public struct GridPathTile
    {
        public int parent;
        public int direction;
        public int location;

        public bool4 opened;
        public int4 openedIndices;
        public int2x4 openedLocations;
        private int4 costs;

        public GridPathTile(int location)
        {
            this.parent = -1;
            this.direction = -1;
            this.location = location;

            opened = false;
            openedIndices = -1;
            openedLocations = -1;
            costs = -1;
        }
        public GridPathTile(int parent, int location, int direction)
        {
            this.parent = parent;
            this.direction = direction;
            this.location = location;

            opened = false;
            openedIndices = -1;
            openedLocations = -1;
            costs = -1;
        }

        public int GetCost(int direction, int2 to)
        {
            if (costs[direction] < 0)
            {
                int2 temp = openedLocations[direction] - to;
                costs[direction] = (temp.x * temp.x) + (temp.y * temp.y);
            }

            return costs[direction];
        }
        public GridPathTile GetNext(int direction)
        {
            return new GridPathTile(location, openedIndices[direction], direction);
        }
        public bool IsRoot() => parent == -1;
        public bool IsBlocked()
        {
            for (int i = 0; i < 4; i++)
            {
                if (opened[i]) return false;
            }
            return true;
        }
        public bool HasParent() => parent >= 0;
        public void Calculate(in ManagedGrid grid, in NativeHashSet<int> ignoreLayers = default)
        {
            int2 current = grid.IndexToLocation(in location);

            for (int i = 0; i < 4; i++)
            {
                int2 nextTempLocation = grid.GetDirection(in current, (Direction)(1 << i));
                if (nextTempLocation.Equals(parent)) continue;

                int nextTemp = grid.LocationToIndex(nextTempLocation);
                if (ignoreLayers.IsCreated)
                {
                    if (ignoreLayers.Contains(nextTemp))
                    {
                        opened[i] = false;
                        openedIndices[i] = -1;
                        openedLocations[i] = -1;
                        continue;
                    }
                }

                opened[i] = true;
                openedIndices[i] = nextTemp;
                openedLocations[i] = nextTempLocation;
            }
        }
    }
}
