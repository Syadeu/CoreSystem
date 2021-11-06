using BehaviorDesigner.Editor;
using Syadeu.Collections;
using Syadeu.Presentation.BehaviorTree;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.BehaviorTree
{
    [CustomObjectDrawer(typeof(EntityReferenceAttribute))]
    public sealed class ReferenceDrawer : ObjectDrawer
    {
        public override void OnGUI(GUIContent label)
        {
            EditorGUILayout.LabelField("test");

            base.OnGUI(label);
        }
    }
}
