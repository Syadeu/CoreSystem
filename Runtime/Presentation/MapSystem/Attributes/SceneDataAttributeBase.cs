using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Map
{
    [AttributeAcceptOnly(typeof(SceneDataEntity))]
    public abstract class SceneDataAttributeBase : AttributeBase { }

    #region Grid Map Attribute
    public sealed class GridMapAttribute : SceneDataAttributeBase
    {
        public sealed class LayerInfo : IEquatable<LayerInfo>, ICloneable
        {
            [JsonProperty(PropertyName = "Hash")][ReflectionSealedView] public Hash m_Hash = Hash.NewHash();
            [JsonProperty(PropertyName = "Name")] public string m_Name = "NewLayer";
            [JsonProperty(PropertyName = "Indices")] public int[] m_Indices = Array.Empty<int>();

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
        [JsonIgnore] public ManagedGrid Grid { get; private set; }
        [JsonIgnore] public NativeHashSet<int>[] Layers { get; private set; }

        public void CreateGrid()
        {
            if (Grid != null) throw new Exception();

            Grid = new ManagedGrid(m_Center, m_Size, m_CellSize);
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
            if (Grid == null) throw new Exception();

            for (int i = 0; i < Layers.Length; i++)
            {
                Layers[i].Dispose();
            }

            Grid.Dispose();
            Grid = null;
        }

        public LayerInfo GetLayer(int idx) => m_Layers[idx];
        public LayerInfo GetLayer(Hash hash) => m_Layers.FindFor((other) => other.m_Hash.Equals(hash));
        public LayerInfo GetLayer(string name) => m_Layers.FindFor((other) => other.m_Name.Equals(name));
    }
    [Preserve]
    internal sealed class GridMapProcessor : AttributeProcessor<GridMapAttribute>
    {
        private static SceneDataEntity ToSceneData(IObject entity) => (SceneDataEntity)entity;

        protected override void OnCreated(GridMapAttribute attribute, IObject entity)
        {
            attribute.CreateGrid();
        }
        protected override void OnDestroy(GridMapAttribute attribute, IObject entity)
        {
            attribute.DestroyGrid();
        }
    }
    #endregion
}
