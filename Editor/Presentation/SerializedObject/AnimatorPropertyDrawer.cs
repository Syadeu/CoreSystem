using UnityEngine;
using UnityEditor;
using Syadeu.Presentation.Attributes;
using SyadeuEditor.Utilities;

namespace SyadeuEditor.Presentation
{
    [CustomPropertyDrawer(typeof(AnimatorAttribute))]
    public sealed class AnimatorPropertyDrawer : CoreSystemObjectPropertyDrawer<AnimatorAttribute>
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = base.GetPropertyHeight(property, label) + PropertyDrawerHelper.GetPropertyHeight(2);

            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("m_AnimationTriggers"));

            return height;
        }

        protected override void OnPropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            base.OnPropertyGUI(ref rect, property, label);

            SerializedProperty
                enableRootMotionProp = property.FindPropertyRelative("m_EnableRootMotion"),
                manualRootMotion = property.FindPropertyRelative("m_ManualRootMotion"),

                animationTriggers = property.FindPropertyRelative("m_AnimationTriggers");

            EditorGUI.PropertyField(rect.Pop(), enableRootMotionProp);
            EditorGUI.PropertyField(rect.Pop(), manualRootMotion);

            EditorGUI.PropertyField(rect.Pop(EditorGUI.GetPropertyHeight(animationTriggers)), animationTriggers, true);
        }
    }
}
