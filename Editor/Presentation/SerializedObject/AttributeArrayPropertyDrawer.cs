using UnityEngine;
using UnityEditor;
using Syadeu.Presentation;
using Syadeu.Presentation.Attributes;
using SyadeuEditor.Utilities;
using Syadeu.Presentation.Entities;
using Newtonsoft.Json;
using Syadeu.Collections.Buffer.LowLevel;
using Syadeu.Collections;
using UnityEditor.AnimatedValues;

namespace SyadeuEditor.Presentation
{
    [CustomPropertyDrawer(typeof(AttributeArray))]
    internal sealed class AttributeArrayPropertyDrawer : ArrayWrapperPropertyDrawerBase
    {
        protected override void OnElementGUI(ref AutoRect rect, SerializedProperty child)
        {
            float height = EditorGUI.GetPropertyHeight(child);
            EditorGUI.PropertyField(rect.Pop(height), child);
        }
    }
}
