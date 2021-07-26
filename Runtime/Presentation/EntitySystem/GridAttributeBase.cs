using Newtonsoft.Json;
using Unity.Mathematics;

namespace Syadeu.Presentation
{
    [AttributeAcceptOnly(typeof(EntityBase))]
    public abstract class GridAttributeBase : AttributeBase { }

    public sealed class GridSizeAttribute : GridAttributeBase
    {
        [JsonProperty] public int2 m_GridSize;
    }
}
