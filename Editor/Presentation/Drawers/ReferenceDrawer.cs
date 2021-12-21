using Syadeu;
using Syadeu.Collections;
using Syadeu.Internal;
using Syadeu.Presentation;
using SyadeuEditor.Utilities;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    public sealed class ReferenceDrawer : ObjectDrawer<IFixedReference>
    {
        private bool 
            m_Open, 
            m_WasEdited = false;

        public ReferenceDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }
        public ReferenceDrawer(IList list, int index, Type elementType) : base(list, index, elementType)
        {
        }
        public ReferenceDrawer(object parentObject, Type declaredType, Action<IFixedReference> setter, Func<IFixedReference> getter) : base(parentObject, declaredType, setter, getter)
        {
        }

        public override IFixedReference Draw(IFixedReference currentValue)
        {
            Type targetType;
            Type[] generics = DeclaredType.GetGenericArguments();
            if (generics.Length > 0) targetType = generics[0];
            else targetType = null;

            using (new GUILayout.VerticalScope())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawReferenceSelector(Name, (idx) =>
                    {
                        ObjectBase objBase = EntityDataList.Instance.GetObject(idx);

                        object temp = TypeHelper.GetConstructorInfo(DeclaredType, TypeHelper.TypeOf<ObjectBase>.Type).Invoke(
                            new object[] { objBase });

                        IFixedReference origin = Getter.Invoke();
                        Setter.Invoke((IFixedReference)temp);
                        m_WasEdited = !origin.Equals(Getter.Invoke());
                    }, currentValue, targetType);
                    if (m_WasEdited)
                    {
                        GUI.changed = true;
                        m_WasEdited = false;
                    }

                    m_Open = GUILayout.Toggle(m_Open,
                                m_Open ? EditorStyleUtilities.FoldoutOpendString : EditorStyleUtilities.FoldoutClosedString
                                , EditorStyleUtilities.MiniButton, GUILayout.Width(20));

                    using (new EditorGUI.DisabledGroupScope(!currentValue.IsValid()))
                    {
                        if (GUILayout.Button("C", GUILayout.Width(20)))
                        {
                            ObjectBase clone = (ObjectBase)currentValue.GetObject().Clone();

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

                            object temp = TypeHelper.GetConstructorInfo(DeclaredType, TypeHelper.TypeOf<ObjectBase>.Type).Invoke(
                                new object[] { clone });
                            currentValue = (IFixedReference)temp;
                        }
                    }
                }

                if (m_Open)
                {
                    Color color3 = Color.red;
                    color3.a = .7f;

                    EditorGUI.indentLevel++;

                    using (new EditorUtilities.BoxBlock(color3))
                    {
                        if (!currentValue.IsValid())
                        {
                            EditorGUILayout.HelpBox(
                                "This reference is invalid.",
                                MessageType.Error);
                        }
                        else
                        {
                            EditorGUILayout.HelpBox(
                                "This is shared reference. Anything made changes in this inspector view will affect to original reference directly.",
                                MessageType.Info);

                            ObjectBaseDrawer.GetDrawer((ObjectBase)currentValue.GetObject()).OnGUI();
                        }
                    }

                    EditorGUI.indentLevel--;
                }
            }

            return currentValue;
        }

        public static void DrawReferenceSelector(string name, Action<Hash> setter, IFixedReference current, Type targetType)
        {
            GUIContent displayName;
            ObjectBase objBase = null;
            if (current == null || current.Hash.Equals(Hash.Empty)) displayName = new GUIContent("None");
            else
            {
                objBase = EntityDataList.Instance.GetObject(current.Hash);
                if (objBase == null) displayName = new GUIContent("None");
                else displayName = new GUIContent(objBase.Name);
            }

            Rect fieldRect;
            int selectorID;
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUI.indentLevel * 15);

                if (!string.IsNullOrEmpty(name))
                {
                    GUILayout.Label(name, GUILayout.Width(Screen.width * .25f));
                }

                fieldRect = GUILayoutUtility.GetRect(displayName, EditorStyles.textField/*, GUILayout.ExpandWidth(true)*/);
                selectorID = GUIUtility.GetControlID(FocusType.Passive, fieldRect);
            }

            bool isHover = fieldRect.Contains(Event.current.mousePosition);

            switch (Event.current.GetTypeForControl(selectorID))
            {
                case EventType.Repaint:
                    EditorStyleUtilities.SelectorStyle.Draw(fieldRect, displayName,
                        isHover, isActive: true, on: true, false);

                    break;
                case EventType.ContextClick:
                    if (!isHover) break;

                    GenericMenu menu = new GenericMenu();
                    menu.AddDisabledItem(displayName);
                    menu.AddSeparator(string.Empty);

                    if (current.IsValid())
                    {
                        menu.AddItem(new GUIContent("Find Referencers"), false, () =>
                        {
                            //if (!EntityWindow.IsOpened) CoreSystemMenuItems.EntityDataListMenu();

                            EntityWindow.Instance.m_DataListWindow.SearchString = $"ref:{current.Hash}";
                        });
                        menu.AddItem(new GUIContent("To Reference"), false, () =>
                        {
                            EntityWindow.Instance.m_DataListWindow.Select(current);
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
                            setter.Invoke(obj.Hash);
                        });
                    }
                    else menu.AddDisabledItem(new GUIContent($"Create New {displayName.text}"));

                    menu.ShowAsContext();

                    Event.current.Use();
                    break;
                case EventType.MouseDown:
                    if (!isHover)
                    {
                        break;
                    }

                    if (Event.current.button == 0)
                    {
                        GUIUtility.hotControl = selectorID;
                        DrawSelectionWindow(setter, targetType);

                        GUI.changed = true;
                        Event.current.Use();
                    }

                    break;
                case EventType.MouseUp:
                    //if (!fieldRect.Contains(Event.current.mousePosition)) break;

                    //if (Event.current.button == 0)
                    //{
                    //    GUIUtility.hotControl = selectorID;
                    //    DrawSelectionWindow(setter, targetType);

                    //    GUI.changed = true;
                    //    Event.current.Use();
                    //}
                    if (GUIUtility.hotControl == selectorID)
                    {
                        GUIUtility.hotControl = 0;
                    }
                    break;
                default:
                    break;
            }
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
