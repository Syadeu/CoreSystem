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
                //if (IsDirty) return "Entity Window*";
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

        public ToolbarWindow m_ToolbarWindow;
        private EntityWindowMenuItem[] m_MenuItems;
        private int m_CurrentWindowIndex = 0;

        public static bool IsOpened { get; private set; }
        public static bool IsDataLoaded => EntityDataList.IsLoaded;
        public EntityWindowMenuItem CurrentWindow
        {
            get => m_MenuItems[m_CurrentWindowIndex];
            set
            {
                m_CurrentWindowIndex = Array.IndexOf(m_MenuItems, value);
                //m_CurrentWindow = value;
            }
        }
        public bool IsFocused { get; private set; } = false;

        private void OnFocus()
        {
            IsFocused = true;

            for (int i = 0; i < m_MenuItems?.Length; i++)
            {
                m_MenuItems[i].OnFocus();
            }
        }
        private void OnLostFocus()
        {
            IsFocused = false;

            for (int i = 0; i < m_MenuItems.Length; i++)
            {
                m_MenuItems[i].OnLostFocus();
            }
        }
        private void Awake()
        {
            var menuItemTypes = TypeHelper.GetTypesIter(t => !t.IsAbstract && !t.IsInterface && TypeHelper.TypeOf<EntityWindowMenuItem>.Type.IsAssignableFrom(t));
            m_MenuItems = new EntityWindowMenuItem[menuItemTypes.Count()];
            {
                int i = 0;
                foreach (var item in menuItemTypes)
                {
                    m_MenuItems[i] = (EntityWindowMenuItem)Activator.CreateInstance(item);

                    i++;
                }
                for (int j = 0; j < m_MenuItems.Length; j++)
                {
                    m_MenuItems[j].Initialize(this);
                }
            }
        }
        private void Update()
        {
            Repaint();
        }
        protected override void OnEnable()
        {
            IsOpened = true;

            m_ToolbarWindow = new ToolbarWindow(this);
            if (m_MenuItems == null)
            {
                var menuItemTypes = TypeHelper.GetTypesIter(t => !t.IsAbstract && !t.IsInterface && TypeHelper.TypeOf<EntityWindowMenuItem>.Type.IsAssignableFrom(t));
                m_MenuItems = new EntityWindowMenuItem[menuItemTypes.Count()];
                {
                    int i = 0;
                    foreach (var item in menuItemTypes)
                    {
                        m_MenuItems[i] = (EntityWindowMenuItem)Activator.CreateInstance(item);

                        i++;
                    }
                    for (int j = 0; j < m_MenuItems.Length; j++)
                    {
                        m_MenuItems[j].Initialize(this);
                    }
                }
            }

            for (int i = 0; i < m_MenuItems.Length; i++)
            {
                m_MenuItems[i].OnEnable();
            }

            Reload();

            base.OnEnable();
        }
        protected override void OnDisable()
        {
            IsOpened = false;

            for (int i = 0; i < m_MenuItems.Length; i++)
            {
                m_MenuItems[i].OnDisable();
            }

            base.OnDisable();
        }
        private void OnSelectionChange()
        {
            GameObject[] selections = Selection.gameObjects;

            for (int i = 0; i < m_MenuItems.Length; i++)
            {
                m_MenuItems[i].OnSelectionChanged(selections);
            }
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

            for (int i = 0; i < m_MenuItems.Length; i++)
            {
                ((IDisposable)m_MenuItems[i]).Dispose();
            }
            m_MenuItems = null;
        }
        public TMenuItem GetMenuItem<TMenuItem>() where TMenuItem : EntityWindowMenuItem
        {
            for (int i = 0; i < m_MenuItems.Length; i++)
            {
                if (m_MenuItems[i] is TMenuItem menu) return menu;
            }
            return null;
        }

        public void Reload()
        {
            if (!Application.isPlaying)
            {
                if (!IsDataLoaded) EntityDataList.Instance.LoadData();
            }

            ObjectBaseDrawer.Pool.Clear();
            GetMenuItem<EntityDataWindow>().Reload();
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

            GetMenuItem<EntityDataWindow>().Add(ins);

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

            GetMenuItem<EntityDataWindow>().Add(ins);

            IsDirty = true;
        }

        public void Remove(ObjectBase obj)
        {
            EntityDataWindow dataWindow = GetMenuItem<EntityDataWindow>();

            if (dataWindow.Selected != null &&
                dataWindow.Selected.Equals(obj)) dataWindow.Selected = null;

            dataWindow.Remove(obj);

            EntityDataList.Instance.m_Objects.Remove(obj.Hash);

            IsDirty = true;
        }

        private const string c_CopyrightText = "Copyright 2022 Syadeu. All rights reserved.";
        private Rect m_CopyrightRect = new Rect(350, 485, 245, 20);

        Rect HeaderPos = new Rect(20, 33, 0, 0);
        Rect HeaderLinePos = new Rect(0, 60, 0, 0);

        Rect EntityListPos = new Rect(6, 60, 260, 430);
        Rect ViewPos = new Rect(265, 60, 687, 430);

        private void OnGUI()
        {
            EditorStyles.textField.wordWrap = true;

            m_ToolbarWindow.OnGUI();

            string headerString = EditorUtilities.String($"{CurrentWindow.Name} Window", 20);
            if (IsDirty)
            {
                headerString += EditorUtilities.String(": Modified", 10);
            }

            EditorGUI.LabelField(HeaderPos,
                headerString,
                EditorStyleUtilities.HeaderStyle);
            HeaderLinePos.width = Screen.width;
            EditorUtilities.Line(HeaderLinePos);

            if (Application.isPlaying && !(CurrentWindow is EntityDebugWindow))
            {
                CurrentWindow = GetMenuItem<EntityDebugWindow>();
                //m_CurrentWindow = WindowType.Debugger;
            }

            EntityListPos.height = Screen.height - 95;
            ViewPos.width = Screen.width - EntityListPos.width - 5;
            ViewPos.height = EntityListPos.height;

            using (new WindowHelper(BeginWindows, EndWindows))
            {
                m_MenuItems[m_CurrentWindowIndex].OnListGUI(EntityListPos);
                m_MenuItems[m_CurrentWindowIndex].OnViewGUI(ViewPos);

                //switch (m_CurrentWindow)
                //{
                //    default:
                //    case WindowType.Entity:
                //        if (!Application.isPlaying)
                //        {
                //            //m_DataListWindow.OnGUI(EntityListPos, 1);
                //            //m_ViewWindow.OnGUI(ViewPos, 2);
                //            m_MenuItems[0].OnListGUI(EntityListPos);
                //            m_MenuItems[0].OnViewGUI(ViewPos);
                //        }

                //        break;
                //    case WindowType.Converter:

                //        break;
                //    case WindowType.Debugger:
                //        m_DebuggerListWindow.OnGUI(EntityListPos, 1);
                //        m_DebuggerViewWindow.OnGUI(ViewPos, 2);
                //        break;
                //}
            }

            m_CopyrightRect.width = Screen.width;
            m_CopyrightRect.x = 0;
            m_CopyrightRect.y = Screen.height - 42;
            EditorGUI.LabelField(m_CopyrightRect, EditorUtilities.String(c_CopyrightText, 11), EditorStyleUtilities.CenterStyle);

            KeyboardShortcuts();
        }
        private void KeyboardShortcuts()
        {
            if (!Event.current.isKey || Application.isPlaying) return;

            if (CurrentWindow is EntityDataWindow && Event.current.control)
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

                        m_MainWindow.GetMenuItem<EntityDataWindow>().Select(drawer);
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
                    if (!Application.isPlaying && m_MainWindow.CurrentWindow is EntityDataWindow)
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
                    if (!Application.isPlaying && m_MainWindow.CurrentWindow is EntityDataWindow)
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

                    for (int i = 0; i < m_MainWindow.m_MenuItems.Length; i++)
                    {
                        int index = i;
                        m_WindowMenu.AddItem(
                            new GUIContent(m_MainWindow.m_MenuItems[index].Name),
                            m_MainWindow.m_CurrentWindowIndex == index,
                            () => m_MainWindow.CurrentWindow = m_MainWindow.m_MenuItems[index]
                            );
                    }

                    m_WindowMenu.ShowAsContext();
                    GUIUtility.ExitGUI();
                }
                GUILayout.FlexibleSpace();
            }
        }

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
    }
}
