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
    [System.Serializable, NodeMenuItem("Primitives/Vector2")]
    public class Vector2Node : BaseNode
    {
        [Output(name = "Out")]
        public Vector2 output;

        [Input(name = "In"), SerializeField]
        public Vector2 input;

        public override string name => "Vector2";

        protected override void Process()
        {
            output = input;
        }
    }
}
