using GraphProcessor;
using UnityEngine;

namespace Syadeu.Collections.Graphs
{
    public class IntNode : BaseNode
    {
        [Output("Out")]
        public int output;

        [SerializeField, Input("In")]
        public int input;

        public override string name => "Int";

        protected override void Process()
        {
            output = input;
        }
    }
}
