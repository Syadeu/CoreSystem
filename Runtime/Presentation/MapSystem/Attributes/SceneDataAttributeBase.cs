using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Attributes;
using System;
using System.Collections.Generic;
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
        }

        [JsonProperty(Order = 0, PropertyName = "Center")] public int3 m_Center;
        [JsonProperty(Order = 1, PropertyName = "Size")] public int3 m_Size;
        [JsonProperty(Order = 2, PropertyName = "CellSize")] public float m_CellSize;
        [JsonProperty(Order = 3, PropertyName = "CellLayers")] public LayerInfo[] m_Layers;

        [JsonIgnore] public ManagedGrid Grid { get; internal set; }
    }
    [Preserve]
    internal sealed class GridMapProcessor : AttributeProcessor<GridMapAttribute>
    {
        private static SceneDataEntity ToSceneData(IObject entity) => (SceneDataEntity)entity;

        protected override void OnCreated(GridMapAttribute attribute, IObject entity)
        {
            //SceneDataEntity sceneData = ToSceneData(entity);

            attribute.Grid = new ManagedGrid(attribute.m_Center, attribute.m_Size, attribute.m_CellSize);
        }
        protected override void OnDestroy(GridMapAttribute attribute, IObject entity)
        {
            attribute.Grid.Dispose();
            attribute.Grid = null;
        }
    }
    #endregion
}
