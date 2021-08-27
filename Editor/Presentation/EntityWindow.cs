using NUnit.Framework;
using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Data;
using Syadeu.Presentation.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    public sealed class EntityWindow : EditorWindowEntity<EntityWindow>
    {
        protected override string DisplayName => "Entity Window";

        readonly List<ObjectBaseDrawer> ObjectBaseDrawers = new List<ObjectBaseDrawer>();

        public ToolbarWindow m_ToolbarWindow;
        public DataListWindow m_DataListWindow;
        public ViewWindow m_ViewWindow;

        public ObjectBaseDrawer m_SelectedObject = null;

        protected override void OnEnable()
        {
            m_ToolbarWindow = new ToolbarWindow(this);
            m_DataListWindow = new DataListWindow(this);
            m_ViewWindow = new ViewWindow(this);

            Reload();

            base.OnEnable();
        }
        public void AddData(ObjectBase other)
        {
            ObjectBaseDrawer drawer;

            drawer = ObjectBaseDrawer.GetDrawer(other);

            ObjectBaseDrawers.Add(drawer);
            m_DataListWindow.AddData(drawer);
        }
        public void Reload()
        {
            ObjectBaseDrawers.Clear();
            var temp = EntityDataList.Instance.m_Objects.Values.ToArray();
            for (int i = 0; i < temp.Length; i++)
            {
                AddData(temp[i]);
            }

            m_DataListWindow.Reload();
        }
        public void Remove(ObjectBaseDrawer objectBase)
        {
            ObjectBaseDrawers.Remove(objectBase);

            EntityDataList.Instance.m_Objects.Remove(objectBase.m_TargetObject.Hash);
        }

        private Rect m_CopyrightRect = new Rect(350, 485, 245, 20);

        Rect HeaderPos = new Rect(20, 33, 0, 0);
        Rect HeaderLinePos = new Rect(0, 60, 0, 0);

        Rect EntityListPos = new Rect(6, 60, 260, 430);
        Rect ViewPos = new Rect(265, 60, 687, 430);

        private void OnGUI()
        {
            EditorStyles.textField.wordWrap = true;

            m_ToolbarWindow.OnGUI();

            EditorGUI.LabelField(HeaderPos, EditorUtils.String("Entity Window", 20), EditorUtils.HeaderStyle);
            HeaderLinePos.width = Screen.width;
            EditorUtils.Line(HeaderLinePos);
            
            BeginWindows();

            m_DataListWindow.OnGUI(EntityListPos, 1);
            m_ViewWindow.OnGUI(ViewPos, 2);

            EndWindows();

            EditorGUI.LabelField(m_CopyrightRect, EditorUtils.String("Copyright 2021 Syadeu. All rights reserved.", 11), EditorUtils.CenterStyle);
        }
        
        public sealed class ToolbarWindow
        {
            EntityWindow m_MainWindow;
            GenericMenu m_FileMenu;

            bool m_IsLoaded = false;

            Rect lastRect;

            public ToolbarWindow(EntityWindow window)
            {
                m_MainWindow = window;

                m_FileMenu = new GenericMenu();
                m_FileMenu.AddItem(new GUIContent("Save"), false, SaveMenu);
                m_FileMenu.AddItem(new GUIContent("Load"), false, LoadMenu);
                m_FileMenu.AddSeparator(string.Empty);
                m_FileMenu.AddItem(new GUIContent("Add/Entity"), false, AddDataMenu<EntityDataBase>);
                m_FileMenu.AddItem(new GUIContent("Add/Attribute"), false, AddDataMenu<AttributeBase>);
                m_FileMenu.AddItem(new GUIContent("Add/Action"), false, AddDataMenu<ActionBase>);
                m_FileMenu.AddItem(new GUIContent("Add/Data"), false, AddDataMenu<DataObjectBase>);
            }
            private void SaveMenu()
            {
                if (!m_IsLoaded) return;

                EntityDataList.Instance.SaveData();
            }
            private void LoadMenu()
            {
                if (!Application.isPlaying) EntityDataList.Instance.LoadData();

                m_MainWindow.Reload();

                m_IsLoaded = true;
            }
            private void AddDataMenu<T>() where T : ObjectBase
            {
                if (!m_IsLoaded) LoadMenu();

                Type[] types = TypeHelper.GetTypes((other) => !other.IsAbstract && TypeHelper.TypeOf<T>.Type.IsAssignableFrom(other));

                PopupWindow.Show(lastRect, SelectorPopup<Type, Type>.GetWindow(types,
                    (t) =>
                    {
                        if (EntityDataList.Instance.m_Objects == null) EntityDataList.Instance.m_Objects = new Dictionary<Hash, ObjectBase>();

                        T ins = (T)Activator.CreateInstance(t);

                        EntityDataList.Instance.m_Objects.Add(ins.Hash, ins);
                        m_MainWindow.AddData(ins);
                    },
                    (t) => t,
                    null,
                    (t) =>
                    {
                        string output = string.Empty;

                        if (t.GetCustomAttribute<ObsoleteAttribute>() != null)
                        {
                            output += "[Deprecated] ";
                        }

                        DisplayNameAttribute displayName = t.GetCustomAttribute<DisplayNameAttribute>();
                        if (displayName != null)
                        {
                            output += displayName.DisplayName;
                        }
                        else output += t.Name;

                        return output;
                    }));
            }

            public void OnGUI()
            {
                using (new EditorUtils.BoxBlock(Color.black))
                {
                    EditorGUILayout.BeginHorizontal();
                    DrawTools();
                    EditorGUILayout.EndHorizontal();
                }
            }
            private void DrawTools()
            {
                if (GUILayout.Button("File", EditorStyles.toolbarDropDown))
                {
                    lastRect = GUILayoutUtility.GetLastRect();
                    lastRect.position = Event.current.mousePosition;

                    m_FileMenu.ShowAsContext();
                    GUIUtility.ExitGUI();
                }
                GUILayout.FlexibleSpace();
                //if (GUILayout.Button("Tools", EditorStyles.toolbarDropDown))
                //{

                //}
            }
        }

        public sealed class DataListWindow
        {
            EntityWindow m_MainWindow;

            Vector2 m_Scroll;
            Rect m_Position;
            int m_Selection = 0;

            List<ObjectBaseDrawer> Drawers => m_MainWindow.ObjectBaseDrawers;

            readonly List<Folder> Objects = new List<Folder>();
            readonly SearchField m_SearchField;
            string m_SearchText = string.Empty;

            public sealed class Folder
            {
                public string Name
                {
                    get
                    {
                        string output = string.Empty;

                        if (Type.GetCustomAttribute<ObsoleteAttribute>() != null)
                        {
                            output += "[Deprecated] ";
                        }

                        DisplayNameAttribute displayName = Type.GetCustomAttribute<DisplayNameAttribute>();
                        if (displayName != null)
                        {
                            output += displayName.DisplayName;
                        }
                        else output += Type.Name;

                        return output;
                    }
                }
                public Type Type;

                public bool Open = false;
                public readonly List<Element> m_Elements = new List<Element>();
            }
            public sealed class Element
            {
                //public int Index;
                public ObjectBaseDrawer Target;
            }
            
            public DataListWindow(EntityWindow window)
            {
                m_MainWindow = window;
                m_SearchField = new SearchField();

                Reload();
            }
            public void AddData(ObjectBaseDrawer drawer)
            {
                Folder baseType = Objects.Find((other) => other.Type.Equals(drawer.Type));
                if (baseType == null)
                {
                    baseType = new Folder
                    {
                        Type = drawer.Type
                    };
                    Objects.Add(baseType);
                }
                baseType.m_Elements.Add(new Element { Target = drawer });
            }
            public void Reload()
            {
                if (m_Selection < Drawers.Count)
                {
                    m_MainWindow.m_SelectedObject = Drawers[m_Selection];
                }
                else m_MainWindow.m_SelectedObject = null;

                Objects.Clear();
                for (int i = 0; i < Drawers.Count; i++)
                {
                    AddData(Drawers[i]);
                }

                m_SearchText = string.Empty;
            }

            public void OnGUI(Rect pos, int unusedID)
            {
                m_Position = pos;

                GUILayout.Window(unusedID, m_Position, Draw, string.Empty, EditorUtils.Box);
            }
            private void Draw(int unusedID)
            {
                EditorGUI.BeginChangeCheck();
                m_SearchText = m_SearchField.OnToolbarGUI(GUILayoutUtility.GetRect(m_Position.width, 20), m_SearchText);
                if (EditorGUI.EndChangeCheck())
                {
                    if (string.IsNullOrEmpty(m_SearchText))
                    {
                        for (int i = 0; i < Objects.Count; i++)
                        {
                            Objects[i].Open = false;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < Objects.Count; i++)
                        {
                            if (Objects[i].m_Elements
                                .Select((other) => other.Target)
                                .Where((other) => other.Name.ToLower().Contains(m_SearchText.ToLower()))
                                .Any())
                            {
                                Objects[i].Open = true;
                            }
                            else Objects[i].Open = false;
                        }
                    }
                }

                m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll, false, true,
                    GUILayout.MaxWidth(m_Position.width), GUILayout.MaxHeight(m_Position.height));

                EditorUtils.BoxBlock box = new EditorUtils.BoxBlock(Color.white, GUILayout.MaxWidth(m_Position.width- 20));
                for (int i = 0; i < Objects.Count; i++)
                {
                    Objects[i].Open = EditorGUILayout.Foldout(Objects[i].Open, Objects[i].Name, true);
                    if (!Objects[i].Open) continue;

                    EditorGUI.indentLevel++;
                    using (new EditorUtils.BoxBlock(Color.white))
                    {
                        for (int a = 0; a < Objects[i].m_Elements.Count; a++)
                        {
                            ObjectBaseDrawer target = Objects[i].m_Elements[a].Target;

                            if (!string.IsNullOrEmpty(m_SearchText) &&
                                !target.Name.ToLower().Contains(m_SearchText.ToLower()))
                            {
                                continue;
                            }

                            bool enabled;
                            if (m_MainWindow.m_SelectedObject == null) enabled = false;
                            else
                            {
                                enabled = m_MainWindow.m_SelectedObject.Equals(target);
                            }

                            EditorGUILayout.BeginHorizontal();

                            EditorGUI.BeginChangeCheck();
                            enabled = GUILayout.Toggle(enabled, target.Name, EditorStyles.toolbarButton);
                            if (EditorGUI.EndChangeCheck())
                            {
                                if (enabled) m_MainWindow.m_SelectedObject = target;
                            }

                            if (GUILayout.Button("-", GUILayout.Width(20)))
                            {
                                m_MainWindow.Remove(target);
                                Objects[i].m_Elements.RemoveAt(a);
                                a--;
                                //GUIUtility.ExitGUI();
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    EditorUtils.Line();
                    EditorGUI.indentLevel--;
                }
                box.Dispose();

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
                GUILayout.Window(unusedID, m_Position, Draw, string.Empty, EditorUtils.Box);
            }
            private void Draw(int unusedID)
            {
                m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll, true, true,
                    GUILayout.MaxWidth(m_Position.width), GUILayout.MaxHeight(m_Position.height));

                #region TestRect Controller

                //m_MainWindow.m_CopyrightRect = EditorGUILayout.RectField("copyright", m_MainWindow.m_CopyrightRect);
                //m_MainWindow.HeaderPos = EditorGUILayout.RectField("headerPos", m_MainWindow. HeaderPos);
                //m_MainWindow.HeaderLinePos = EditorGUILayout.RectField("HeaderLinePos", m_MainWindow.HeaderLinePos);
                //m_MainWindow.EntityListPos = EditorGUILayout.RectField("entitylistPos", m_MainWindow. EntityListPos);

                //m_MainWindow.ViewPos = EditorGUILayout.RectField("ViewPos", m_MainWindow.ViewPos);
                //EditorGUILayout.Space();

                #endregion

                if (m_MainWindow.m_SelectedObject != null)
                {
                    m_MainWindow.m_SelectedObject.OnGUI();
                }
                else
                {
                    EditorGUILayout.LabelField("select object");
                }

                EditorGUILayout.EndScrollView();
            }
        }

        public sealed class EntityDrawer : ObjectBaseDrawer
        {
            public EntityDataBase Target => (EntityDataBase)m_TargetObject;
            readonly ReflectionHelperEditor.AttributeListDrawer m_AttributeDrawer;

            public EntityDrawer(ObjectBase objectBase) : base(objectBase)
            {
                m_AttributeDrawer = ReflectionHelperEditor.GetAttributeDrawer(Type, Target.Attributes);
            }

            protected override void DrawGUI()
            {
                EditorUtils.StringRich(Name + EditorUtils.String($": {Type.Name}", 11), 20);
                EditorGUILayout.Space(3);
                EditorUtils.Line();

                Target.Name = EditorGUILayout.TextField("Name: ", Target.Name);
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("Hash: ", Target.Hash.ToString());
                EditorGUI.EndDisabledGroup();
                if (Target is EntityBase entityBase)
                {
                    ReflectionHelperEditor.DrawPrefabReference("Prefab: ",
                        (idx) =>
                        {
                            entityBase.Prefab = idx;
                            if (idx >= 0)
                            {
                                GameObject temp = (GameObject)entityBase.Prefab.GetObjectSetting().m_RefPrefab.editorAsset;
                                Transform tr = temp.transform;

                                AABB aabb = new AABB(tr.position, float3.zero);
                                foreach (var item in tr.GetComponentsInChildren<Renderer>())
                                {
                                    aabb.Encapsulate(item.bounds);
                                }
                                entityBase.Center = aabb.center - ((float3)tr.position);
                                entityBase.Size = aabb.size;
                            }
                        }
                        , entityBase.Prefab);
                }
                using (new EditorUtils.BoxBlock(Color.black))
                {
                    m_AttributeDrawer.OnGUI();
                }
                EditorUtils.Line();

                for (int i = 0; i < m_ObjectDrawers.Length; i++)
                {
                    if (m_ObjectDrawers[i] == null) continue;

                    if (m_ObjectDrawers[i].Name.Equals("Name") ||
                        m_ObjectDrawers[i].Name.Equals("Hash") ||
                        m_ObjectDrawers[i].Name.Equals("Prefab") ||
                        m_ObjectDrawers[i].Name.Equals("Attributes"))
                    {
                        continue;
                    }

                    DrawField(m_ObjectDrawers[i]);
                }
            }
        }

        public class ObjectBaseDrawer : ObjectDrawerBase
        {
            protected static readonly Dictionary<ObjectBase, ObjectBaseDrawer> Pool = new Dictionary<ObjectBase, ObjectBaseDrawer>();

            public readonly ObjectBase m_TargetObject;
            private Type m_Type;
            private ObsoleteAttribute m_Obsolete;

            private readonly MemberInfo[] m_Members;
            protected readonly ObjectDrawerBase[] m_ObjectDrawers;

            public override sealed object TargetObject => m_TargetObject;
            public Type Type => m_Type;
            public override string Name => m_TargetObject.Name;
            public override int FieldCount => m_ObjectDrawers.Length;

            public static ObjectBaseDrawer GetDrawer(ObjectBase objectBase)
            {
                if (Pool.TryGetValue(objectBase, out var drawer)) return drawer;

                if (TypeHelper.TypeOf<EntityDataBase>.Type.IsAssignableFrom(objectBase.GetType()))
                {
                    drawer = new EntityDrawer(objectBase);
                }
                else drawer = new ObjectBaseDrawer(objectBase);

                Pool.Add(objectBase, drawer);

                return drawer;
            }

            protected ObjectBaseDrawer(ObjectBase objectBase)
            {
                m_TargetObject = objectBase;
                m_Type = objectBase.GetType();
                m_Obsolete = m_Type.GetCustomAttribute<ObsoleteAttribute>();

                m_Members = ReflectionHelper.GetSerializeMemberInfos(m_Type);
                m_ObjectDrawers = new ObjectDrawerBase[m_Members.Length];
                for (int i = 0; i < m_ObjectDrawers.Length; i++)
                {
                    m_ObjectDrawers[i] = ToDrawer(m_TargetObject, m_Members[i], true);
                }
            }
            public override sealed void OnGUI()
            {
                const string c_ObsoleteMsg = "This type marked as deprecated.\n{0}";

                using (new EditorUtils.BoxBlock(Color.black))
                {
                    if (m_Obsolete != null)
                    {
                        EditorGUILayout.HelpBox(string.Format(c_ObsoleteMsg, m_Obsolete.Message), 
                            m_Obsolete.IsError ? MessageType.Error : MessageType.Warning);
                    }

                    DrawGUI();
                }
            }
            protected virtual void DrawGUI()
            {
                EditorUtils.StringRich(Name + EditorUtils.String($": {Type.Name}", 11), 20);
                EditorGUILayout.Space(3);
                EditorUtils.Line();
                for (int i = 0; i < m_ObjectDrawers.Length; i++)
                {
                    DrawField(m_ObjectDrawers[i]);
                }
            }
            protected void DrawField(ObjectDrawerBase drawer)
            {
                if (drawer == null)
                {
                    EditorGUILayout.LabelField($"not support");
                    return;
                }
                try
                {
                    drawer.OnGUI();
                }
                catch (Exception ex)
                {
                    EditorGUILayout.LabelField($"Error at {drawer.Name} {ex.Message}");
                    Debug.LogException(ex);
                }
            }
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
