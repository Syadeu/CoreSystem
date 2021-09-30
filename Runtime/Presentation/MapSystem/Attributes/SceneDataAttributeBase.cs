using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using System.Linq;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Map
{
    [AttributeAcceptOnly(typeof(SceneDataEntity))]
    public abstract class SceneDataAttributeBase : AttributeBase { }
    
}
