// Copyright 2021 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Internal;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Render;
using System;
using System.ComponentModel;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Map
{
    [DisplayName("Attribute: Grid map")]
    [Description("엔티티가 생성되면 자동으로 입력한 크기의 그리드를 생성합니다.")]
    public sealed class GridMapAttribute : SceneDataAttributeBase
    {
        [Serializable]
        public sealed class LayerInfo : IEquatable<LayerInfo>, ICloneable
        {
            [JsonProperty(Order = 0, PropertyName = "Name")] public string m_Name = "NewLayer";
            [ReflectionSealedView, JsonProperty(Order = 1, PropertyName = "Hash")] public Hash m_Hash = Hash.NewHash();
            [Description("반대로 적용합니다.")]
            [JsonProperty(Order = 2, PropertyName = "Inverse")] public bool m_Inverse = false;
            [JsonProperty(Order = 3, PropertyName = "Indices")] public int[] m_Indices = Array.Empty<int>();

            public object Clone()
            {
                LayerInfo c = (LayerInfo)MemberwiseClone();
                c.m_Hash = Hash.NewHash();
                c.m_Name = string.Copy(m_Name);
                c.m_Indices = new int[m_Indices.Length];
                Array.Copy(m_Indices, c.m_Indices, m_Indices.Length);

                return c;
            }
            public bool Equals(LayerInfo other) => m_Hash.Equals(other.m_Hash);

            public static implicit operator Hash(LayerInfo a) => a.m_Hash;
        }
        [Serializable]
        public sealed class SubGrid
        {
            [JsonProperty(Order = 0, PropertyName = "Name")] public string m_Name;
            [JsonProperty(Order = 1, PropertyName = "Center")] public int3 m_Center;
            [JsonProperty(Order = 2, PropertyName = "Size")] public int3 m_Size;
        }

        [JsonProperty(Order = 0, PropertyName = "Center")] private int3 m_Center;
        [JsonProperty(Order = 1, PropertyName = "Size")] private int3 m_Size;
        [JsonProperty(Order = 2, PropertyName = "CellSize")] private float m_CellSize;
        [JsonProperty(Order = 3, PropertyName = "CellLayers")]
        public LayerInfo[] m_Layers = new LayerInfo[] {new LayerInfo()
        {
            m_Name = "Default"
        }};

        [Header("Sub Grid")]
        [JsonProperty(Order = 4, PropertyName = "SubGrids")]
        private SubGrid[] m_SubGrids = Array.Empty<SubGrid>();

        [Header("Cell Overray UI")]
        [JsonProperty(Order = 5, PropertyName = "CellUIPrefab")]
        internal Reference<UIObjectEntity> m_CellUIPrefab = Reference<UIObjectEntity>.Empty;

        [JsonIgnore] public int3 Center => m_Center;
        [JsonIgnore] public int3 Size => m_Size;
        [JsonIgnore] public float CellSize => m_CellSize;
        [JsonIgnore] public int LayerCount => m_Layers.Length;
        [JsonIgnore] private BinaryGrid Grid { get; set; }
        [JsonIgnore] private BinaryGrid[] SubGrids { get; set; }
        //[JsonIgnore] private NativeHashSet<int>[] Layers { get; set; }

        //[JsonIgnore] public List<int> m_ObstacleLayerIndices = new List<int>();
        //[JsonIgnore] public NativeHashSet<int> ObstacleLayer { get; private set; }
        [JsonIgnore] public int Length
        {
            get
            {
                int temp = Grid.length;
                for (int i = 0; i < SubGrids.Length; i++)
                {
                    temp += SubGrids[i].length;
                }
                return temp;
            }
        }

        [JsonIgnore] public Mesh CellMesh { get; private set; }
        [JsonIgnore] public Material CellMaterial { get; private set; }

        public void CreateGrid()
        {
            Grid = new BinaryGrid(m_Center, m_Size, m_CellSize);
            //Layers = new NativeHashSet<int>[m_Layers.Length];
            //for (int i = 0; i < m_Layers.Length; i++)
            //{
            //    Layers[i] = new NativeHashSet<int>(m_Layers[i].m_Indices.Length, Allocator.Persistent);
            //    for (int a = 0; a < m_Layers[i].m_Indices.Length; a++)
            //    {
            //        Layers[i].Add(m_Layers[i].m_Indices[a]);
            //    }
            //}

            if (m_SubGrids.Length == 0)
            {
                SubGrids = Array.Empty<BinaryGrid>();
            }
            else
            {
                SubGrids = new BinaryGrid[m_SubGrids.Length];
                for (int i = 0; i < m_SubGrids.Length; i++)
                {
#if UNITY_EDITOR
                    if (m_SubGrids[i].m_Size.Equals(int3.zero))
                    {
                        CoreSystem.Logger.LogError(Channel.Presentation,
                            $"Sub Grid {m_SubGrids[i].m_Name} at {i} size is zero. This is not allowed.");
                    }
#endif
                    SubGrids[i] = new BinaryGrid(m_SubGrids[i].m_Center, m_SubGrids[i].m_Size, m_CellSize);
                }
#if UNITY_EDITOR
                for (int i = 0; i < SubGrids.Length; i++)
                {
                    if (Grid.bounds.Intersect(SubGrids[i].bounds))
                    {
                        CoreSystem.Logger.LogError(Channel.Presentation,
                            $"Sub Grid {m_SubGrids[i].m_Name} intersects with main grid. Are you intended?");
                    }
                }
#endif
            }

            float halfCell = m_CellSize * .5f;
            CellMesh = new Mesh();
            CellMesh.vertices = new Vector3[]
            {
                new Vector3(-halfCell, 0, halfCell),
                new Vector3(halfCell, 0, halfCell),
                new Vector3(halfCell, 0, -halfCell),
                new Vector3(-halfCell, 0, -halfCell)
            };
            CellMesh.triangles = new int[]
            {
                0, 1, 2,
                0, 2, 3
            };
            CellMesh.uv = new Vector2[]
            {
                new Vector2(0, 1),
                new Vector2(0, 1),
                new Vector2(0, 1),
                new Vector2(0, 1)
            };
            CellMesh.RecalculateBounds();

            CellMaterial = new Material(Shader.Find(RenderSystem.s_DefaultShaderName));
        }
        public void DestroyGrid()
        {
            //if (Grid == null) throw new Exception();

            //for (int i = 0; i < Layers.Length; i++)
            //{
            //    Layers[i].Dispose();
            //}
            //Layers = null;

            //if (ObstacleLayer.IsCreated) ObstacleLayer.Dispose();

            //Grid.Dispose();
            //Grid = null;
        }
        //public void SetObstacleLayers(params int[] layers)
        //{
        //    if (ObstacleLayer.IsCreated) ObstacleLayer.Dispose();
        //    ObstacleLayer = new NativeHashSet<int>(4096, Allocator.Persistent);
        //    m_ObstacleLayerIndices.Clear();
        //    for (int i = 0; i < layers.Length; i++)
        //    {
        //        m_ObstacleLayerIndices.Add(layers[i]);
        //    }

        //    for (int i = 0; i < layers.Length; i++)
        //    {
        //        foreach (var item in m_Layers[layers[i]].m_Indices)
        //        {
        //            ObstacleLayer.Add(item);
        //        }
        //    }
        //}
        //public void AddObstacleLayers(params int[] layers)
        //{
        //    if (!ObstacleLayer.IsCreated)
        //    {
        //        ObstacleLayer = new NativeHashSet<int>(4096, Allocator.Persistent);
        //    }

        //    for (int i = 0; i < layers.Length; i++)
        //    {
        //        if (m_ObstacleLayerIndices.Contains(layers[i]))
        //        {
        //            "already added".ToLogError();
        //            continue;
        //        }

        //        m_ObstacleLayerIndices.Add(layers[i]);
        //        foreach (var item in m_Layers[layers[i]].m_Indices)
        //        {
        //            ObstacleLayer.Add(item);
        //        }
        //    }
        //}

        #region Gets

        private void CalculateSubGridIndex(in int index, out int gridIdx, out int targetIndex)
        {
            gridIdx = -1;
            if (index < Grid.length)
            {
                targetIndex = index;
                return;
            }

            targetIndex = index - Grid.length;
            for (int i = 0; i < SubGrids.Length; i++)
            {
                if (targetIndex < SubGrids.Length)
                {
                    gridIdx = i;
                    break;
                }

                targetIndex -= SubGrids.Length;
            }
        }
        private int ConvertToWorldIndex(in int gridIdx, in int index)
        {
            if (gridIdx < 0)
            {
                if (Grid.length < index)
                {
                    return -1;
                }
                return index;
            }

            int output = Grid.length;
            for (int i = 0; i < gridIdx - 1; i++)
            {
                output += SubGrids[i].length;
            }

            output += index;
            if (SubGrids[gridIdx].length < output)
            {
                output = -1;
            }

            return output;
        }
        public BinaryGrid GetTargetGrid(in int index, out int targetIndex)
        {
            CalculateSubGridIndex(in index, out int gridIdx, out targetIndex);

            if (gridIdx < 0)
            {
                targetIndex = index;
                return Grid;
            }

            return SubGrids[gridIdx];
        }

        public GridPosition GetGridPosition(in float3 position)
        {
            BinaryGrid targetGrid = default(BinaryGrid);
            int index = 0;
            if (!Grid.HasCell(in position))
            {
                index += Grid.length;
                bool found = false;
                for (int i = 0; i < SubGrids.Length; i++)
                {
                    if (SubGrids[i].HasCell(in position))
                    {
                        targetGrid = SubGrids[i];

                        index += targetGrid.PositionToIndex(position);

                        found = true;
                        break;
                    }

                    index += SubGrids[i].length;
                }

                if (!found)
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"Could not found an valid grid position for {position}.");

                    return GridPosition.Empty;
                }
            }
            else
            {
                targetGrid = Grid;
                index = targetGrid.PositionToIndex(position);
            }

            return new GridPosition(
                index,
                targetGrid.PositionToLocation(position)
                );
        }
        public GridPosition GetGridPosition(in int idx)
        {
            BinaryGrid grid = GetTargetGrid(in idx, out int targetIndex);

            return new GridPosition(idx, grid.IndexToLocation(in targetIndex));
        }
        public float3 GetPosition(in GridPosition position)
        {
            CalculateSubGridIndex(in position.index, out int gridIdx, out int index);
            if (gridIdx < 0) return Grid.IndexToPosition(in position.index);

            return SubGrids[gridIdx].IndexToPosition(in index);
        }
        public float3 GetPosition(in int index)
        {
            BinaryGrid grid = GetTargetGrid(in index, out int targetIndex);

            return grid.IndexToPosition(in targetIndex);
        }
        public int2 GetLocation(in int index)
        {
            BinaryGrid grid = GetTargetGrid(in index, out int targetIndex);

            return grid.IndexToLocation(in targetIndex);
        }

        public int GetIndex(in float3 position)
        {
            return GetGridPosition(in position).index;
        }

        public GridPosition GetDirection(in int from, in Direction direction)
        {
            CalculateSubGridIndex(in from, out int gridIdx, out int idx);
            BinaryGrid grid = gridIdx < 0 ? Grid : SubGrids[gridIdx];

            int2 location = grid.GetDirection(in idx, in direction);

            int cellIdx = grid.LocationToIndex(location);

            int worldIdx = ConvertToWorldIndex(in gridIdx, in cellIdx);

            if (worldIdx < 0) return GridPosition.Empty;
            return new GridPosition(worldIdx, location);
        }

        #endregion

        #region Math

        public GridPosition Add(in GridPosition pos, in int2 location)
        {
            BinaryGrid grid = GetTargetGrid(in pos.index, out _);
            int2 temp = pos.location + location;

            var output = new GridPosition(grid.LocationToIndex(temp), temp);

            if (output.index < 0)
            {
                $"{output.index} error {pos.location} + {location} = {pos.location + location}".ToLogError();
            }

            return output;
        }

        #endregion

        #region Layers

        public int[] GetLayer(in int layer)
        {
            return m_Layers[layer].m_Indices;
        }

        #endregion

        #region GL

        public void DrawGridGL(float thinkness) => DrawGridGL(Grid, thinkness);
        public void DrawOccupiedCells(int[] gridEntities) => DrawOccupiedCells(Grid, gridEntities);
        public void DrawOccupiedCells(in NativeArray<int> gridEntities) => DrawOccupiedCells(Grid, in gridEntities);

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
        static void DrawOccupiedCells(in BinaryGrid grid, in NativeArray<int> gridEntities)
        {
            float sizeHalf = grid.cellSize * .5f;

            for (int i = 0; i < gridEntities.Length; i++)
            {
#if CORESYSTEM_SHAPES
                Vector3 cellPos = grid.IndexToPosition(gridEntities[i]);
                Shapes.Draw.Rectangle(cellPos + new Vector3(0, .05f, 0), Vector3.up, new Vector2(grid.cellSize, grid.cellSize), Color.red);
#else
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
#endif
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

#endregion
    }
    [Preserve]
    internal sealed class GridMapProcessor : AttributeProcessor<GridMapAttribute>
    {
        private GridSystem m_GridSystem;

        protected override void OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, GridSystem>(Bind);
        }
        private void Bind(GridSystem other)
        {
            m_GridSystem = other;
        }

        protected override void OnCreated(GridMapAttribute attribute, EntityData<IEntityData> entity)
        {
            attribute.CreateGrid();
            m_GridSystem.RegisterGrid(attribute);
        }
        protected override void OnDestroy(GridMapAttribute attribute, EntityData<IEntityData> entity)
        {
            m_GridSystem.UnregisterGrid(attribute);
            attribute.DestroyGrid();
        }
    }
}
