using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Syadeu.Collections;
using Syadeu.Collections.Lua;
using Syadeu.Internal;
using UnityEditor;
using UnityEngine;

using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine.AddressableAssets;
using Syadeu.Mono;
using Syadeu.Presentation;
using Unity.Mathematics;
using Syadeu;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Actions;
using SyadeuEditor.Presentation;
using SyadeuEditor.Utilities;

namespace SyadeuEditor
{
    public sealed class ReflectionHelperEditor
    {
        public static void DrawAttributeSelector(string name, Action<Hash> setter, Hash current, Type entityType)
        {
            GUIContent displayName;
            EntityDataList.Instance.m_Objects.TryGetValue(current, out var attVal);
            //AttributeBase att = (AttributeBase)EntityDataList.Instance.GetObject(current);
            AttributeBase att = attVal == null ? null : (AttributeBase)attVal;
            if (current.Equals(Hash.Empty)) displayName = new GUIContent("None");
            else if (att == null) displayName = new GUIContent("Attribute Not Found");
            else displayName = new GUIContent(att.Name);

            GUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 15);

            if (!string.IsNullOrEmpty(name))
            {
                GUILayout.Label(name);
            }

            Rect fieldRect = GUILayoutUtility.GetRect(displayName, EditorStyleUtilities.SelectorStyle, GUILayout.ExpandWidth(true));
            int selectorID = GUIUtility.GetControlID(FocusType.Passive, fieldRect);

            switch (Event.current.GetTypeForControl(selectorID))
            {
                case EventType.Repaint:
                    bool isHover = fieldRect.Contains(Event.current.mousePosition);

                    EditorStyleUtilities.SelectorStyle.Draw(fieldRect, displayName, isHover, isActive: true, on: true, false);
                    break;
                case EventType.ContextClick:
                    if (!fieldRect.Contains(Event.current.mousePosition)) break;

                    Event.current.Use();

                    GenericMenu menu = new GenericMenu();
                    menu.AddDisabledItem(displayName);
                    menu.AddSeparator(string.Empty);

                    if (att != null)
                    {
                        menu.AddItem(new GUIContent("Find Referencers"), false, () =>
                        {
                            if (!EntityWindow.IsOpened) CoreSystemMenuItems.EntityDataListMenu();
                            EntityWindow.Instance.m_DataListWindow.SearchString = $"ref:{att.Hash}";
                        });
                        menu.AddItem(new GUIContent("To Reference"), false, () =>
                        {
                            EntityWindow.Instance.m_DataListWindow.Select(new FixedReference(att.Hash));
                        });
                    }
                    else
                    {
                        menu.AddDisabledItem(new GUIContent("Find Referencers"));
                        menu.AddDisabledItem(new GUIContent("To Reference"));
                    }

                    menu.ShowAsContext();
                    break;
                case EventType.MouseDown:
                    if (!fieldRect.Contains(Event.current.mousePosition) ||
                        Event.current.button != 0) break;

                    EntityAcceptOnlyAttribute acceptOnly = entityType.GetCustomAttribute<EntityAcceptOnlyAttribute>();
                    if (acceptOnly != null && (
                        acceptOnly.AttributeTypes == null ||
                        acceptOnly.AttributeTypes.Length == 0))
                    {
                        throw new Exception($"entity({entityType.Name}) has null attribute accepts");
                    }

                    Rect tempRect = GUILayoutUtility.GetLastRect();
                    tempRect.position = Event.current.mousePosition;

                    var atts = EntityDataList.Instance.GetData<AttributeBase>()
                        .Where((other) =>
                        {
                            Type attType = other.GetType();
                            bool attCheck = false;
                            if (acceptOnly != null)
                            {
                                for (int i = 0; i < acceptOnly.AttributeTypes.Length; i++)
                                {
                                    if (acceptOnly.AttributeTypes[i].IsAssignableFrom(attType))
                                    {
                                        attCheck = true;
                                        break;
                                    }
                                }
                            }
                            else attCheck = true;

                            if (!attCheck) return false;
                            attCheck = false;

                            AttributeAcceptOnlyAttribute requireEntity = attType.GetCustomAttribute<AttributeAcceptOnlyAttribute>();
                            if (requireEntity == null) return true;

                            if (requireEntity.Types == null || requireEntity.Types.Length == 0)
                            {
                                return false;
                            }
                            else
                            {
                                for (int i = 0; i < requireEntity.Types.Length; i++)
                                {
                                    if (requireEntity.Types[i].IsAssignableFrom(entityType))
                                    {
                                        attCheck = true;
                                        break;
                                    }
                                }
                            }

                            if (!attCheck) return false;
                            return true;
                        })
                        .ToArray();

                    Event.current.Use();

                    PopupWindow.Show(tempRect,
                        SelectorPopup<Hash, AttributeBase>.GetWindow(atts, setter, (att) =>
                        {
                            return att.Hash;
                        }, Hash.Empty)
                        );
                    
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == selectorID)
                    {
                        Event.current.Use();
                        GUIUtility.hotControl = 0;
                    }
                    break;
                default:
                    break;
            }

            //if (GUILayout.Button(displayName, SelectorStyle, GUILayout.ExpandWidth(true)))
            //{
            //    EntityAcceptOnlyAttribute acceptOnly = entityType.GetCustomAttribute<EntityAcceptOnlyAttribute>();
            //    if (acceptOnly != null && (
            //        acceptOnly.AttributeTypes == null || 
            //        acceptOnly.AttributeTypes.Length == 0))
            //    {
            //        throw new Exception($"entity({entityType.Name}) has null attribute accepts");
            //    }

                
            //}

            GUILayout.EndHorizontal();
        }
        public static void DrawReferenceSelector(string name, Action<Hash> setter, IFixedReference current, Type targetType)
        {
            string displayName;
            if (current == null || current.Hash.Equals(Hash.Empty)) displayName = "None";
            else
            {
                ObjectBase objBase = EntityDataList.Instance.GetObject(current.Hash);
                if (objBase == null) displayName = "None";
                else displayName = objBase.Name;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 15);
            
            if (!string.IsNullOrEmpty(name))
            {
                GUILayout.Label(name, GUILayout.Width(Screen.width * .25f));
            }
            
            if (GUILayout.Button(displayName, EditorStyleUtilities.SelectorStyle, GUILayout.ExpandWidth(true)))
            {
                Rect rect = GUILayoutUtility.GetRect(150, 300);
                rect.position = Event.current.mousePosition;

                if (targetType == null)
                {
                    try
                    {
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
                    catch (ExitGUIException)
                    {
                    }
                }
                else
                {
                    ObjectBase[] actionBases = EntityDataList.Instance.GetData<ObjectBase>()
                        .Where((other) => other.GetType().Equals(targetType) ||
                                targetType.IsAssignableFrom(other.GetType()))
                        .ToArray();

                    try
                    {
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
                    catch (ExitGUIException)
                    {
                    }
                }
            }

            GUILayout.EndHorizontal();
        }
    }
}
