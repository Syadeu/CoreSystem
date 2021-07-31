using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Map
{
    [AttributeAcceptOnly(typeof(EntityBase))]
    public abstract class GridAttributeBase : AttributeBase { }

    #region Grid Size Attribute
    public sealed class GridSizeAttribute : GridAttributeBase
    {
        [JsonProperty(Order = 0, PropertyName = "GridLocations")] public int2[] m_GridLocations;

        [JsonIgnore] public GridSystem GridSystem { get; internal set; }

        public GridSizeAttribute()
        {
            m_GridLocations = new int2[] { int2.zero };
        }
    }
    //[Preserve]
    //internal sealed class GridSizeProcessor : AttributeProcessor<GridSizeAttribute>
    //{
    //    protected override void OnCreated(GridSizeAttribute attribute, EntityData<IEntityData> entity)
    //    {
            
    //    }
    //}
    #endregion
}
