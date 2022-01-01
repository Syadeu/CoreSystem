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
using SyadeuEditor.Utilities;
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

        public EntityDataListWindow m_DataListWindow;
        public EntityViewWindow m_ViewWindow;

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

            ObjectBaseDrawer.Pool.Clear();
            m_DataListWindow.Reload();
            CoreSystem.Logger.Log(Channel.Editor, "Entity data loaded");
        }
        public ObjectBase Add(Type objType)
        {
            if (!EntityDataList.IsLoaded)
            {
                EntityDataList.Instance.LoadData();
            }

            ObjectBase ins = (ObjectBase)Activator.CreateInstance(objType);
            EntityDataList.Instance.m_Objects.Add(ins.Hash, ins);

            m_DataListWindow.Add(ins);

            IsDirty = true;
            return ins;
        }
        public void Add(ObjectBase ins)
        {
            if (!EntityDataList.IsLoaded)
            {
                EntityDataList.Instance.LoadData();
            }

            EntityDataList.Instance.m_Objects.Add(ins.Hash, ins);

            m_DataListWindow.Add(ins);

            IsDirty = true;
        }

        public void Remove(ObjectBase obj)
        {
            if (m_DataListWindow.Selected != null && 
                m_DataListWindow.Selected.Equals(obj)) m_DataListWindow.Selected = null;

            //ObjectBaseDrawers.Remove(obj);
            m_DataListWindow.Remove(obj);

            EntityDataList.Instance.m_Objects.Remove(obj.Hash);

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

            string headerString = EditorUtilities.String($"{m_CurrentWindow} Window", 20);
            if (IsDirty)
            {
                headerString += EditorUtilities.String(": Modified", 10);
            }

            EditorGUI.LabelField(HeaderPos,
                headerString,
                EditorStyleUtilities.HeaderStyle);
            HeaderLinePos.width = Screen.width;
            EditorUtilities.Line(HeaderLinePos);

            if (Application.isPlaying && m_CurrentWindow != WindowType.Debugger)
            {
                m_CurrentWindow = WindowType.Debugger;
            }

            EntityListPos.height = Screen.height - 95;
            ViewPos.width = Screen.width - EntityListPos.width - 5;
            ViewPos.height = EntityListPos.height;

            using (new WindowHelper(BeginWindows, EndWindows))
            {
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
            }
            //BeginWindows();
            
            

            //EndWindows();

            m_CopyrightRect.width = Screen.width;
            m_CopyrightRect.x = 0;
            m_CopyrightRect.y = Screen.height - 42;
            EditorGUI.LabelField(m_CopyrightRect, EditorUtilities.String(c_CopyrightText, 11), EditorStyleUtilities.CenterStyle);

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
        
        private sealed class WindowHelper : IDisposable
        {
            public Action StartWindow, EndWindow;

            public WindowHelper(Action start, Action end)
            {
                StartWindow = start;
                EndWindow = end;

                StartWindow.Invoke();
            }
            public void Dispose()
            {
                EndWindow.Invoke();

                StartWindow = null;
                EndWindow = null;
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
                        ObjectBase drawer = m_MainWindow.Add(t);

                        m_MainWindow.m_DataListWindow.Select(drawer);
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
                using (new EditorUtilities.BoxBlock(Color.black))
                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawTools();
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
            private ObjectBase m_Selected;

            public string SearchString
            {
                get => EntityListTreeView.searchString;
                set => EntityListTreeView.searchString = value;
            }
            public ObjectBase Selected
            {
                get => m_Selected;
                set
                {
                    m_Selected = value;
                    if (value != null)
                    {
                        SelectedDrawer = ObjectBaseDrawer.GetDrawer(value);
                    }
                    else SelectedDrawer = null;
                }
            }
            public ObjectBaseDrawer SelectedDrawer { get; private set; }

            public EntityDataListWindow(EntityWindow window)
            {
                m_MainWindow = window;

                TreeViewState = new TreeViewState();
                EntityListTreeView = new EntityListTreeView(m_MainWindow, TreeViewState);
                EntityListTreeView.OnSelect += EntityListTreeView_OnSelect;
            }
            private void EntityListTreeView_OnSelect(ObjectBase obj)
            {
                Selected = obj;
            }

            public void Select(IFixedReference reference)
            {
                EntityListTreeView.SetSelection(reference);
            }
            public void Select(ObjectBase entityObj)
            {
                EntityListTreeView.SetSelection(entityObj);
            }
            public void Add(ObjectBase drawer)
            {
                EntityListTreeView.AddItem(drawer);
                EntityListTreeView.Reload();
            }
            public void Remove(ObjectBase drawer)
            {
                if (Selected != null && Selected.Equals(drawer))
                {
                    Selected = null;
                }

                EntityListTreeView.RemoveItem(drawer);
                EntityListTreeView.Reload();
            }
            public void Reload()
            {
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
                GUILayout.Window(unusedID, m_Position, Draw, string.Empty, EditorStyleUtilities.Box);
                GUI.color = origin;
            }
            private void Draw(int unusedID)
            {
                using (var scroll = new EditorGUILayout.ScrollViewScope(m_Scroll, true, true,
                    GUILayout.MaxWidth(m_Position.width), GUILayout.MaxHeight(m_Position.height)))
                {
                    m_Scroll = scroll.scrollPosition;

                    #region TestRect Controller

                    //m_MainWindow.m_CopyrightRect = EditorGUILayout.RectField("copyright", m_MainWindow.m_CopyrightRect);
                    //m_MainWindow.HeaderPos = EditorGUILayout.RectField("headerPos", m_MainWindow. HeaderPos);
                    //m_MainWindow.HeaderLinePos = EditorGUILayout.RectField("HeaderLinePos", m_MainWindow.HeaderLinePos);
                    //m_MainWindow.EntityListPos = EditorGUILayout.RectField("entitylistPos", m_MainWindow. EntityListPos);

                    //m_MainWindow.ViewPos = EditorGUILayout.RectField("ViewPos", m_MainWindow.ViewPos);
                    //EditorGUILayout.Space();

                    #endregion

                    using (new EditorUtilities.BoxBlock(ColorPalettes.PastelDreams.Yellow, GUILayout.Width(m_Position.width - 15)))
                    using (var change = new EditorGUI.ChangeCheckScope())
                    {
                        if (m_MainWindow.m_DataListWindow.Selected != null)
                        {
                            m_MainWindow.m_DataListWindow.SelectedDrawer.OnGUI();
                        }
                        else
                        {
                            EditorGUILayout.LabelField("select object");
                        }

                        if (change.changed) m_MainWindow.IsDirty = true;
                    }
                }
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

            private Entity<ObjectBase> m_Selected;
            private string m_SelectedName = string.Empty;
            private ObjectDrawerBase[] m_SelectedMembers = null;

            public Entity<ObjectBase> Selected
            {
                get => m_Selected;
                set
                {
                    if (value.IsEmpty() || !value.IsValid())
                    {
                        $"1: {value.IsEmpty()} :: {value.IsValid()}".ToLog();
                        m_Selected = Entity<ObjectBase>.Empty;
                        m_SelectedName = string.Empty;
                        m_SelectedMembers = null;
                        return;
                    }

                    var entity = value.Target;
                    m_SelectedName = entity.Name + EditorUtilities.String($": {entity.GetType().Name}", 11);

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
                GUILayout.Window(unusedID, m_Position, Draw, string.Empty, EditorStyleUtilities.Box);
                GUI.color = origin;
            }
            private void Draw(int unusedID)
            {
                using (var scroll = new EditorGUILayout.ScrollViewScope(m_Scroll, true, true,
                    GUILayout.MaxWidth(m_Position.width), GUILayout.MaxHeight(m_Position.height)))
                using (new EditorUtilities.BoxBlock(Color.black))
                {
                    if (!Application.isPlaying)
                    {
                        EditorUtilities.StringRich("Debugger only works in runtime", true);
                        return;
                    }

                    if (m_Selected.IsEmpty())
                    {
                        EditorUtilities.StringRich("Select Data", true);
                        return;
                    }

                    if (!m_Selected.IsValid())
                    {
                        EditorUtilities.StringRich("This data has been destroyed", true);
                        return;
                    }

                    ObjectBase obj = m_Selected.Target;

                    EditorUtilities.StringRich(m_SelectedName, 20);
                    EditorGUILayout.Space(3);
                    EditorUtilities.Line();

                    DrawDefaultInfomation(obj);

                    if (obj is EntityDataBase entityDataBase)
                    {
                        DrawEntity(entityDataBase);
                    }

                    EditorUtilities.Line();

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
                using (new EditorGUI.DisabledGroupScope(true))
                {
                    EditorGUILayout.TextField("Name: ", obj.Name);
                    EditorGUILayout.TextField("Hash: ", obj.Hash.ToString());
                    EditorGUILayout.TextField("Idx: ", obj.Idx.ToString());
                }
            }
            private void DrawEntity(EntityDataBase entity)
            {
                if (entity is EntityBase entityBase)
                {
                    ProxyTransform proxy = entityBase.GetTransform();
                    using (new EditorUtilities.BoxBlock(ColorPalettes.WaterFoam.Teal))
                    {
                        EntityDrawer.DrawPrefab(entityBase, true);

                        if (proxy.hasProxy)
                        {
                            EditorGUILayout.ObjectField((UnityEngine.Object)proxy.proxy, TypeHelper.TypeOf<RecycleableMonobehaviour>.Type, true);
                        }

                        entityBase.Center
                            = EditorGUILayout.Vector3Field("Center", entityBase.Center);
                        entityBase.Size
                            = EditorGUILayout.Vector3Field("Size", entityBase.Size);
                    }
                    EditorUtilities.Line();
                    using (new EditorUtilities.BoxBlock(ColorPalettes.WaterFoam.Teal))
                    {
                        EditorUtilities.StringRich("Transform", 15);
                        EditorGUI.indentLevel++;

                        proxy.position =
                            EditorGUILayout.Vector3Field("Position", proxy.position);

                        Vector3 eulerAngles = proxy.eulerAngles;

                        using (var change = new EditorGUI.ChangeCheckScope())
                        {
                            eulerAngles = EditorGUILayout.Vector3Field("Rotation", eulerAngles);
                            if (change.changed)
                            {
                                proxy.eulerAngles = eulerAngles;
                            }
                        }

                        proxy.scale
                            = EditorGUILayout.Vector3Field("Scale", proxy.scale);

                        EditorGUI.indentLevel--;
                    }
                }
            }
        }

        #endregion
    }
}
