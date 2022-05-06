using GraphProcessor;
using UnityEngine;

namespace Syadeu.Collections.Graphs
{
    [System.Serializable, NodeMenuItem("Primitives/Vector3")]
    public class Vector3Node : BaseNode
    {
        [Output(name = "Out")]
        public Vector3 output;

        [Input(name = "In"), SerializeField]
        public Vector3 input;

        public override string name => "Vector3";

        protected override void Process()
        {
            output = input;
        }
    }
}
