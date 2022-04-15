using Syadeu.Collections;
using Syadeu.Internal;
using Syadeu.Presentation;
using Syadeu.Presentation.Attributes;
using SyadeuEditor.Utilities;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    [CustomPropertyDrawer(typeof(IFixedReference), true)]
    public sealed class ReferencePropertyDrawer : PropertyDrawer<IFixedReference>
    {
        private static string
            s_InvalidMsg = ("This reference is invalid."),
            s_ValidMsg = ("This is shared reference. Anything made changes in this inspector view will affect to original reference directly.");
        private const float c_MsgHeight = 40;

        private SerializedProperty m_HashProperty;
        private Type m_TargetType;
        private string m_DisplayName;
        private Reference m_Current;

        private static SerializedProperty GetHashProperty(SerializedProperty property)
        {
            return property.FindPropertyRelative("m_Hash");
        }
        private static Reference GetReference(SerializedProperty hashProperty) 
            => new Reference(SerializedPropertyHelper.ReadHash(hashProperty));

        protected override float PropertyHeight(SerializedProperty property, GUIContent label)
        {
            //return base.GetPropertyHeight(property, label) + EditorGUIUtility.standardVerticalSpacing;
            if (!property.isExpanded)
            {
                return CoreGUI.GetLineHeight(1);
            }

            float height = CoreGUI.GetLineHeight(1);
            height += c_MsgHeight;

            SerializedProperty hashProp = GetHashProperty(property);
            Reference refProp = GetReference(hashProp);
            if (!refProp.IsEmpty() && refProp.IsValid())
            {
                height += SerializedObject<ObjectBase>.GetPropertyHeight(refProp.GetObject());
            }

            return height;
        }

        protected override void BeforePropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            m_HashProperty = GetHashProperty(property);
            Type t = fieldInfo.FieldType;

            if (!t.IsArray && !TypeHelper.TypeOf<IList>.Type.IsAssignableFrom(t))
            {
                Type[] generics = t.GetGenericArguments();
                if (generics.Length > 0) m_TargetType = generics[0];
                else m_TargetType = null;
            }
            else
            {
                Type[] generics;
                if (t.IsArray)
                {
                    generics = t.GetElementType().GetGenericArguments();
                }
                else
                {
                    generics = t.GetGenericArguments()[0].GetGenericArguments();
                }

                if (generics.Length > 0) m_TargetType = generics[0];
                else m_TargetType = null;
            }

            m_Current = GetReference(m_HashProperty);
            if (m_Current.GetObject() == null) m_DisplayName = "None";
            else m_DisplayName = m_Current.GetObject().Name;
        }

        protected override void OnPropertyGUI(ref AutoRect temp, SerializedProperty property, GUIContent label)
        {
            Rect buttonRect, expandRect, cloneBttRect;
            if (!property.IsInArray())
            {
                Rect elementRect = temp.Pop();
                var rects = AutoRect.DivideWithRatio(elementRect, .2f, .8f);
                EditorGUI.LabelField(rects[0], label);
                buttonRect = rects[1];

                Rect[] tempRects = AutoRect.DivideWithFixedWidthRight(rects[1], 20, 20);
                expandRect = tempRects[0];
                cloneBttRect = tempRects[1];
                AutoRect.AlignRect(ref buttonRect, expandRect);
            }
            else
            {
                if (property.GetParent().CountInProperty() > 1)
                {
                    Rect elementRect = temp.Pop();
                    var rects = AutoRect.DivideWithRatio(elementRect, .2f, .8f);
                    EditorGUI.LabelField(rects[0], label);
                    buttonRect = rects[1];

                    Rect[] tempRects = AutoRect.DivideWithFixedWidthRight(rects[1], 20, 20);
                    expandRect = tempRects[0];
                    cloneBttRect = tempRects[1];
                    AutoRect.AlignRect(ref buttonRect, expandRect);
                }
                else
                {
                    Rect elementRect = temp.Pop();
                    //var rects = AutoRect.DivideWithRatio(elementRect, .8f, .2f);
                    buttonRect = elementRect;

                    Rect[] tempRects = AutoRect.DivideWithFixedWidthRight(elementRect, 20, 20);
                    expandRect = tempRects[0];
                    cloneBttRect = tempRects[1];
                    AutoRect.AlignRect(ref buttonRect, expandRect);
                }
            }

            IFixedReference current = m_Current;
            Type targetType = m_TargetType;
            
            bool clicked = CoreGUI.BoxButton(buttonRect, m_DisplayName, ColorPalettes.PastelDreams.Mint, () =>
            {
                GenericMenu menu = new GenericMenu();
                menu.AddDisabledItem(new GUIContent(m_DisplayName));
                menu.AddSeparator(string.Empty);

                if (m_Current.IsValid())
                {
                    menu.AddItem(new GUIContent("Find Referencers"), false, () =>
                    {
                        //if (!EntityWindow.IsOpened) CoreSystemMenuItems.EntityDataListMenu();
                        
                        EntityWindow.Instance.GetMenuItem<EntityDataWindow>().SearchString = $"ref:{current.Hash}";
                    });
                    menu.AddItem(new GUIContent("To Reference"), false, () =>
                    {
                        EntityWindow.Instance.GetMenuItem<EntityDataWindow>().Select(current);
                    });
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("Find Referencers"));
                    menu.AddDisabledItem(new GUIContent("To Reference"));
                }

                menu.AddSeparator(string.Empty);
                if (targetType != null && !targetType.IsAbstract)
                {
                    menu.AddItem(new GUIContent($"Create New {TypeHelper.ToString(targetType)}"), false, () =>
                    {
                        var obj = EntityWindow.Instance.Add(targetType);
                        //setter.Invoke(obj.Hash);
                        SerializedPropertyHelper.SetHash(GetHashProperty(property), obj.Hash);
                        property.serializedObject.ApplyModifiedProperties();
                    });
                }
                else menu.AddDisabledItem(new GUIContent($"Create New {m_DisplayName}"));

                menu.ShowAsContext();
            });

            bool disable = current.IsEmpty() || !current.IsValid();
            using (new EditorGUI.DisabledGroupScope(disable))
            {
                if (disable)
                {
                    property.isExpanded = false;
                }

                string str = property.isExpanded ? EditorStyleUtilities.FoldoutOpendString : EditorStyleUtilities.FoldoutClosedString;
                property.isExpanded = CoreGUI.BoxToggleButton(
                    expandRect,
                    property.isExpanded,
                    new GUIContent(str),
                    ColorPalettes.PastelDreams.TiffanyBlue,
                    ColorPalettes.PastelDreams.HotPink
                    );
                if (CoreGUI.BoxButton(cloneBttRect, "C", ColorPalettes.WaterFoam.Spearmint, null))
                {
                    ObjectBase clone = (ObjectBase)current.GetObject().Clone();

                    clone.Hash = Hash.NewHash();
                    clone.Name += "_Clone";

                    if (EntityWindow.IsOpened)
                    {
                        EntityWindow.Instance.Add(clone);
                    }
                    else
                    {
                        EntityDataList.Instance.m_Objects.Add(clone.Hash, clone);
                        EntityDataList.Instance.SaveData(clone);
                    }

                    current = (IFixedReference)TypeHelper.GetConstructorInfo(fieldInfo.FieldType, TypeHelper.TypeOf<ObjectBase>.Type).Invoke(
                        new object[] { clone });
                    SerializedPropertyHelper.SetHash(GetHashProperty(property), clone.Hash);
                }

                if (property.isExpanded)
                {
                    if (!current.IsValid())
                    {
                        Rect expandTotalRect = temp.Pop(c_MsgHeight);

                        CoreGUI.DrawRect(EditorGUI.IndentedRect(expandTotalRect), Color.red);
                        EditorGUI.HelpBox(EditorGUI.IndentedRect(expandTotalRect), s_InvalidMsg, MessageType.Error);
                    }
                    else
                    {
                        Rect 
                            msgRect = temp.Pop(c_MsgHeight),
                            expandTotalRect = msgRect;
                        var targetSerialObj = SerializedObject<ObjectBase>.GetSharedObject(current.GetObject());
                        float
                            elementHeight = targetSerialObj.PropertyHeight,
                            totalHeight = c_MsgHeight + elementHeight;
                        expandTotalRect.height = totalHeight;
                        CoreGUI.DrawRect(EditorGUI.IndentedRect(expandTotalRect), Color.white);

                        EditorGUI.HelpBox(EditorGUI.IndentedRect(msgRect), s_ValidMsg, MessageType.Info);
                        EditorGUI.PropertyField(temp.Pop(elementHeight), targetSerialObj);
                    }
                }
            }

            if (clicked)
            {
                Vector2 pos = Event.current.mousePosition;
                pos = GUIUtility.GUIToScreenPoint(pos);

                ObjectBase[] objectBases = Array.Empty<ObjectBase>();

                EntityAcceptOnlyAttribute entityAcceptOnly = GetEntityAcceptOnly(property, out Type entityType);
                if (entityAcceptOnly == null || (
                    entityAcceptOnly.AttributeTypes == null ||
                    entityAcceptOnly.AttributeTypes.Length == 0))
                {
                    SearchWindow.Open(new SearchWindowContext(pos),
                        new ReferenceSearchProvider(targetType,
                        onClick: (t) =>
                        {
                            SerializedPropertyHelper.SetHash(GetHashProperty(property), t);
                            property.serializedObject.ApplyModifiedProperties();
                        },
                        predicate: (t) =>
                        {
                            return true;
                        }
                        ));
                }
                else
                {
                    SearchWindow.Open(new SearchWindowContext(pos),
                        new ReferenceSearchProvider(targetType,
                        onClick: (t) =>
                        {
                            SerializedPropertyHelper.SetHash(GetHashProperty(property), t);
                            property.serializedObject.ApplyModifiedProperties();
                        },
                        predicate: (t) =>
                        {
                            Type attType = t.GetType();

                            if (!IsEntityAccepts(entityAcceptOnly, attType)) return false;

                            AttributeAcceptOnlyAttribute requireEntity = attType.GetCustomAttribute<AttributeAcceptOnlyAttribute>();
                            if (requireEntity == null) return true;
                            else if (!IsAttributeAccepts(requireEntity, entityType)) return false;

                            return true;
                        }
                        ));
                }
            }
        }

        static bool IsEntityAccepts(EntityAcceptOnlyAttribute entityAcceptOnly, Type attributeType)
        {
            for (int i = 0; i < entityAcceptOnly.AttributeTypes.Length; i++)
            {
                if (TypeHelper.InheritsFrom(attributeType, entityAcceptOnly.AttributeTypes[i]))
                {
                    return true;
                }
            }
            return false;
        }
        static bool IsAttributeAccepts(AttributeAcceptOnlyAttribute attributeAcceptOnly, Type entityType)
        {
            if (attributeAcceptOnly.Types == null || attributeAcceptOnly.Types.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < attributeAcceptOnly.Types.Length; i++)
            {
                if (TypeHelper.InheritsFrom(entityType, attributeAcceptOnly.Types[i]))
                {
                    return true;
                }
            }
            return false;
        }
        static EntityAcceptOnlyAttribute GetEntityAcceptOnly(SerializedProperty property, out Type entityType)
        {
            if (!property.IsInArray())
            {
                entityType = null;
                return null;
            }

            SerializedProperty array = property.GetParentArrayOfProperty(out _).GetParent();
            Type arrayType = array.GetSystemType();
            if (!arrayType.Equals(TypeHelper.TypeOf<AttributeArray>.Type))
            {
                entityType = null;
                //Debug.Log($"{arrayType.Name} not attarry at {array.GetParent().GetSystemType().Name}");
                return null;
            }

            // entity type
            entityType = array.GetParent().GetSystemType();
            if (!TypeHelper.InheritsFrom<ObjectBase>(entityType))
            {
                //Debug.Log($"{entityType.FullName} not objbase, arr:{array.GetSystemType().Name}");
                return null;
            }

            return entityType.GetCustomAttribute<EntityAcceptOnlyAttribute>();
        }

        static void DrawSelectionWindow(Action<Hash> setter, Type targetType)
        {
            Rect rect = GUILayoutUtility.GetRect(150, 300);
            rect.position = Event.current.mousePosition;

            if (targetType == null)
            {
                //GUIUtility.ExitGUI();

                PopupWindow.Show(rect, SelectorPopup<Hash, ObjectBase>.GetWindow(
                    list: EntityDataList.Instance.m_Objects.Values.ToArray(),
                    setter: setter,
                    getter: (att) =>
                    {
                        return att.Hash;
                    },
                    noneValue: Hash.Empty,
                    (other) => other.Name
                    ));
            }
            else
            {
                ObjectBase[] actionBases = EntityDataList.Instance.GetData<ObjectBase>()
                    .Where((other) => other.GetType().Equals(targetType) ||
                            targetType.IsAssignableFrom(other.GetType()))
                    .ToArray();

                //GUIUtility.ExitGUI();

                PopupWindow.Show(rect, SelectorPopup<Hash, ObjectBase>.GetWindow(
                    list: actionBases,
                    setter: setter,
                    getter: (att) =>
                    {
                        return att.Hash;
                    },
                    noneValue: Hash.Empty,
                    (other) => other.Name
                    ));
            }

            if (Event.current.type == EventType.Repaint) rect = GUILayoutUtility.GetLastRect();
        }
    }
}
