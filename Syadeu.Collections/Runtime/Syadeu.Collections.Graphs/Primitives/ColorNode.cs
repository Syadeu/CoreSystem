using GraphProcessor;
using UnityEngine;

namespace Syadeu.Collections.Graphs
{
    [System.Serializable, NodeMenuItem("Primitives/Color")]
    public class ColorNode : BaseNode
    {
        [Output(name = "Color"), SerializeField]
        new public Color color;

        public override string name => "Color";
    }
}
