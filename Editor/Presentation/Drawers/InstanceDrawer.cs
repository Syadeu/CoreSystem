using Syadeu.Collections;
using Syadeu.Presentation;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    public sealed class InstanceDrawer : ObjectDrawer<IInstance>
    {
        private MemberInfo m_MemberInfo;
        private GUIContent m_DisplayName;
        private bool IsHover;

        public InstanceDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
            m_MemberInfo = memberInfo;

            IInstance target = Getter.Invoke();
            if (target.IsValid())
            {
                m_DisplayName = new GUIContent(target.GetObject().Name);
            }
            else
            {
                m_DisplayName = new GUIContent("INVALID");
            }
        }

        public override IInstance Draw(IInstance currentValue)
        {
            using (new EditorUtils.BoxBlock(Color.black))
            {
                DrawField();
            }

            return currentValue;
        }
        private void DrawField()
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUI.indentLevel * 15);
                GUILayout.Label(Name, GUILayout.Width(Screen.width * .25f));

                Rect fieldRect = GUILayoutUtility.GetRect(m_DisplayName, ReflectionHelperEditor.SelectorStyle, GUILayout.ExpandWidth(true));
                int selectorID = GUIUtility.GetControlID(FocusType.Passive, fieldRect);

                switch (Event.current.GetTypeForControl(selectorID))
                {
                    case EventType.Repaint:
                        IsHover = fieldRect.Contains(Event.current.mousePosition);
                        ReflectionHelperEditor.SelectorStyle.Draw(fieldRect, m_DisplayName, IsHover, isActive: false, on: false, false);
                        break;
                    case EventType.ContextClick:
                        if (!fieldRect.Contains(Event.current.mousePosition)) break;

                        Event.current.Use();

                        GenericMenu menu = new GenericMenu();
                        menu.AddDisabledItem(m_DisplayName);
                        menu.AddSeparator(string.Empty);

                        if (EntityWindow.IsOpened)
                        {
                            EntityWindow.Instance.CurrentWindow = EntityWindow.WindowType.Debugger;

                            menu.AddItem(new GUIContent("To Reference"), false, () =>
                            {
                                EntityWindow.Instance.m_DebuggerListWindow.Select(Getter.Invoke());
                                EntityWindow.Instance.m_DebuggerViewWindow.Selected = new Instance<ObjectBase>(Getter.Invoke().Idx);
                            });
                        }

                        menu.ShowAsContext();

                        break;
                    default:
                        break;
                }
            }
        }
    }
}
