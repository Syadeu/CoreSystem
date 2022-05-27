using GraphProcessor;
using UnityEngine;

namespace Syadeu.Collections.Graphs
{
    [System.Serializable, NodeMenuItem("Primitives/Vector4")]
    public class Vector4Node : BaseNode
    {
        [Output(name = "Out")]
        public Vector4 output;

        [Input(name = "In"), SerializeField]
        public Vector4 input;

        public override string name => "Vector4";

        protected override void Process()
        {
            output = input;
        }
    }
}
