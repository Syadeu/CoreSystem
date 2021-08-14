using Syadeu.Presentation.Entities;
using UnityEngine.Scripting;
//using Syadeu.ThreadSafe;

namespace Syadeu.Presentation.Attributes
{
    [AttributeAcceptOnly(typeof(EntityBase))]
    public sealed class NavObstacleAttribute : AttributeBase
    {

    }
    [Preserve]
    internal sealed class NavObstacleProcesor : AttributeProcessor<NavObstacleAttribute>
    {

    }
}
