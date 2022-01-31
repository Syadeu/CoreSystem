using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Collections.Converters;
using Syadeu.Presentation.Actions;
using SyadeuEditor.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    public sealed class ConstActionReferenceDrawer : ObjectDrawer<IConstActionReference>
    {
        public ConstActionReferenceDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }
        public ConstActionReferenceDrawer(IList list, int index, Type elementType) : base(list, index, elementType)
        {
        }
        public ConstActionReferenceDrawer(object parentObject, Type declaredType, Action<IConstActionReference> setter, Func<IConstActionReference> getter) : base(parentObject, declaredType, setter, getter)
        {
        }

        public override IConstActionReference Draw(IConstActionReference currentValue)
        {
            Type targetType;
            Type[] generics = DeclaredType.GetGenericArguments();
            if (generics.Length > 0) targetType = generics[0];
            else targetType = null;

            string targetName;
            Type currentActionType = null;
            if (currentValue != null && !currentValue.IsEmpty())
            {
                var iter = ConstActionUtilities.Types.Where(t => t.GUID.Equals(currentValue.Guid));
                if (iter.Any())
                {
                    currentActionType = iter.First();
                    targetName = TypeHelper.ToString(currentActionType);
                }
                else
                {
                    targetName = "None";
                }
            }
            else
            {
                targetName = "None";
            }

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

                if (currentActionType != null &&
                    ConstActionUtilities.HashMap.TryGetValue(currentActionType, out var info) &&
                    info.ArgumentFields.Length > 0)
                {
                    if (currentValue.Arguments.Length != info.ArgumentFields.Length)
                    {
                        currentValue.SetArguments(new object[info.ArgumentFields.Length]);
                        for (int i = 0; i < currentValue.Arguments.Length; i++)
                        {
                            currentValue.Arguments[i] = TypeHelper.GetDefaultValue(info.ArgumentFields[i].FieldType);
                        }
                    }


                    EditorGUI.indentLevel += 2;
                    using (new EditorUtilities.BoxBlock(Color.black))
                    {
                        EditorUtilities.StringRich("Arguments", 13);
                        EditorGUI.indentLevel++;

                        for (int i = 0; i < info.ArgumentFields.Length; i++)
                        {
                            currentValue.Arguments[i] =
                                EditorUtilities.AutoField(
                                    info.ArgumentFields[i],
                                    string.IsNullOrEmpty(info.JsonAttributes[i].PropertyName) ? info.ArgumentFields[i].Name : info.JsonAttributes[i].PropertyName,
                                    currentValue.Arguments[i]);
                        }

                        EditorGUI.indentLevel--;
                        EditorUtilities.Line();
                    }

                    EditorGUI.indentLevel -= 2;
                }
            }

            if (clicked)
            {
                DrawSelectionWindow((t) =>
                {
                    var ctor = TypeHelper.GetConstructorInfo(
                        DeclaredType, TypeHelper.TypeOf<Guid>.Type, TypeHelper.TypeOf<IEnumerable<object>>.Type);

                    object temp;
                    if (t == null)
                    {
                        temp = ctor.Invoke(new object[] { Guid.Empty, null });
                    }
                    else
                    {
                        temp = ctor.Invoke(new object[] { t.GUID, null });
                    }

                    Setter.Invoke((IConstActionReference)temp);

                }, targetType);
            }

            return currentValue;
        }

        static void DrawSelectionWindow(Action<Type> setter, Type targetType)
        {
            Rect rect = GUILayoutUtility.GetRect(150, 300);
            rect.position = Event.current.mousePosition;

            Type[] sort;
            if (targetType != null)
            {
                sort = ConstActionUtilities.Types
                    .Where(t => t.BaseType.GenericTypeArguments[0].Equals(targetType)).ToArray();
            }
            else
            {
                sort = ConstActionUtilities.Types;
            }

            PopupWindow.Show(rect, SelectorPopup<Type, Type>.GetWindow(
                list: sort,
                setter: setter,
                getter: (t) =>
                {
                    return t;
                },
                noneValue: null,
                other => TypeHelper.ToString(other)
                ));
        }
    }
}
