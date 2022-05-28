using UnityEditor;
using Syadeu.Presentation;
using SyadeuEditor.Utilities;
using Syadeu.Collections.PropertyDrawers;
using Syadeu.Collections.Editor;

namespace SyadeuEditor.Presentation
{
    [CustomPropertyDrawer(typeof(ConstActionReferenceArray), true)]
    internal sealed class ConstActionReferenceArrayPropertyDrawer : ArrayWrapperPropertyDrawerBase
    {
        protected override bool OverrideSingleLineElementGUI => true;
        protected override bool EnableExpanded => false;

        protected override void OnElementGUI(ref AutoRect rect, SerializedProperty child)
        {
            //float height = EditorGUI.GetPropertyHeight(child);
            //EditorGUI.PropertyField(rect.Pop(height), child);
        }
    }
    [CustomPropertyDrawer(typeof(ConstActionReferenceArray<>), true)]
    internal sealed class ConstActionReferenceArrayTPropertyDrawer : ArrayWrapperPropertyDrawerBase
    {
        protected override bool OverrideSingleLineElementGUI => true;
        protected override bool EnableExpanded => false;

        protected override void OnElementGUI(ref AutoRect rect, SerializedProperty child)
        {
            //float height = EditorGUI.GetPropertyHeight(child);
            //EditorGUI.PropertyField(rect.Pop(height), child);
        }
    }
}
