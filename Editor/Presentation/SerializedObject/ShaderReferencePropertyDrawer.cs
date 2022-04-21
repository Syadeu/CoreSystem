using SyadeuEditor.Utilities;
using Syadeu.Presentation.Render;
using UnityEditor;
using UnityEngine;
using System;

namespace SyadeuEditor.Presentation
{
    internal sealed class ShaderReferencePropertyDrawer : PropertyDrawer<ShaderReference>
    {
        private bool m_Changed = false;

        private SerializedProperty GetIndexProperty(SerializedProperty property)
        {
            const string c_Str = "m_Index";
            return property.FindPropertyRelative(c_Str);
        }

        protected override void BeforePropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            if (m_Changed)
            {
                GUI.changed = true;
                m_Changed = false;
            }
        }
        protected override void OnPropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            #region Rect Setup

            Rect
                propertyRect = EditorGUI.IndentedRect(rect.Pop()),
                buttonRect;

            if (!property.IsInArray())
            {
                var rects = AutoRect.DivideWithRatio(propertyRect, .2f, .8f);
                EditorGUI.LabelField(rects[0], label);
                buttonRect = rects[1];
            }
            else
            {
                if (property.GetParent().CountInProperty() > 1)
                {
                    var rects = AutoRect.DivideWithRatio(propertyRect, .2f, .8f);
                    EditorGUI.LabelField(rects[0], label);
                    buttonRect = rects[1];
                }
                else
                {
                    buttonRect = propertyRect;
                }
            }

            #endregion

            var indexProp = GetIndexProperty(property);
            ShaderReference currentValue = new ShaderReference(indexProp.intValue);
            string displayName;
            if (!currentValue.IsEmpty())
            {
                displayName = currentValue.Shader.name;
            }
            else displayName = "None";

            bool clicked = CoreGUI.BoxButton(buttonRect, displayName, ColorPalettes.PastelDreams.Mint, () =>
            {

            });

            if (clicked)
            {
                DrawSelectionWindow((shaderRef) =>
                {
                    indexProp.intValue = shaderRef.Index;
                    indexProp.serializedObject.ApplyModifiedProperties();

                    m_Changed = true;
                });
            }
        }

        static void DrawSelectionWindow(Action<ShaderReference> setter)
        {
            Rect rect = GUILayoutUtility.GetRect(150, 300);
            rect.position = Event.current.mousePosition;

            //var sort = Syadeu.Presentation.Render.RenderSettings.Instance.m_Shaders
            //    .Where(t => t.BaseType.GenericTypeArguments[0].Equals(targetType)).ToArray();

            var renderSettings = Syadeu.Presentation.Render.RenderSettings.Instance;

            PopupWindow.Show(rect, SelectorPopup<ShaderReference, Shader>.GetWindow(
                list: renderSettings.m_Shaders,
                setter: setter,
                getter: (t) =>
                {
                    int index = Array.IndexOf(renderSettings.m_Shaders, t);

                    return new ShaderReference(index);
                },
                noneValue: ShaderReference.Empty,
                other => other.name
                ));
        }
    }
}
