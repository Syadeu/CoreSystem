using Newtonsoft.Json;
using Syadeu.Database;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Map
{
    [AttributeAcceptOnly(typeof(SceneDataEntity))]
    public abstract class SceneDataAttributeBase : AttributeBase { }

    #region Grid Map Attribute
    public sealed class GridMapAttribute : SceneDataAttributeBase
    {
        [JsonProperty(Order = 0, PropertyName = "Center")] public int3 m_Center;
        [JsonProperty(Order = 1, PropertyName = "Size")] public int3 m_Size;
        [JsonProperty(Order = 2, PropertyName = "CellSize")] public float m_CellSize;

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
