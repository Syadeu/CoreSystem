using Newtonsoft.Json;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Map
{
    [AttributeAcceptOnly(typeof(EntityBase))]
    public abstract class GridAttributeBase : AttributeBase { }

    #region Grid Size Attribute
    public sealed class GridSizeAttribute : GridAttributeBase
    {
        [JsonProperty(Order = 0, PropertyName = "GridSize")] public int2 m_GridSize;
    }
    //[Preserve]
    //internal sealed class GridSizeProcessor : AttributeProcessor<GridSizeAttribute>
    //{
    //    protected override void OnCreated(GridSizeAttribute attribute, IObject entity)
    //    {

    //    }
    //}
    #endregion
}
