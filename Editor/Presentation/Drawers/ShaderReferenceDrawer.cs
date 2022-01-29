using Syadeu.Collections;
using Syadeu.Presentation.Render;
using SyadeuEditor.Utilities;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    public sealed class ShaderReferenceDrawer : ObjectDrawer<ShaderReference>
    {
        public ShaderReferenceDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }
        public ShaderReferenceDrawer(IList list, int index, Type elementType) : base(list, index, elementType)
        {
        }
        public ShaderReferenceDrawer(object parentObject, Type declaredType, Action<ShaderReference> setter, Func<ShaderReference> getter) : base(parentObject, declaredType, setter, getter)
        {
        }

        public override ShaderReference Draw(ShaderReference currentValue)
        {
            string targetName;
            if (!currentValue.IsEmpty())
            {
                targetName = currentValue.Shader.name;
            }
            else targetName = "None";

            bool clicked;
            using (new EditorGUILayout.VerticalScope())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(Name, GUILayout.Width(Screen.width * .25f));

                    clicked = EditorUtilities.BoxButton(targetName, ColorPalettes.PastelDreams.Mint, () =>
                    {
                    });
                }
            }

            if (clicked)
            {
                DrawSelectionWindow(Setter);
            }

            return currentValue;
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
