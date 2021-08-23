using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation;
using System;
using System.Linq;
using System.Reflection;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    public sealed class EntityWindow : EditorWindowEntity<EntityWindow>
    {
        protected override string DisplayName => "Entity Window";

        ObjectBaseDrawer[] ObjectBaseDrawers;

        public DataListWindow m_DataListWindow;
        public ViewWindow m_ViewWindow;

        protected override void OnEnable()
        {
            ObjectBaseDrawers = new ObjectBaseDrawer[EntityDataList.Instance.m_Objects.Count];

            var temp = EntityDataList.Instance.m_Objects.Values.ToArray();
            for (int i = 0; i < temp.Length; i++)
            {
                ObjectBaseDrawers[i] = new ObjectBaseDrawer(temp[i]);
            }

            m_DataListWindow = new DataListWindow(this);
            m_ViewWindow = new ViewWindow(this);

            base.OnEnable();
        }

        Rect HeaderPos = new Rect(20, 10, 0, 0);
        Rect EntityListPos = new Rect(6, 45, 260, 465);
        Rect ViewPos = new Rect(265, 40, 687, 470);
        private void OnGUI()
        {
            //if (GUILayout.Button("save"))
            //{
            //    EntityDataList.Instance.SaveData();
            //}

            EditorGUI.LabelField(HeaderPos, EditorUtils.String("TEST Header", 20), EditorUtils.HeaderStyle);
            //HeaderPos = EditorGUILayout.RectField("headerPos", HeaderPos);
            //EntityListPos = EditorGUILayout.RectField("entitylistPos", EntityListPos);

            BeginWindows();

            m_DataListWindow.OnGUI(EntityListPos, 1);
            m_ViewWindow.OnGUI(ViewPos, 2);
            //GUILayout.Window(1, EntityListPos, m_DataListWindow.OnGUI, "", EditorUtils.Box);

            EndWindows();
            //for (int i = 0; i < ObjectBaseDrawers.Length; i++)
            //{
            //    ObjectBaseDrawers[i].OnGUI();
            //}
        }

        public sealed class DataListWindow
        {
            EntityWindow m_MainWindow;

            Vector2 scroll;
            Rect m_Position;
            int selection = 0;

            ObjectBaseDrawer[] Drawers => m_MainWindow.ObjectBaseDrawers;

            public DataListWindow(EntityWindow window)
            {
                m_MainWindow = window;
            }

            public void OnGUI(Rect pos, int unusedID)
            {
                m_Position = pos;

                GUILayout.Window(unusedID, m_Position, Draw, "", EditorUtils.Box);
            }
            private void Draw(int unusedID)
            {
                EditorGUILayout.BeginHorizontal();

                GUILayout.Button("test1");
                GUILayout.Button("test2");
                GUILayout.Button("test3");

                EditorGUILayout.EndHorizontal();

                scroll = EditorGUILayout.BeginScrollView(scroll, true,true,
                    GUILayout.MaxWidth(m_Position.width), GUILayout.MaxHeight(m_Position.height));

                selection = GUILayout.SelectionGrid(selection, Drawers.Select((other) => other.Name).ToArray(), 1);
                //EditorGUILayout.LabelField("test");
                //for (int i = 0; i < ObjectBaseDrawers.Length; i++)
                //{
                //    ObjectBaseDrawers[i].OnGUI();
                //}

                EditorGUILayout.EndScrollView();
            }
        }
        public sealed class ViewWindow
        {
            EntityWindow m_MainWindow;
            Rect m_Position;
            Vector2 m_Scroll;

            public ViewWindow(EntityWindow window)
            {
                m_MainWindow = window;
            }

            public void OnGUI(Rect pos, int unusedID)
            {
                m_Position = pos;
                GUILayout.Window(unusedID, m_Position, Draw, "", EditorUtils.Box);
            }
            private void Draw(int unusedID)
            {
                m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll, true, true,
                    GUILayout.MaxWidth(m_Position.width), GUILayout.MaxHeight(m_Position.height));

                m_MainWindow.HeaderPos = EditorGUILayout.RectField("headerPos", m_MainWindow. HeaderPos);
                m_MainWindow.EntityListPos = EditorGUILayout.RectField("entitylistPos", m_MainWindow. EntityListPos);

                m_MainWindow.ViewPos = EditorGUILayout.RectField("ViewPos", m_MainWindow.ViewPos);

                EditorGUILayout.EndScrollView();
            }
        }

        public sealed class ObjectBaseDrawer : ObjectDrawerBase
        {
            private readonly ObjectBase m_TargetObject;
            private Type m_Type;
            private ObsoleteAttribute m_Obsolete;

            private readonly MemberInfo[] m_Members;
            private readonly ObjectDrawerBase[] m_ObjectDrawers;

            public override object TargetObject => m_TargetObject;
            public override string Name => m_TargetObject.Name;

            public ObjectBaseDrawer(ObjectBase objectBase)
            {
                m_TargetObject = objectBase;
                m_Type = objectBase.GetType();
                m_Obsolete = m_Type.GetCustomAttribute<ObsoleteAttribute>();

                m_Members = ReflectionHelper.GetSerializeMemberInfos(m_Type);
                m_ObjectDrawers = new ObjectDrawerBase[m_Members.Length];
                for (int i = 0; i < m_ObjectDrawers.Length; i++)
                {
                    m_ObjectDrawers[i] = ToDrawer(m_TargetObject, m_Members[i]);
                }
            }
            public override void OnGUI()
            {
                EditorGUILayout.LabelField(Name);
                for (int i = 0; i < m_ObjectDrawers.Length; i++)
                {
                    if (m_ObjectDrawers[i] == null) continue;

                    try
                    {
                        m_ObjectDrawers[i].OnGUI();
                    }
                    catch (Exception)
                    {
                        EditorGUILayout.LabelField($"Error at {m_ObjectDrawers[i].Name}");
                    }
                }
                EditorUtils.Line();
            }
        }
    }
}
