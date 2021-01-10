using Syadeu.XNode;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if XNODE
using XNode;
using XNodeEditor;

namespace SyadeuEditor.XNode
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
