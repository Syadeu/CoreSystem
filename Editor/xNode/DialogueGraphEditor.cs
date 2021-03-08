using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if XNODE
using XNode;
using XNodeEditor;

using Syadeu.xNode;

namespace SyadeuEditor.xNode
{
    [CustomNodeGraphEditor(typeof(DialogueGraph))]
    public class DialogueGraphEditor : NodeGraphEditor
    {
        public override string GetNodeMenuName(Type type)
        {
            if (typeof(DialogueNodeBase).IsAssignableFrom(type)) return base.GetNodeMenuName(type);
            else return null;
        }
    }

}
#endif
