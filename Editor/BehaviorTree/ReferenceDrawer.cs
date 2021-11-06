using BehaviorDesigner.Editor;
using Syadeu.Collections;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.BehaviorTree
{
    [CustomObjectDrawer(typeof(IFixedReference))]
    public sealed class ReferenceDrawer : ObjectDrawer
    {
        public override void OnGUI(GUIContent label)
        {
            EditorGUILayout.LabelField("test");

            base.OnGUI(label);
        }
    }
}
