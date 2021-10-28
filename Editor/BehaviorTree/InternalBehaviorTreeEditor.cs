using BehaviorDesigner.Editor;
using Syadeu.Presentation.BehaviorTree;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.BehaviorTree
{
    [CustomEditor(typeof(InternalBehaviorTree))]
    public class InternalBehaviorTreeEditor : BehaviorInspector
    {
    }
}
