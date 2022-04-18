using GraphProcessor;
using System;
using System.Collections;
using UnityEngine;

namespace Syadeu.Collections.Graphs
{
    [Serializable]
    public class VisualGraph : BaseGraph
    {
    }

    public class TestStackNode : BaseStackNode
    {
        public TestStackNode(Vector2 position, string title = "Stack", bool acceptDrop = true, bool acceptNewNode = true) : base(position, title, acceptDrop, acceptNewNode)
        {
        }
    }
}
