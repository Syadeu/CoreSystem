using GraphProcessor;
using UnityEngine;

namespace Syadeu.Collections.Graphs
{
    [System.Serializable, NodeMenuItem("Primitives/String")]
    public class StringNode : BaseNode
    {
        [Output(name = "Out"), SerializeField]
        public string output;

        public override string name => "String";
    }
}
