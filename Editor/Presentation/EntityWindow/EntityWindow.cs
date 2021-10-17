using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;
using Syadeu;
using Syadeu.Collections;
using Syadeu.Internal;
using Syadeu.Presentation;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Data;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Proxy;
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

        public bool IsDirty
        {
            get => hasUnsavedChanges;
            set
            {
                if (!Application.isPlaying)
                {
                    saveChangesMessage = "Unsaved changes detected";
                    hasUnsavedChanges = value;
                }
            }
        }

        private WindowType m_CurrentWindow = WindowType.Entity;

        public ToolbarWindow m_ToolbarWindow;

        #region Entity Window

        readonly List<ObjectBaseDrawer> ObjectBaseDrawers = new List<ObjectBaseDrawer>();
        public EntityDataListWindow m_DataListWindow;
        public EntityViewWindow m_ViewWindow;
        public ObjectBaseDrawer m_SelectedObject = null;

        #endregion

        public DebuggerListWindow m_DebuggerListWindow;
        public DebuggerViewWindow m_DebuggerViewWindow;

        public static bool IsOpened { get; private set; }
        public static bool IsDataLoaded => EntityDataList.IsLoaded;
        public WindowType CurrentWindow
        {
            get => m_CurrentWindow;
            set
            {
                m_CurrentWindow = value;
            }
        }
        public bool IsFocused { get; private set; } = false;

        private void OnFocus()
        {
            IsFocused = true;
        }
        private void OnLostFocus()
        {
            IsFocused = false;
        }
        protected override void OnEnable()
        {
            IsOpened = true;

            m_ToolbarWindow = new ToolbarWindow(this);
            m_DataListWindow = new EntityDataListWindow(this);
            m_ViewWindow = new EntityViewWindow(this);

            m_DebuggerListWindow = new DebuggerListWindow(this);
            m_DebuggerViewWindow = new DebuggerViewWindow(this);

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
            if (Application.isPlaying) return;

            CoreSystem.Logger.Log(Channel.Editor, "Entity data saved");

            EntityDataList.Instance.SaveData();
            base.SaveChanges();

            IsDirty = false;
            Repaint();
        }
        private void OnDestroy()
        {
            if (!Application.isPlaying)
            {
                if (IsDirty) EntityDataList.Instance.LoadData();
            }
        }
        public void Reload()
        {
            if (!Application.isPlaying)
            {
                if (!IsDataLoaded) EntityDataList.Instance.LoadData();
            }

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

        public void Select(IFixedReference reference)
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

            if (Application.isPlaying && m_CurrentWindow != WindowType.Debugger)
            {
                m_CurrentWindow = WindowType.Debugger;
            }

            EntityListPos.height = Screen.height - 95;
            ViewPos.width = Screen.width - EntityListPos.width - 5;
            ViewPos.height = EntityListPos.height;
            BeginWindows();

            switch (m_CurrentWindow)
            {
                default:
                case WindowType.Entity:
                    if (!Application.isPlaying)
                    {
                        m_DataListWindow.OnGUI(EntityListPos, 1);
                        m_ViewWindow.OnGUI(ViewPos, 2);
                    }
                    
                    break;
                case WindowType.Converter:

                    break;
                case WindowType.Debugger:
                    m_DebuggerListWindow.OnGUI(EntityListPos, 1);
                    m_DebuggerViewWindow.OnGUI(ViewPos, 2);
                    break;
            }

            EndWindows();

            m_CopyrightRect.width = Screen.width;
            m_CopyrightRect.x = 0;
            m_CopyrightRect.y = Screen.height - 42;
            EditorGUI.LabelField(m_CopyrightRect, EditorUtils.String(c_CopyrightText, 11), EditorUtils.CenterStyle);

            KeyboardShortcuts();
        }
        private void KeyboardShortcuts()
        {
            if (!Event.current.isKey || Application.isPlaying) return;

            if (m_CurrentWindow == WindowType.Entity && Event.current.control)
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
            Debugger,
        }

        public sealed class ToolbarWindow
        {
            EntityWindow m_MainWindow;

            GenericMenu 
                m_WindowMenu;

            Rect lastRect;

            public ToolbarWindow(EntityWindow window)
            {
                m_MainWindow = window;
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

                    var fileMenu = new GenericMenu();
                    if (!Application.isPlaying && m_MainWindow.CurrentWindow == WindowType.Entity)
                    {
                        fileMenu.AddItem(new GUIContent("Save Ctrl+S"), false, SaveMenu);
                        fileMenu.AddItem(new GUIContent("Load Ctrl+R"), false, LoadMenu);
                    }
                    else
                    {
                        fileMenu.AddDisabledItem(new GUIContent("Save Ctrl+S"), false);
                        fileMenu.AddDisabledItem(new GUIContent("Load Ctrl+R"), false);
                    }
                    fileMenu.AddSeparator(string.Empty);
                    if (!Application.isPlaying && m_MainWindow.CurrentWindow == WindowType.Entity)
                    {
                        fileMenu.AddItem(new GUIContent("Add/Entity"), false, AddDataMenu<EntityDataBase>);
                        fileMenu.AddItem(new GUIContent("Add/Attribute"), false, AddDataMenu<AttributeBase>);
                        fileMenu.AddItem(new GUIContent("Add/Action"), false, AddDataMenu<ActionBase>);
                        fileMenu.AddItem(new GUIContent("Add/Data"), false, AddDataMenu<DataObjectBase>);
                    }
                    else
                    {
                        fileMenu.AddDisabledItem(new GUIContent("Add/Entity"), false);
                        fileMenu.AddDisabledItem(new GUIContent("Add/Attribute"), false);
                        fileMenu.AddDisabledItem(new GUIContent("Add/Action"), false);
                        fileMenu.AddDisabledItem(new GUIContent("Add/Data"), false);
                    }

                    fileMenu.ShowAsContext();
                    GUIUtility.ExitGUI();
                }
                if (GUILayout.Button("Window", EditorStyles.toolbarDropDown))
                {
                    lastRect = GUILayoutUtility.GetLastRect();
                    lastRect.position = Event.current.mousePosition;

                    m_WindowMenu = new GenericMenu();

                    if (!Application.isPlaying)
                    {
                        m_WindowMenu.AddItem(new GUIContent("Entity"), m_MainWindow.m_CurrentWindow == WindowType.Entity, () => m_MainWindow.m_CurrentWindow = WindowType.Entity);

                        m_WindowMenu.AddItem(new GUIContent("Converter"), m_MainWindow.m_CurrentWindow == WindowType.Converter, () => m_MainWindow.m_CurrentWindow = WindowType.Converter);
                    }
                    else
                    {
                        m_WindowMenu.AddDisabledItem(new GUIContent("Entity"), m_MainWindow.m_CurrentWindow == WindowType.Entity);

                        m_WindowMenu.AddDisabledItem(new GUIContent("Converter"), m_MainWindow.m_CurrentWindow == WindowType.Converter);
                    }

                    m_WindowMenu.AddItem(new GUIContent("Debugger"), m_MainWindow.m_CurrentWindow == WindowType.Debugger, () => m_MainWindow.m_CurrentWindow = WindowType.Debugger);

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

            public void Select(IFixedReference reference)
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

            public void OnGUI(Rect pos, int unusedID)
            {

            }
        }

        #region Debugger

        public sealed class DebuggerListWindow
        {
            EntityWindow m_MainWindow;

            private DebuggerListTreeView ListTreeView;
            private TreeViewState TreeViewState;

            public DebuggerListWindow(EntityWindow window)
            {
                m_MainWindow = window;

                TreeViewState = new TreeViewState();
                ListTreeView = new DebuggerListTreeView(m_MainWindow, TreeViewState);
            }

            public void OnGUI(Rect pos, int unusedID)
            {
                ListTreeView.OnGUI(pos);
            }

            public void Select(IInstance instance)
            {
                ListTreeView.Select(instance);
            }
        }
        public sealed class DebuggerViewWindow
        {
            EntityWindow m_MainWindow;
            Rect m_Position;
            Vector2 m_Scroll;

            private Instance<ObjectBase> m_Selected;
            private string m_SelectedName = string.Empty;
            private ObjectDrawerBase[] m_SelectedMembers = null;

            public Instance<ObjectBase> Selected
            {
                get => m_Selected;
                set
                {
                    if (value.IsEmpty() || !value.IsValid())
                    {
                        $"1: {value.IsEmpty()} :: {value.IsValid()}".ToLog();
                        m_Selected = Instance<ObjectBase>.Empty;
                        m_SelectedName = string.Empty;
                        m_SelectedMembers = null;
                        return;
                    }

                    var entity = value.GetObject();
                    m_SelectedName = entity.Name + EditorUtils.String($": {entity.GetType().Name}", 11);

                    MemberInfo[] temp = entity.GetType()
                        .GetMembers(
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Where((other) =>
                        {
                            if (other.MemberType != MemberTypes.Field && 
                                other.MemberType != MemberTypes.Property) return false;

                            if (other.GetCustomAttribute<ObsoleteAttribute>() != null)
                            {
                                return false;
                            }

                            Type declaredType = ReflectionHelper.GetDeclaredType(other);

                            if (TypeHelper.TypeOf<Delegate>.Type.IsAssignableFrom(declaredType) ||
                                TypeHelper.TypeOf<IFixedReference>.Type.IsAssignableFrom(declaredType))
                            {
                                return false;
                            }

                            if (ReflectionHelper.IsBackingField(other)) return false;

                            return true;
                        })
                        .ToArray();
                    m_SelectedMembers = new ObjectDrawerBase[temp.Length];
                    for (int i = 0; i < temp.Length; i++)
                    {
                        m_SelectedMembers[i] = ObjectDrawerBase.ToDrawer(entity, temp[i], true);
                    }

                    m_Selected = value;
                }
            }

            public DebuggerViewWindow(EntityWindow window)
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
                using (var scroll = new EditorGUILayout.ScrollViewScope(m_Scroll, true, true,
                    GUILayout.MaxWidth(m_Position.width), GUILayout.MaxHeight(m_Position.height)))
                using (new EditorUtils.BoxBlock(Color.black))
                {
                    if (!Application.isPlaying)
                    {
                        EditorUtils.StringRich("Debugger only works in runtime", true);
                        return;
                    }

                    if (m_Selected.IsEmpty())
                    {
                        EditorUtils.StringRich("Select Data", true);
                        return;
                    }

                    if (!m_Selected.IsValid())
                    {
                        EditorUtils.StringRich("This data has been destroyed", true);
                        return;
                    }

                    ObjectBase obj = m_Selected.GetObject();

                    EditorUtils.StringRich(m_SelectedName, 20);
                    EditorGUILayout.Space(3);
                    EditorUtils.Line();

                    DrawDefaultInfomation(obj);

                    if (obj is EntityDataBase entityDataBase)
                    {
                        DrawEntity(entityDataBase);
                    }

                    EditorUtils.Line();

                    for (int i = 0; i < m_SelectedMembers.Length; i++)
                    {
                        if (m_SelectedMembers[i] is AttributeListDrawer ||
                            m_SelectedMembers[i].Name.Equals("Name") ||
                            m_SelectedMembers[i].Name.Equals("Hash") ||
                            m_SelectedMembers[i].Name.Equals("Idx") ||
                            m_SelectedMembers[i].Name.Equals("EnableCull") ||
                            m_SelectedMembers[i].Name.Equals("Prefab") ||
                            m_SelectedMembers[i].Name.Equals("Center") ||
                            m_SelectedMembers[i].Name.Equals("Size") ||
                            m_SelectedMembers[i].Name.Equals("transform"))
                        {
                            continue;
                        }
                        else if (m_SelectedMembers[i] is ArrayDrawer array)
                        {
                            if (TypeHelper.TypeOf<IFixedReference>.Type.IsAssignableFrom(array.ElementType)) continue;
                        }

                        m_SelectedMembers[i].OnGUI();
                    }

                    m_Scroll = scroll.scrollPosition;
                }
            }
            private void DrawDefaultInfomation(ObjectBase obj)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("Name: ", obj.Name);
                EditorGUILayout.TextField("Hash: ", obj.Hash.ToString());
                EditorGUILayout.TextField("Idx: ", obj.Idx.ToString());
                EditorGUI.EndDisabledGroup();
            }
            private void DrawEntity(EntityDataBase entity)
            {
                if (entity is EntityBase entityBase)
                {
                    using (new EditorUtils.BoxBlock(ColorPalettes.WaterFoam.Teal))
                    {
                        EntityDrawer.DrawPrefab(entityBase, true);

                        if (entityBase.transform is ProxyTransform proxy &&
                            proxy.hasProxy)
                        {
                            EditorGUILayout.ObjectField((UnityEngine.Object)proxy.proxy, TypeHelper.TypeOf<RecycleableMonobehaviour>.Type, true);
                        }
                        else if (entityBase.transform is UnityTransform unityTr)
                        {
                            EditorGUILayout.ObjectField(unityTr.provider, TypeHelper.TypeOf<Transform>.Type, true);
                        }

                        entityBase.Center
                            = EditorGUILayout.Vector3Field("Center", entityBase.Center);
                        entityBase.Size
                            = EditorGUILayout.Vector3Field("Size", entityBase.Size);
                    }
                    EditorUtils.Line();
                    using (new EditorUtils.BoxBlock(ColorPalettes.WaterFoam.Teal))
                    {
                        EditorUtils.StringRich("Transform", 15);
                        EditorGUI.indentLevel++;

                        entityBase.transform.position =
                EditorGUILayout.Vector3Field("Position", entityBase.transform.position);

                        Vector3 eulerAngles = entityBase.transform.eulerAngles;
                        EditorGUI.BeginChangeCheck();
                        eulerAngles = EditorGUILayout.Vector3Field("Rotation", eulerAngles);
                        if (EditorGUI.EndChangeCheck())
                        {
                            entityBase.transform.eulerAngles = eulerAngles;
                        }

                        entityBase.transform.scale
                            = EditorGUILayout.Vector3Field("Scale", entityBase.transform.scale);

                        EditorGUI.indentLevel--;
                    }
                }
            }
        }

        #endregion
    }
}
