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
    [CustomPropertyDrawer(typeof(IConstActionReference), true)]
    internal sealed class ConstActionReferenPropertyDrawer : PropertyDrawer<IConstActionReference>
    {
        private Type m_TargetType;

        private SerializedProperty GetGuidField(SerializedProperty property)
        {
            return property.FindPropertyRelative("m_Guid");
        }
        private SerializedProperty GetArgumentsField(SerializedProperty property)
        {
            return property.FindPropertyRelative("m_Arguments");
        }

        protected override void OnInitialize(SerializedProperty property)
        {
            Type[] generics = fieldInfo.FieldType.GetGenericArguments();
            if (generics.Length > 0) m_TargetType = generics[0];
            else m_TargetType = null;


        }
        protected override float PropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = PropertyDrawerHelper.GetPropertyHeight(1) + EditorGUIUtility.standardVerticalSpacing;
            SerializedProperty guidField = GetGuidField(property);

            if (guidField.isExpanded)
            {
                height += 17f;

                var argsField = GetArgumentsField(property);
                height += PropertyDrawerHelper.GetPropertyHeight(argsField.arraySize);
            }

            return height;
        }

        protected override void OnPropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
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

            Rect 
                elementRect = rect.Pop(),
                middleRect, expandRect;

            if (!property.IsInArray())
            {
                var rects = AutoRect.DivideWithRatio(elementRect, .2f, .75f, .05f);
                EditorGUI.LabelField(rects[0], label);
                middleRect = rects[1];
                expandRect = rects[2];
            }
            else
            {
                if (property.GetParent().CountInProperty() > 1)
                {
                    var rects = AutoRect.DivideWithRatio(elementRect, .2f, .75f, .05f);
                    //EditorStyles.label.CalcMinMaxWidth()
                    EditorGUI.LabelField(rects[0], label);
                    middleRect = rects[1];
                    expandRect = rects[2];
                }
                else
                {
                    var rects = AutoRect.DivideWithRatio(elementRect, .95f, .05f);
                    middleRect = rects[0];
                    expandRect = rects[1];
                }
            }

            SerializedProperty guidField = GetGuidField(property);
            bool clicked = CoreGUI.BoxButton(middleRect, targetName, ColorPalettes.PastelDreams.Mint, () =>
            {
            });

            bool disabled = currentActionType == null ||
                    !ConstActionUtilities.HashMap.TryGetValue(currentActionType, out var info) ||
                    (info.ArgumentFields.Length < 1 && description == null);
            using (new EditorGUI.DisabledGroupScope(disabled))
            {
                if (disabled)
                {
                    guidField.isExpanded = false;
                }

                string str = guidField.isExpanded ? EditorStyleUtilities.FoldoutOpendString : EditorStyleUtilities.FoldoutClosedString;
                guidField.isExpanded = CoreGUI.BoxToggleButton(
                    expandRect,
                    guidField.isExpanded,
                    new GUIContent(str),
                    ColorPalettes.PastelDreams.TiffanyBlue,
                    ColorPalettes.PastelDreams.HotPink
                    );

                if (guidField.isExpanded)
                {
                    if (description != null)
                    {
                        //EditorGUI.HelpBox(description.Description, MessageType.Info);
                    }

                    var infomation = ConstActionUtilities.HashMap[currentActionType];
                    if (infomation.ArgumentFields.Length > 0)
                    {
                        var argsField = GetArgumentsField(property);

                        rect.Indent(10);
                        Rect startArgRect = rect.Pop(15.5f),
                            argBoxRect = startArgRect;
                        argBoxRect.height += PropertyDrawerHelper.GetPropertyHeight(argsField.arraySize);
                        PropertyDrawerHelper.DrawRect(AutoRect.Indent(argBoxRect, -10), Color.black);

                        CoreGUI.Label(startArgRect, new GUIContent("Arguments"), 13, TextAnchor.MiddleLeft);

                        for (int i = 0; i < argsField.arraySize; i++)
                        {
                            var argElement = argsField.GetArrayElementAtIndex(i);
                            argElement.managedReferenceValue =
                                CoreGUI.AutoField(
                                    AutoRect.Indent(rect.Pop(), 10),
                                    infomation.ArgumentFields[i].FieldType,
                                    string.IsNullOrEmpty(infomation.JsonAttributes[i].PropertyName) ? infomation.ArgumentFields[i].Name : infomation.JsonAttributes[i].PropertyName,
                                    currentValue.Arguments[i]);
                        }
                        rect.Indent(-10);
                    }
                }
            }

            if (clicked)
            {
                SerializedProperty cachedProperty = property.Copy();
                DrawSelectionWindow(fieldInfo.GetCustomAttribute<ConstActionOptionsAttribute>(), (t) =>
                {
                    if (t == null)
                    {
                        SerializedPropertyHelper.SetConstActionReference(cachedProperty, Guid.Empty);
                    }
                    else
                    {
                        var infomation = ConstActionUtilities.HashMap[t];
                        object[] param = new object[infomation.ArgumentFields.Length];
                        for (int i = 0; i < param.Length; i++)
                        {
                            param[i] = TypeHelper.GetDefaultValue(infomation.ArgumentFields[i].FieldType);
                        }

                        SerializedPropertyHelper.SetConstActionReference(cachedProperty, t.GUID, param);
                    }
                    cachedProperty.isExpanded = false;
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
