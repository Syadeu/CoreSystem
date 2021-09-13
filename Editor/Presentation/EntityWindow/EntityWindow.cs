using NUnit.Framework;
using Syadeu;
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
        protected override string DisplayName
        {
            get
            {
                if (IsDirty) return "Entity Window*";
                return "Entity Window";
            }
        }

        readonly List<ObjectBaseDrawer> ObjectBaseDrawers = new List<ObjectBaseDrawer>();

        public bool IsDirty
        {
            get => hasUnsavedChanges;
            set
            {
                saveChangesMessage = "Unsaved changes detected";
                hasUnsavedChanges = value;
            }
        }

        private WindowType m_CurrentWindow = WindowType.Entity;

        public ToolbarWindow m_ToolbarWindow;

        public EntityDataListWindow m_DataListWindow;
        public EntityViewWindow m_ViewWindow;

        public ObjectBaseDrawer m_SelectedObject = null;

        public static bool IsOpened { get; private set; }
        public static bool IsDataLoaded => EntityDataList.IsLoaded;

        protected override void OnEnable()
        {
            IsOpened = true;

            m_ToolbarWindow = new ToolbarWindow(this);
            m_DataListWindow = new EntityDataListWindow(this);
            m_ViewWindow = new EntityViewWindow(this);

            Reload();

            base.OnEnable();
        }
        protected override void OnDisable()
        {
            IsOpened = false;

            base.OnDisable();
        }
        public override void SaveChanges()
        {
            CoreSystem.Logger.Log(Channel.Editor, "Entity data saved");

            EntityDataList.Instance.SaveData();
            base.SaveChanges();

            IsDirty = false;
            Repaint();
        }
        private void OnDestroy()
        {
            if (IsDirty) EntityDataList.Instance.LoadData();
        }
        public void Reload()
        {
            if (!IsDataLoaded) EntityDataList.Instance.LoadData();

            ObjectBaseDrawers.Clear();
            if (EntityDataList.Instance.m_Objects != null)
            {
                foreach (var item in EntityDataList.Instance.m_Objects.Values)
                {
                    var drawer = ObjectBaseDrawer.GetDrawer(item);
                    ObjectBaseDrawers.Add(drawer);
                }
            }

            m_DataListWindow.Reload();
            CoreSystem.Logger.Log(Channel.Editor, "Entity data loaded");
        }

        public void Select(IReference reference)
        {
            var obj = reference.GetObject();
            if (obj == null)
            {
                "reference not found return".ToLog();
                return;
            }

            var iter = ObjectBaseDrawers.Where((other) => other.m_TargetObject.Equals(obj));
            if (!iter.Any())
            {
                "reference drawer not found return".ToLog();
                return;
            }

            m_SelectedObject = iter.First();
            m_DataListWindow.Select(m_SelectedObject);
        }
        public void Select(ObjectBaseDrawer drawer)
        {
            var iter = ObjectBaseDrawers.Where((other) => other.Equals(drawer));
            if (!iter.Any())
            {
                "reference drawer not found return".ToLog();
                return;
            }

            m_SelectedObject = drawer;
            m_DataListWindow.Select(drawer);
        }
        public ObjectBaseDrawer Add(Type objType)
        {
            if (EntityDataList.Instance.m_Objects == null) EntityDataList.Instance.m_Objects = new Dictionary<Hash, ObjectBase>();

            ObjectBase ins = (ObjectBase)Activator.CreateInstance(objType);
            EntityDataList.Instance.m_Objects.Add(ins.Hash, ins);

            ObjectBaseDrawer drawer = ObjectBaseDrawer.GetDrawer(ins);
            ObjectBaseDrawers.Add(drawer);
            m_DataListWindow.Add(drawer);

            IsDirty = true;
            return drawer;
        }
        public ObjectBaseDrawer Add(ObjectBase ins)
        {
            if (EntityDataList.Instance.m_Objects == null) EntityDataList.Instance.m_Objects = new Dictionary<Hash, ObjectBase>();

            EntityDataList.Instance.m_Objects.Add(ins.Hash, ins);

            ObjectBaseDrawer drawer = ObjectBaseDrawer.GetDrawer(ins);
            ObjectBaseDrawers.Add(drawer);
            m_DataListWindow.Add(drawer);

            IsDirty = true;
            return drawer;
        }

        public void Remove(ObjectBaseDrawer obj)
        {
            if (m_SelectedObject != null && m_SelectedObject.Equals(obj)) m_SelectedObject = null;

            ObjectBaseDrawers.Remove(obj);
            m_DataListWindow.Remove(obj);

            EntityDataList.Instance.m_Objects.Remove(obj.m_TargetObject.Hash);

            IsDirty = true;
        }

        private const string c_CopyrightText = "Copyright 2021 Syadeu. All rights reserved.";
        private Rect m_CopyrightRect = new Rect(350, 485, 245, 20);

        Rect HeaderPos = new Rect(20, 33, 0, 0);
        Rect HeaderLinePos = new Rect(0, 60, 0, 0);

        Rect EntityListPos = new Rect(6, 60, 260, 430);
        Rect ViewPos = new Rect(265, 60, 687, 430);

        private void OnGUI()
        {
            EditorStyles.textField.wordWrap = true;

            m_ToolbarWindow.OnGUI();

            string headerString = EditorUtils.String($"{m_CurrentWindow} Window", 20);
            if (IsDirty)
            {
                headerString += EditorUtils.String(": Modified", 10);
            }

            EditorGUI.LabelField(HeaderPos,
                headerString, 
                EditorUtils.HeaderStyle);
            HeaderLinePos.width = Screen.width;
            EditorUtils.Line(HeaderLinePos);

            EntityListPos.height = Screen.height - 95;
            ViewPos.width = Screen.width - EntityListPos.width - 5;
            ViewPos.height = EntityListPos.height;
            BeginWindows();

            m_DataListWindow.OnGUI(EntityListPos, 1);
            m_ViewWindow.OnGUI(ViewPos, 2);

            EndWindows();

            m_CopyrightRect.width = Screen.width;
            m_CopyrightRect.x = 0;
            m_CopyrightRect.y = Screen.height - 42;
            EditorGUI.LabelField(m_CopyrightRect, EditorUtils.String(c_CopyrightText, 11), EditorUtils.CenterStyle);

            KeyboardShortcuts();
        }
        private void KeyboardShortcuts()
        {
            if (!Event.current.isKey) return;

            if (Event.current.control)
            {
                if (Event.current.keyCode == KeyCode.S)
                {
                    if (IsDirty) SaveChanges();
                }
                else if (Event.current.keyCode == KeyCode.R)
                {
                    Reload();
                }
            }
        }
        
        public enum WindowType
        {
            Entity,
            Converter,
        }

        public sealed class ToolbarWindow
        {
            EntityWindow m_MainWindow;

            GenericMenu 
                m_FileMenu,
                m_WindowMenu;

            Rect lastRect;

            public ToolbarWindow(EntityWindow window)
            {
                m_MainWindow = window;

                m_FileMenu = new GenericMenu();
                m_FileMenu.AddItem(new GUIContent("Save Ctrl+S"), false, SaveMenu);
                m_FileMenu.AddItem(new GUIContent("Load Ctrl+R"), false, LoadMenu);
                m_FileMenu.AddSeparator(string.Empty);
                m_FileMenu.AddItem(new GUIContent("Add/Entity"), false, AddDataMenu<EntityDataBase>);
                m_FileMenu.AddItem(new GUIContent("Add/Attribute"), false, AddDataMenu<AttributeBase>);
                m_FileMenu.AddItem(new GUIContent("Add/Action"), false, AddDataMenu<ActionBase>);
                m_FileMenu.AddItem(new GUIContent("Add/Data"), false, AddDataMenu<DataObjectBase>);
            }
            private void SaveMenu()
            {
                if (!IsDataLoaded) return;

                m_MainWindow.SaveChanges();
            }
            private void LoadMenu()
            {
                EntityDataList.Instance.LoadData();
                m_MainWindow.Reload();
            }
            private void AddDataMenu<T>() where T : ObjectBase
            {
                if (!IsDataLoaded) LoadMenu();

                Type[] types = TypeHelper.GetTypes((other) => !other.IsAbstract && TypeHelper.TypeOf<T>.Type.IsAssignableFrom(other));

                PopupWindow.Show(lastRect, SelectorPopup<Type, Type>.GetWindow(types,
                    (t) =>
                    {
                        var drawer = m_MainWindow.Add(t);

                        m_MainWindow.Select(drawer);
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
                if (GUILayout.Button("Window", EditorStyles.toolbarDropDown))
                {
                    lastRect = GUILayoutUtility.GetLastRect();
                    lastRect.position = Event.current.mousePosition;

                    m_WindowMenu = new GenericMenu();
                    m_WindowMenu.AddItem(new GUIContent("Entity"), m_MainWindow.m_CurrentWindow == WindowType.Entity, () => m_MainWindow.m_CurrentWindow = WindowType.Entity);
                    m_WindowMenu.AddItem(new GUIContent("Converter"), m_MainWindow.m_CurrentWindow == WindowType.Converter, () => m_MainWindow.m_CurrentWindow = WindowType.Converter);

                    m_WindowMenu.ShowAsContext();
                    GUIUtility.ExitGUI();
                }
                GUILayout.FlexibleSpace();
            }
        }

        #region Entity Window

        public sealed class EntityDataListWindow
        {
            EntityWindow m_MainWindow;

            private EntityListTreeView EntityListTreeView;
            private TreeViewState TreeViewState;

            public string SearchString
            {
                get => EntityListTreeView.searchString;
                set => EntityListTreeView.searchString = value;
            }

            public EntityDataListWindow(EntityWindow window)
            {
                m_MainWindow = window;

                TreeViewState = new TreeViewState();
                EntityListTreeView = new EntityListTreeView(m_MainWindow, TreeViewState);
                EntityListTreeView.OnSelect += EntityListTreeView_OnSelect;
            }
            private void EntityListTreeView_OnSelect(ObjectBaseDrawer obj)
            {
                m_MainWindow.m_SelectedObject = obj;
            }

            public void Select(IReference reference)
            {
                var obj = reference.GetObject();
                if (obj == null) return;

                var iter = EntityListTreeView.GetRows().Where((other) => (other is EntityListTreeView.ObjectTreeElement objEle) && objEle.Target.m_TargetObject.Equals(obj));

                if (!iter.Any()) return;

                EntityListTreeView.SetSelection(new int[] { iter.First().id });
            }
            public void Select(ObjectBaseDrawer drawer)
            {
                var folder =  EntityListTreeView.GetFolder(drawer.Type);
                var iter = folder.children.Where((other) => (other is EntityListTreeView.ObjectTreeElement ele) && ele.Target.Equals(drawer));

                if (!iter.Any())
                {
                    "in".ToLog();
                    return;
                }

                var ele = iter.First();
                EntityListTreeView.SetExpanded(ele.parent.id, true);
                EntityListTreeView.FrameItem(ele.id);
                EntityListTreeView.SetSelection(new int[] { ele.id });
            }
            public void Add(ObjectBaseDrawer drawer)
            {
                EntityListTreeView.AddItem(drawer);
                EntityListTreeView.Reload();
            }
            public void Remove(ObjectBaseDrawer drawer)
            {
                EntityListTreeView.RemoveItem(drawer);
                EntityListTreeView.Reload();
            }
            public void Reload()
            {
                //if (Drawers.Count == 0) return;

                EntityListTreeView.Reload();
            }

            public void OnGUI(Rect pos, int unusedID)
            {
                if (m_MainWindow.m_CurrentWindow != WindowType.Entity)
                {
                    Color color = Color.gray;
                    color.a = .25f;

                    EditorGUI.DrawRect(pos, color);
                    EditorGUI.LabelField(pos, "Disabled");
                    return;
                }

                EntityListTreeView.OnGUI(pos);
            }
        }
        public sealed class EntityViewWindow
        {
            EntityWindow m_MainWindow;
            Rect m_Position;
            Vector2 m_Scroll;

            public EntityViewWindow(EntityWindow window)
            {
                m_MainWindow = window;
            }

            public void OnGUI(Rect pos, int unusedID)
            {
                m_Position = pos;

                Color origin = GUI.color;
                GUI.color = ColorPalettes.PastelDreams.Yellow;
                GUILayout.Window(unusedID, m_Position, Draw, string.Empty, EditorUtils.Box);
                GUI.color = origin;
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

                using (new EditorUtils.BoxBlock(ColorPalettes.PastelDreams.Yellow, GUILayout.Width(m_Position.width - 15)))
                {
                    if (m_MainWindow.m_SelectedObject != null)
                    {
                        EditorGUI.BeginChangeCheck();
                        m_MainWindow.m_SelectedObject.OnGUI();
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_MainWindow.IsDirty = true;
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("select object");
                    }
                }

                EditorGUILayout.EndScrollView();
            }
        }

        #endregion

        public sealed class ConverterListWindow
        {
            EntityWindow m_MainWindow;

            public ConverterListWindow(EntityWindow window)
            {
                m_MainWindow = window;
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
