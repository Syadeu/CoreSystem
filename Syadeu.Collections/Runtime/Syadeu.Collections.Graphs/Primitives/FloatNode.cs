﻿using GraphProcessor;
using UnityEngine;

namespace Syadeu.Collections.Graphs
{
    [System.Serializable, NodeMenuItem("Primitives/Float")]
    public class FloatNode : BaseNode
    {
        [Output("Out")]
        public float output;

        [SerializeField, Input("In")]
        public float input;

        public override string name => "Float";

        protected override void Process() => output = input;
    }
}
