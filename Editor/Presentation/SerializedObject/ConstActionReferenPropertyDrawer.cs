using UnityEditor;
using SyadeuEditor.Utilities;
using Syadeu.Collections;
using UnityEngine;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace SyadeuEditor.Presentation
{
    [CustomPropertyDrawer(typeof(ConstActionReference<>), true)]
    internal sealed class ConstActionReferenPropertyDrawer : PropertyDrawer<IConstActionReference>
    {
        private Type m_TargetType;

        protected override void OnInitialize(SerializedProperty property)
        {
            Type[] generics = fieldInfo.FieldType.GetGenericArguments();
            if (generics.Length > 0) m_TargetType = generics[0];
            else m_TargetType = null;


        }
        protected override void BeforePropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            base.BeforePropertyGUI(ref rect, property, label);
        }
        protected override void OnPropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            //base.OnPropertyGUI(ref rect, property, label);

            IConstActionReference currentValue = (IConstActionReference)property.GetTargetObject();

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
                else targetName = "None";
            }
            else targetName = "None";

            Rect elementRect = rect.Pop();
            var rects = AutoRect.DivideWithRatio(elementRect, .9f, .1f);

            bool clicked = CoreGUI.BoxButton(rects[0], targetName, ColorPalettes.PastelDreams.Mint, () =>
            {
            });

            using (new EditorGUI.DisabledGroupScope(currentActionType == null ||
                    !ConstActionUtilities.HashMap.TryGetValue(currentActionType, out var info) ||
                    (info.ArgumentFields.Length < 1 && description == null)))
            {
                string str = property.isExpanded ? EditorStyleUtilities.FoldoutOpendString : EditorStyleUtilities.FoldoutClosedString;
                property.isExpanded = CoreGUI.BoxToggleButton(
                    rects[1],
                    property.isExpanded,
                    new GUIContent(str),
                    ColorPalettes.PastelDreams.TiffanyBlue,
                    ColorPalettes.PastelDreams.HotPink
                    );

                if (property.isExpanded)
                {
                    if (description != null)
                    {
                        EditorGUILayout.HelpBox(description.Description, MessageType.Info);
                    }

                    var infomation = ConstActionUtilities.HashMap[currentActionType];
                    if (infomation.ArgumentFields.Length > 0)
                    {
                        if (currentValue.Arguments.Length != infomation.ArgumentFields.Length)
                        {
                            currentValue.SetArguments(new object[infomation.ArgumentFields.Length]);
                            for (int i = 0; i < currentValue.Arguments.Length; i++)
                            {
                                currentValue.Arguments[i] = TypeHelper.GetDefaultValue(infomation.ArgumentFields[i].FieldType);
                            }
                        }

                        //using (new EditorUtilities.BoxBlock(Color.black))
                        //{
                        //    EditorUtilities.StringRich("Arguments", 13);
                        //    EditorGUI.indentLevel++;

                        //    for (int i = 0; i < infomation.ArgumentFields.Length; i++)
                        //    {
                        //        currentValue.Arguments[i] =
                        //            EditorUtilities.AutoField(
                        //                infomation.ArgumentFields[i],
                        //                string.IsNullOrEmpty(infomation.JsonAttributes[i].PropertyName) ? infomation.ArgumentFields[i].Name : infomation.JsonAttributes[i].PropertyName,
                        //                currentValue.Arguments[i]);
                        //    }

                        //    EditorGUI.indentLevel--;
                        //}
                    }
                }
            }

            if (clicked)
            {
                DrawSelectionWindow(fieldInfo.GetCustomAttribute<ConstActionOptionsAttribute>(), (t) =>
                {
                    //var ctor = TypeHelper.GetConstructorInfo(
                    //    fieldInfo.FieldType, TypeHelper.TypeOf<Guid>.Type, TypeHelper.TypeOf<IEnumerable<object>>.Type);

                    //object temp;
                    if (t == null)
                    {
                        //temp = ctor.Invoke(new object[] { Guid.Empty, null });
                        SerializedPropertyHelper.SetConstActionReference(property, Guid.Empty);
                    }
                    else
                    {
                        //temp = ctor.Invoke(new object[] { t.GUID, null });
                        SerializedPropertyHelper.SetConstActionReference(property, t.GUID);
                    }

                    //fieldInfo.SetValue(property.GetParent().GetTargetObject(), temp);
                    //Setter.Invoke((IConstActionReference)temp);

                }, m_TargetType);
            }
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
