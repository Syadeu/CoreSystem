using Newtonsoft.Json;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Map
{
    [AttributeAcceptOnly(typeof(SceneDataEntity))]
    public abstract class SceneDataAttributeBase : AttributeBase { }

    public sealed class GridMapAttribute : SceneDataAttributeBase
    {
        [JsonProperty(Order = 0, PropertyName = "Center")] public int3 m_Center;
        [JsonProperty(Order = 1, PropertyName = "Size")] public int3 m_Size;
    }
    [Preserve]
    internal sealed class GridMapProcessor : AttributeProcessor<GridMapAttribute>
    {
        private static SceneDataEntity ToSceneData(IObject entity) => (SceneDataEntity)entity;

        protected override void OnCreated(GridMapAttribute attribute, IObject entity)
        {
            
        }
    }
}
