using GraphProcessor;
using System;
using UnityEngine;

namespace Syadeu.Collections.Graphs
{
    [Serializable, NodeMenuItem("Logic/Entry")]
    public class EntryNode : BaseNode
    {
        [SerializeField, Output("Out")]
        public bool block;

        public override string name => "Entry";
    }
}
