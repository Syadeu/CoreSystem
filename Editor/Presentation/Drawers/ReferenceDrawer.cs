using Syadeu;
using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation;
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    public sealed class ReferenceDrawer : ObjectDrawer<IReference>
    {
        private bool 
            m_Open, 
            m_WasEdited = false;

        public ReferenceDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }

        public ReferenceDrawer(object parentObject, Type declaredType, Action<IReference> setter, Func<IReference> getter) : base(parentObject, declaredType, setter, getter)
        {
        }

        public override IReference Draw(IReference currentValue)
        {
            Type targetType;
            Type[] generics = DeclaredType.GetGenericArguments();
            if (generics.Length > 0) targetType = generics[0];
            else targetType = null;

            GUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();

            DrawReferenceSelector(Name, (idx) =>
            {
                ObjectBase objBase = EntityDataList.Instance.GetObject(idx);

                object temp = TypeHelper.GetConstructorInfo(DeclaredType, TypeHelper.TypeOf<ObjectBase>.Type).Invoke(
                    new object[] { objBase });

                IReference origin = Getter.Invoke();
                Setter.Invoke((IReference)temp);
                m_WasEdited = !origin.Equals(Getter.Invoke());
            }, currentValue, targetType);
            if (m_WasEdited)
            {
                GUI.changed = true;
                m_WasEdited = false;
            }

            m_Open = GUILayout.Toggle(m_Open,
                        m_Open ? EditorUtils.FoldoutOpendString : EditorUtils.FoldoutClosedString
                        , EditorUtils.MiniButton, GUILayout.Width(20));

            EditorGUI.BeginDisabledGroup(!currentValue.IsValid());
            if (GUILayout.Button("C", GUILayout.Width(20)))
            {
                ObjectBase clone = (ObjectBase)currentValue.GetObject().Clone();

                clone.Hash = Hash.NewHash();
                clone.Name += "_Clone";

                if (EntityWindow.IsOpened)
                {
                    EntityWindow.Instance.Add(clone);
                }
                else EntityDataList.Instance.m_Objects.Add(clone.Hash, clone);

                object temp = TypeHelper.GetConstructorInfo(DeclaredType, TypeHelper.TypeOf<ObjectBase>.Type).Invoke(
                    new object[] { clone });
                currentValue = (IReference)temp;
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            if (m_Open)
            {
                Color color3 = Color.red;
                color3.a = .7f;

                EditorGUI.indentLevel++;

                using (new EditorUtils.BoxBlock(color3))
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

                        ObjectBaseDrawer.GetDrawer(currentValue.GetObject()).OnGUI();
                    }
                }

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            return currentValue;
        }

        public static void DrawReferenceSelector(string name, Action<Hash> setter, IReference current, Type targetType)
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

            GUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 15);

            if (!string.IsNullOrEmpty(name))
            {
                GUILayout.Label(name, GUILayout.Width(Screen.width * .25f));
            }

            Rect fieldRect = GUILayoutUtility.GetRect(displayName, EditorStyles.textField, GUILayout.ExpandWidth(true));
            
            int selectorID = GUIUtility.GetControlID(FocusType.Passive, fieldRect);

            switch (Event.current.GetTypeForControl(selectorID))
            {
                case EventType.Repaint:
                    bool isHover = fieldRect.Contains(Event.current.mousePosition);

                    ReflectionHelperEditor.SelectorStyle.Draw(fieldRect, displayName,
                        isHover, isActive: true, on: true, false);

                    break;
                case EventType.ContextClick:
                    if (!fieldRect.Contains(Event.current.mousePosition)) break;

                    GenericMenu menu = new GenericMenu();
                    menu.AddDisabledItem(displayName);
                    menu.AddSeparator(string.Empty);

                    if (current.IsValid())
                    {
                        menu.AddItem(new GUIContent("Find Referencers"), false, () =>
                        {
                            if (!EntityWindow.IsOpened) CoreSystemMenuItems.EntityDataListMenu();

                            EntityWindow.Instance.m_DataListWindow.SearchString = $"ref:{current.Hash}";
                        });
                        menu.AddItem(new GUIContent("To Reference"), false, () =>
                        {
                            EntityWindow.Instance.Select(current);
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
                            var obj = EntityWindow.Instance.Add(targetType).m_TargetObject;
                            setter.Invoke(obj.Hash);
                        });
                    }
                    else menu.AddDisabledItem(new GUIContent($"Create New {displayName.text}"));
                    
                    menu.ShowAsContext();

                    Event.current.Use();
                    break;
                case EventType.MouseDown:
                    if (!fieldRect.Contains(Event.current.mousePosition)) break;

                    if (Event.current.button == 0)
                    {
                        GUIUtility.hotControl = selectorID;
                        DrawSelectionWindow(setter, targetType);
                        GUI.changed = true;
                        Event.current.Use();
                    }

                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == selectorID)
                    {
                        GUIUtility.hotControl = 0;
                    }
                    break;
                default:
                    break;
            }

            GUILayout.EndHorizontal();

            static void DrawSelectionWindow(Action<Hash> setter, Type targetType)
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
        }
    }
}
