using Newtonsoft.Json;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    public abstract class InstanceActionBase : ActionBase
    {
        protected virtual void OnInitialize() { }
        protected virtual void OnTerminate() { }
    }
}
