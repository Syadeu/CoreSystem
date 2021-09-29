using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Collections;
using Unity.Mathematics;
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

        [JsonProperty(Order = 0, PropertyName = "Center")] private int3 m_Center;
        [JsonProperty(Order = 1, PropertyName = "Size")] private int3 m_Size;
        [JsonProperty(Order = 2, PropertyName = "CellSize")] private float m_CellSize;
        [JsonProperty(Order = 3, PropertyName = "CellLayers")]
        public LayerInfo[] m_Layers = new LayerInfo[] {new LayerInfo()
        {
            m_Name = "Default"
        }};

        [JsonIgnore] public int3 Center => m_Center;
        [JsonIgnore] public int3 Size => m_Size;
        [JsonIgnore] public float CellSize => m_CellSize;
        [JsonIgnore] public int LayerCount => m_Layers.Length;
        [JsonIgnore] public BinaryGrid Grid { get; private set; }
        [JsonIgnore] private NativeHashSet<int>[] Layers { get; set; }

        [JsonIgnore] public FixedList32Bytes<int> m_ObstacleLayerIndices = new FixedList32Bytes<int>();
        [JsonIgnore] public NativeHashSet<int> ObstacleLayer { get; private set; }

        public void CreateGrid()
        {
            //if (Grid != null) throw new Exception();

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
                        indices.RemoveAtSwapBack(indices[i]);
                        continue;
                    }
                }
                else
                {
                    if (Layers[layer].Contains(indices[i]))
                    {
                        //filtered.Add(indices[i]);
                        indices.RemoveAtSwapBack(indices[i]);
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

        public bool LayerContains(in int layer, in int index)
        {
            return Layers[layer].Contains(index);
        }
    }
    [Preserve]
    internal sealed class GridMapProcessor : AttributeProcessor<GridMapAttribute>
    {
        private GridSystem m_GridSystem;

        protected override void OnInitialize()
        {
            RequestSystem<GridSystem>(Bind);
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
