using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Collections.Converters;
using Syadeu.Presentation.Actions;
using SyadeuEditor.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    public sealed class ConstActionReferenceDrawer : ObjectDrawer<IConstActionReference>
    {
        private bool m_Open = false;

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
            DescriptionAttribute description = null;
            if (currentValue != null && !currentValue.IsEmpty())
            {
                var iter = ConstActionUtilities.Types.Where(t => t.GUID.Equals(currentValue.Guid));
                if (iter.Any())
                {
                    currentActionType = iter.First();
                    targetName = TypeHelper.ToString(currentActionType);
                    description = currentActionType.GetCustomAttribute<DescriptionAttribute>();
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
                    if (!string.IsNullOrEmpty(Name))
                    {
                        GUILayout.Label(Name, GUILayout.Width(Screen.width * .25f));
                    }

                    clicked = CoreGUI.BoxButton(targetName, ColorPalettes.PastelDreams.Mint);

                    using (new EditorGUI.DisabledGroupScope(currentActionType == null ||
                            !ConstActionUtilities.HashMap.TryGetValue(currentActionType, out var info) ||
                            (info.ArgumentFields.Length < 1 && description == null)))
                    {
                        m_Open = CoreGUI.BoxToggleButton(
                            m_Open,
                            ColorPalettes.PastelDreams.TiffanyBlue,
                            ColorPalettes.PastelDreams.HotPink,
                            GUILayout.Width(20)
                            );
                    }
                }

                if (m_Open)
                {
                    if (description != null)
                    {
                        EditorGUILayout.HelpBox(description.Description, MessageType.Info);
                    }

                    var info = ConstActionUtilities.HashMap[currentActionType];
                    if (info.ArgumentFields.Length > 0)
                    {
                        if (currentValue.Arguments.Length != info.ArgumentFields.Length)
                        {
                            currentValue.SetArguments(new object[info.ArgumentFields.Length]);
                            for (int i = 0; i < currentValue.Arguments.Length; i++)
                            {
                                currentValue.Arguments[i] = TypeHelper.GetDefaultValue(info.ArgumentFields[i].FieldType);
                            }
                        }

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
                        }
                    }
                }
            }
            
            if (clicked)
            {
                DrawSelectionWindow(GetFieldAttribute<ConstActionOptionsAttribute>(), (t) =>
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

        static void DrawSelectionWindow(ConstActionOptionsAttribute option, Action<Type> setter, Type targetType)
        {
            Rect rect = GUILayoutUtility.GetRect(150, 300);
            rect.position = Event.current.mousePosition;

            Type[] sort;
            if (targetType != null)
            {
                var temp = ConstActionUtilities.Types
                    .Where(t => t.BaseType.GenericTypeArguments[0].Equals(targetType));

                temp = SortOptions(temp, option);

                sort = temp.ToArray();
            }
            else
            {
                var temp = SortOptions(ConstActionUtilities.Types, option);
                sort = temp.ToArray();
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
        static IEnumerable<Type> SortOptions(IEnumerable<Type> arr, ConstActionOptionsAttribute option)
        {
            if (option == null)
            {
                return arr;
            }

            if (option.TriggerActionOnly)
            {
                arr = arr.Where(t => TypeHelper.TypeOf<IConstTriggerAction>.Type.IsAssignableFrom(t));
            }

            return arr;
        }
    }
}
