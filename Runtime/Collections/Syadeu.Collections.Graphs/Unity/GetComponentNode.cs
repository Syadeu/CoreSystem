using GraphProcessor;
using UnityEngine;

namespace Syadeu.Collections.Graphs
{
    [System.Serializable, NodeMenuItem("Unity/Get Component")]
    public abstract class GetComponentNode<TComponent> : BaseNode
        where TComponent : Component
    {
        [Input(name = "Target")]
        protected object target;

        [Output(name = "Output")]
        protected TComponent output;

        protected override sealed void Process()
        {
            if (target == null ||
                !TypeHelper.InheritsFrom<UnityEngine.Object>(target.GetType())) return;

            if (target is GameObject gameObject)
            {
                output = gameObject.GetComponentInChildren<TComponent>();
            }
            else if (target is Component component)
            {
                output = component.GetComponent<TComponent>();
            }
        }
    }
}
