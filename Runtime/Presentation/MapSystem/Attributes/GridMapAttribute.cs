using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Render;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Map
{
    [DisplayName("Attribute: Grid map")]
    [ReflectionDescription("엔티티가 생성되면 자동으로 입력한 크기의 그리드를 생성합니다.")]
    public sealed class GridMapAttribute : SceneDataAttributeBase
    {
        [Serializable]
        public sealed class LayerInfo : IEquatable<LayerInfo>, ICloneable
        {
            [JsonProperty(Order = 0, PropertyName = "Name")] public string m_Name = "NewLayer";
            [ReflectionSealedView, JsonProperty(Order = 1, PropertyName = "Hash")] public Hash m_Hash = Hash.NewHash();
            [ReflectionDescription("반대로 적용합니다.")]
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
        [JsonIgnore] public int GridCellCapacity => Grid.length;
        [JsonIgnore] private BinaryGrid Grid { get; set; }
        [JsonIgnore] private BinaryGrid[] SubGrids { get; set; }
        [JsonIgnore] private NativeHashSet<int>[] Layers { get; set; }

        [JsonIgnore] public List<int> m_ObstacleLayerIndices = new List<int>();
        [JsonIgnore] public NativeHashSet<int> ObstacleLayer { get; private set; }

        [JsonIgnore] public Mesh CellMesh { get; private set; }
        [JsonIgnore] public Material CellMaterial { get; private set; }

        public void CreateGrid()
        {
            Grid = new BinaryGrid(m_Center, m_Size, m_CellSize);
            Layers = new NativeHashSet<int>[m_Layers.Length];
            for (int i = 0; i < m_Layers.Length; i++)
            {
                Layers[i] = new NativeHashSet<int>(m_Layers[i].m_Indices.Length, Allocator.Persistent);
                for (int a = 0; a < m_Layers[i].m_Indices.Length; a++)
                {
                    Layers[i].Add(m_Layers[i].m_Indices[a]);
                }
            }

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

            for (int i = 0; i < Layers.Length; i++)
            {
                Layers[i].Dispose();
            }
            Layers = null;

            if (ObstacleLayer.IsCreated) ObstacleLayer.Dispose();

            //Grid.Dispose();
            //Grid = null;
        }
        public void SetObstacleLayers(params int[] layers)
        {
            if (ObstacleLayer.IsCreated) ObstacleLayer.Dispose();
            ObstacleLayer = new NativeHashSet<int>(4096, Allocator.Persistent);
            m_ObstacleLayerIndices.Clear();
            for (int i = 0; i < layers.Length; i++)
            {
                m_ObstacleLayerIndices.Add(layers[i]);
            }

            for (int i = 0; i < layers.Length; i++)
            {
                foreach (var item in m_Layers[layers[i]].m_Indices)
                {
                    ObstacleLayer.Add(item);
                }
            }
        }
        public void AddObstacleLayers(params int[] layers)
        {
            if (!ObstacleLayer.IsCreated)
            {
                ObstacleLayer = new NativeHashSet<int>(4096, Allocator.Persistent);
            }

            for (int i = 0; i < layers.Length; i++)
            {
                if (m_ObstacleLayerIndices.Contains(layers[i]))
                {
                    "already added".ToLogError();
                    continue;
                }

                m_ObstacleLayerIndices.Add(layers[i]);
                foreach (var item in m_Layers[layers[i]].m_Indices)
                {
                    ObstacleLayer.Add(item);
                }
            }
        }
        protected override void OnDispose()
        {
            ////if (Grid != null)
            //{
            //    //Grid.Dispose();
            //    //Grid = null;
            //}

            if (Layers != null)
            {
                for (int i = 0; i < Layers.Length; i++)
                {
                    Layers[i].Dispose();
                }
                Layers = null;
            }

            if (ObstacleLayer.IsCreated) ObstacleLayer.Dispose();
        }

        #region Filter

        public FixedList32Bytes<int> FilterByLayer32(in int layer, in FixedList32Bytes<int> indices)
        {
            FixedList32Bytes<int> temp = new FixedList32Bytes<int>();
            for (int i = 0; i < indices.Length; i++)
            {
                if (m_Layers[layer].m_Inverse)
                {
                    if (!Layers[layer].Contains(indices[i]))
                    {
                        //filtered.Add(indices[i]);
                        continue;
                    }
                }
                else
                {
                    if (Layers[layer].Contains(indices[i]))
                    {
                        //filtered.Add(indices[i]);
                        continue;
                    }
                }

                temp.Add(indices[i]);
            }
            return temp;
        }
        public FixedList64Bytes<int> FilterByLayer64(in int layer, in FixedList64Bytes<int> indices)
        {
            FixedList64Bytes<int> temp = new FixedList64Bytes<int>();
            for (int i = 0; i < indices.Length; i++)
            {
                if (m_Layers[layer].m_Inverse)
                {
                    if (!Layers[layer].Contains(indices[i]))
                    {
                        //filtered.Add(indices[i]);
                        continue;
                    }
                }
                else
                {
                    if (Layers[layer].Contains(indices[i]))
                    {
                        //filtered.Add(indices[i]);
                        continue;
                    }
                }

                temp.Add(indices[i]);
            }
            return temp;
        }
        public FixedList128Bytes<int> FilterByLayer128(in int layer, in FixedList128Bytes<int> indices)
        {
            FixedList128Bytes<int> temp = new FixedList128Bytes<int>();
            for (int i = 0; i < indices.Length; i++)
            {
                if (m_Layers[layer].m_Inverse)
                {
                    if (!Layers[layer].Contains(indices[i]))
                    {
                        //filtered.Add(indices[i]);
                        continue;
                    }
                }
                else
                {
                    if (Layers[layer].Contains(indices[i]))
                    {
                        //filtered.Add(indices[i]);
                        continue;
                    }
                }

                temp.Add(indices[i]);
            }
            return temp;
        }
        public FixedList512Bytes<int> FilterByLayer512(in int layer, in FixedList512Bytes<int> indices)
        {
            FixedList512Bytes<int> temp = new FixedList512Bytes<int>();
            for (int i = 0; i < indices.Length; i++)
            {
                if (m_Layers[layer].m_Inverse)
                {
                    if (!Layers[layer].Contains(indices[i]))
                    {
                        //filtered.Add(indices[i]);
                        continue;
                    }
                }
                else
                {
                    if (Layers[layer].Contains(indices[i]))
                    {
                        //filtered.Add(indices[i]);
                        continue;
                    }
                }

                temp.Add(indices[i]);
            }
            return temp;
        }
        public FixedList4096Bytes<int> FilterByLayer1024(in int layer, in FixedList4096Bytes<int> indices)
        {
            FixedList4096Bytes<int> temp = new FixedList4096Bytes<int>();
            for (int i = 0; i < indices.Length; i++)
            {
                if (m_Layers[layer].m_Inverse)
                {
                    if (!Layers[layer].Contains(indices[i]))
                    {
                        //filtered.Add(indices[i]);
                        continue;
                    }
                }
                else
                {
                    if (Layers[layer].Contains(indices[i]))
                    {
                        //filtered.Add(indices[i]);
                        continue;
                    }
                }

                temp.Add(indices[i]);
            }
            return temp;
        }

        public void FilterByLayer(in int layer, ref NativeList<int> indices)
        {
            for (int i = indices.Length - 1; i >= 0; i--)
            {
                if (m_Layers[layer].m_Inverse)
                {
                    if (!Layers[layer].Contains(indices[i]))
                    {
                        //filtered.Add(indices[i]);
                        indices.RemoveAt(i);
                        continue;
                    }
                }
                else
                {
                    if (Layers[layer].Contains(indices[i]))
                    {
                        //filtered.Add(indices[i]);
                        indices.RemoveAt(i);
                        continue;
                    }
                }
            }
        }

        [Obsolete]
        public int[] FilterByLayer(int layer, int[] indices, out int[] filteredIndices)
        {
            List<int> temp = new List<int>();
            List<int> filtered = new List<int>();
            for (int i = 0; i < indices.Length; i++)
            {
                if (m_Layers[layer].m_Inverse)
                {
                    if (!Layers[layer].Contains(indices[i]))
                    {
                        filtered.Add(indices[i]);
                        continue;
                    }
                }
                else
                {
                    if (Layers[layer].Contains(indices[i]))
                    {
                        filtered.Add(indices[i]);
                        continue;
                    }
                }

                temp.Add(indices[i]);
            }
            filteredIndices = filtered.Count == 0 ? Array.Empty<int>() : filtered.ToArray();
            return temp.ToArray();
        }
        [Obsolete]
        public int[] FilterByLayer(Hash layer, int[] indices, out int[] filteredIndices)
            => FilterByLayer(GetLayer(layer), indices, out filteredIndices);
        [Obsolete]
        public int[] FilterByLayer(string layer, int[] indices, out int[] filteredIndices)
            => FilterByLayer(GetLayer(layer), indices, out filteredIndices);

        #endregion

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
        private BinaryGrid GetTargetGrid(in int index, out int targetIndex)
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

                if (!found) return GridPosition.Empty;
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

            return new GridPosition(grid.LocationToIndex(temp), temp);
        }

        #endregion

        #region Layers

        public int[] GetLayer(in int layer)
        {
            return m_Layers[layer].m_Indices;
        }
        public int GetLayer(Hash hash)
        {
            for (int i = 0; i < m_Layers.Length; i++)
            {
                if (m_Layers[i].m_Hash.Equals(hash)) return i;
            }
            return -1;
        }
        public int GetLayer(string name)
        {
            for (int i = 0; i < m_Layers.Length; i++)
            {
                if (m_Layers[i].m_Name.Equals(name)) return i;
            }
            return -1;
        }

        public bool LayerContains(in int layer, in int index)
        {
            return Layers[layer].Contains(index);
        }

        #endregion

        #region Get Ranges

        [Obsolete]
        public int[] GetRange(int idx, int range, params int[] ignoreLayers)
        {
            var grid = GetTargetGrid(in idx, out int targetIdx);

            // TODO : 임시. 이후 gridsize 에 맞춰서 인덱스 반환
            int[] temp = grid.GetRange(in targetIdx, in range);
            for (int i = 0; i < ignoreLayers?.Length; i++)
            {
                temp = FilterByLayer(ignoreLayers[i], temp, out _);
            }

            return temp;
        }
        public FixedList32Bytes<int> GetRange8(in int idx, in int range, in FixedList128Bytes<int> ignoreLayers)
        {
            var grid = GetTargetGrid(in idx, out int targetIdx);

            FixedList32Bytes<int> temp = grid.GetRange8(in targetIdx, in range);
            for (int i = 0; i < ignoreLayers.Length; i++)
            {
                temp = FilterByLayer32(ignoreLayers[i], in temp);
            }

            return temp;
        }
        public FixedList64Bytes<int> GetRange16(in int idx, in int range, in FixedList128Bytes<int> ignoreLayers)
        {
            var grid = GetTargetGrid(in idx, out int targetIdx);

            FixedList64Bytes<int> temp = grid.GetRange16(in targetIdx, in range);
            for (int i = 0; i < ignoreLayers.Length; i++)
            {
                temp = FilterByLayer64(ignoreLayers[i], in temp);
            }

            return temp;
        }
        public FixedList128Bytes<int> GetRange32(in int idx, in int range, in FixedList128Bytes<int> ignoreLayers)
        {
            var grid = GetTargetGrid(in idx, out int targetIdx);

            FixedList128Bytes<int> temp = grid.GetRange32(in targetIdx, in range);
            for (int i = 0; i < ignoreLayers.Length; i++)
            {
                temp = FilterByLayer128(ignoreLayers[i], in temp);
            }

            return temp;
        }
        public FixedList4096Bytes<int> GetRange1024(in int idx, in int range, in FixedList128Bytes<int> ignoreLayers)
        {
            var grid = GetTargetGrid(in idx, out int targetIdx);

            FixedList4096Bytes<int> temp = grid.GetRange1024(in targetIdx, in range);
            for (int i = 0; i < ignoreLayers.Length; i++)
            {
                temp = FilterByLayer1024(ignoreLayers[i], in temp);
            }

            return temp;
        }
        public void GetRange(ref NativeList<int> list, in int idx, in int range, in FixedList128Bytes<int> ignoreLayers)
        {
            var grid = GetTargetGrid(in idx, out int targetIdx);

            grid.GetRange(ref list, in targetIdx, in range);
            for (int i = 0; i < ignoreLayers.Length; i++)
            {
                FilterByLayer(ignoreLayers[i], ref list);
            }
        }

        #endregion

        #region GL

        public void DrawGridGL(float thinkness) => DrawGridGL(Grid, thinkness);
        public void DrawOccupiedCells(int[] gridEntities) => DrawOccupiedCells(Grid, gridEntities);

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
