using Syadeu.Collections;
using Syadeu.Presentation;
using Syadeu.Presentation.Attributes;
using SyadeuEditor.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    public sealed class AttributeListDrawer : ObjectDrawer<Reference<AttributeBase>[]>
    {
        private List<ObjectDrawerBase> m_Drawers = new List<ObjectDrawerBase>();
        private List<bool> m_Open = new List<bool>();

        public AttributeListDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
            Reload();
        }
        private void Reload()
        {
            m_Drawers.Clear();
            m_Open.Clear();

            Reference<AttributeBase>[] arr = Getter.Invoke();

            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i].IsValid()) m_Drawers.Add(ObjectBaseDrawer.GetDrawer(arr[i]));
                else m_Drawers.Add(null);

                m_Open.Add(false);
            }
        }
        public override Reference<AttributeBase>[] Draw(Reference<AttributeBase>[] currentValue)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorUtilities.StringRich(Name, 15);
                if (GUILayout.Button("+", GUILayout.Width(20)))
                {
                    Reference<AttributeBase>[] copy = new Reference<AttributeBase>[currentValue.Length + 1];
                    if (currentValue.Length > 0)
                    {
                        Array.Copy(currentValue, copy, currentValue.Length);
                    }

                    currentValue = copy;

                    m_Drawers.Add(null);
                    m_Open.Add(false);
                }
                if (currentValue.Length > 0 && GUILayout.Button("-", GUILayout.Width(20)))
                {
                    Reference<AttributeBase>[] copy = new Reference<AttributeBase>[currentValue.Length - 1];
                    if (currentValue.Length > 0)
                    {
                        Array.Copy(currentValue, copy, copy.Length);
                    }

                    currentValue = copy;
                    m_Drawers.RemoveAt(m_Drawers.Count - 1);
                    m_Open.RemoveAt(m_Open.Count - 1);
                }
            }

            int contextTarget = -1;
            bool showSelectContext = false;
            using (new EditorUtilities.BoxBlock(Color.black))
            {
                for (int i = 0; i < currentValue.Length; i++)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        int idx = i;
                        using (var change = new EditorGUI.ChangeCheckScope())
                        {
                            idx = EditorGUILayout.DelayedIntField(idx, GUILayout.Width(40));

                            if (change.changed)
                            {
                                if (idx >= currentValue.Length) idx = currentValue.Length - 1;

                                Reference<AttributeBase> cache = currentValue[i];
                                bool cacheOpen = m_Open[i];
                                var cacheDrawer = m_Drawers[i];

                                var temp = currentValue.ToList();
                                temp.RemoveAt(i);
                                m_Open.RemoveAt(i);
                                m_Drawers.RemoveAt(i);

                                temp.Insert(idx, cache);
                                m_Open.Insert(idx, cacheOpen);
                                m_Drawers.Insert(idx, cacheDrawer);

                                currentValue = temp.ToArray();
                                Setter.Invoke(currentValue);
                                break;
                            }
                        }

                        //idx = i;
                        if (DrawAttributeSelector(null, currentValue[idx], TargetObject.GetType()))
                        {
                            showSelectContext = true;
                            contextTarget = idx;

                            break;
                        }

                        if (GUILayout.Button("-", GUILayout.Width(20)))
                        {
                            if (currentValue.Length == 1)
                            {
                                currentValue = Array.Empty<Reference<AttributeBase>>();
                                m_Open.Clear();
                                m_Drawers.Clear();
                            }
                            else
                            {
                                var temp = currentValue.ToList();
                                temp.RemoveAt(i);
                                m_Open.RemoveAt(i);
                                m_Drawers.RemoveAt(i);
                                currentValue = temp.ToArray();
                                Setter.Invoke(currentValue);
                            }

                            i--;
                            continue;
                        }

                        m_Open[i] = GUILayout.Toggle(m_Open[i], m_Open[i] ? EditorStyleUtilities.FoldoutOpendString : EditorStyleUtilities.FoldoutClosedString, EditorStyleUtilities.MiniButton, GUILayout.Width(20));

                        if (GUILayout.Button("C", GUILayout.Width(20)))
                        {
                            AttributeBase cloneAtt = (AttributeBase)EntityDataList.Instance.GetObject(currentValue[i]).Clone();

                            cloneAtt.Hash = Hash.NewHash();
                            cloneAtt.Name += "_Clone";
                            EntityDataList.Instance.m_Objects.Add(cloneAtt.Hash, cloneAtt);

                            currentValue[i] = new Reference<AttributeBase>(cloneAtt.Hash);
                            m_Drawers[i] = ObjectBaseDrawer.GetDrawer(cloneAtt);
                        }
                    }

                    if (m_Open[i])
                    {
                        Color color3 = Color.red;
                        color3.a = .7f;

                        using (new EditorUtilities.BoxBlock(color3))
                        {
                            if (!currentValue[i].IsValid())
                            {
                                EditorGUILayout.HelpBox(
                                    "This attribute is invalid.",
                                    MessageType.Error);
                            }
                            else
                            {
                                EditorGUILayout.HelpBox(
                                    "This is shared attribute. Anything made changes in this inspector view will affect to original attribute directly not only as this entity.",
                                    MessageType.Info);

                                m_Drawers[i].OnGUI();
                                //SetAttribute(m_CurrentList[i], m_AttributeDrawers[i].OnGUI());
                            }
                        }

                        EditorUtilities.Line();
                    }
                }
            }

            if (showSelectContext)
            {
                var atts = EntityDataList.Instance.GetAttributesFor(
                                TypeHelper.TypeOf<AttributeBase>.Type, TargetObject.GetType());

                Rect tempRect = GUILayoutUtility.GetLastRect();
                tempRect.position = Event.current.mousePosition;
                PopupWindow.Show(tempRect,
                    SelectorPopup<Hash, AttributeBase>.GetWindow(atts,
                    attHash =>
                    {
                        currentValue[contextTarget] = new Reference<AttributeBase>(attHash);

                        AttributeBase targetAtt = currentValue[contextTarget].GetObject();
                        if (targetAtt != null)
                        {
                            m_Drawers[contextTarget] = ObjectBaseDrawer.GetDrawer(targetAtt);
                        }
                    },
                    att => att.Hash,
                    Hash.Empty)
                    );
            }

            return currentValue;
        }
        public static bool DrawAttributeSelector(string name, Hash current, Type entityType)
        {
            GUIContent displayName;
            EntityDataList.Instance.m_Objects.TryGetValue(current, out var attVal);
            //AttributeBase att = (AttributeBase)EntityDataList.Instance.GetObject(current);
            AttributeBase att = attVal == null ? null : (AttributeBase)attVal;
            if (current.Equals(Hash.Empty)) displayName = new GUIContent("None");
            else if (att == null) displayName = new GUIContent("Attribute Not Found");
            else displayName = new GUIContent(att.Name);

            Rect fieldRect;
            int selectorID;
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUI.indentLevel * 15);

                if (!string.IsNullOrEmpty(name))
                {
                    GUILayout.Label(name);
                }

                fieldRect = GUILayoutUtility.GetRect(displayName, EditorStyleUtilities.SelectorStyle, GUILayout.ExpandWidth(true));
                selectorID = GUIUtility.GetControlID(FocusType.Passive, fieldRect);
            }

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
                            //if (!EntityWindow.IsOpened) CoreSystemMenuItems.EntityDataListMenu();
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

                    Event.current.Use();

                    //Rect tempRect = GUILayoutUtility.GetLastRect();
                    //tempRect.position = Event.current.mousePosition;

                    //var atts = EntityDataList.Instance.GetData<AttributeBase>()
                    //    .Where((other) =>
                    //    {
                    //        Type attType = other.GetType();
                    //        bool attCheck = false;
                    //        if (acceptOnly != null)
                    //        {
                    //            for (int i = 0; i < acceptOnly.AttributeTypes.Length; i++)
                    //            {
                    //                if (acceptOnly.AttributeTypes[i].IsAssignableFrom(attType))
                    //                {
                    //                    attCheck = true;
                    //                    break;
                    //                }
                    //            }
                    //        }
                    //        else attCheck = true;

                    //        if (!attCheck) return false;
                    //        attCheck = false;

                    //        AttributeAcceptOnlyAttribute requireEntity = attType.GetCustomAttribute<AttributeAcceptOnlyAttribute>();
                    //        if (requireEntity == null) return true;

                    //        if (requireEntity.Types == null || requireEntity.Types.Length == 0)
                    //        {
                    //            return false;
                    //        }
                    //        else
                    //        {
                    //            for (int i = 0; i < requireEntity.Types.Length; i++)
                    //            {
                    //                if (requireEntity.Types[i].IsAssignableFrom(entityType))
                    //                {
                    //                    attCheck = true;
                    //                    break;
                    //                }
                    //            }
                    //        }

                    //        if (!attCheck) return false;
                    //        return true;
                    //    })
                    //    .ToArray();

                    //GUIUtility.ExitGUI();
                    //PopupWindow.Show(tempRect,
                    //    SelectorPopup<Hash, AttributeBase>.GetWindow(atts, setter, (att) =>
                    //    {
                    //        return att.Hash;
                    //    }, Hash.Empty)
                    //    );
                    return true;

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

            return false;
        }
    }
    //[EditorTool("TestTool", typeof(EntityWindow))]
    //public sealed class TestTool : EditorTool
    //{
    //    public override void OnToolGUI(EditorWindow window)
    //    {
    //        EditorGUILayout.LabelField("test");
    //        base.OnToolGUI(window);
    //    }
    //}
}
