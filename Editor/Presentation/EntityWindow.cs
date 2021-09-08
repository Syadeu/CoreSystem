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
        protected override string DisplayName => "Entity Window";

        readonly List<ObjectBaseDrawer> ObjectBaseDrawers = new List<ObjectBaseDrawer>();

        public ToolbarWindow m_ToolbarWindow;
        public DataListWindow m_DataListWindow;
        public ViewWindow m_ViewWindow;

        public ObjectBaseDrawer m_SelectedObject = null;

        public static bool IsOpened { get; private set; }
        public static bool IsDataLoaded => EntityDataList.IsLoaded;

        protected override void OnEnable()
        {
            IsOpened = true;

            m_ToolbarWindow = new ToolbarWindow(this);
            m_DataListWindow = new DataListWindow(this);
            m_ViewWindow = new ViewWindow(this);

            if (!IsDataLoaded)
            {
                EntityDataList.Instance.LoadData();
            }
            Reload();

            base.OnEnable();
        }
        protected override void OnDisable()
        {
            IsOpened = false;

            base.OnDisable();
        }

        public void Reload()
        {
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
            return drawer;
        }
        public ObjectBaseDrawer Add(ObjectBase ins)
        {
            if (EntityDataList.Instance.m_Objects == null) EntityDataList.Instance.m_Objects = new Dictionary<Hash, ObjectBase>();

            EntityDataList.Instance.m_Objects.Add(ins.Hash, ins);

            ObjectBaseDrawer drawer = ObjectBaseDrawer.GetDrawer(ins);
            ObjectBaseDrawers.Add(drawer);
            m_DataListWindow.Add(drawer);
            return drawer;
        }

        public void Remove(ObjectBaseDrawer obj)
        {
            if (m_SelectedObject != null && m_SelectedObject.Equals(obj)) m_SelectedObject = null;

            ObjectBaseDrawers.Remove(obj);

            EntityDataList.Instance.m_Objects.Remove(obj.m_TargetObject.Hash);
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
            EditorGUI.LabelField(m_CopyrightRect, EditorUtils.String("Copyright 2021 Syadeu. All rights reserved.", 11), EditorUtils.CenterStyle);
        }
        
        public sealed class ToolbarWindow
        {
            EntityWindow m_MainWindow;
            GenericMenu m_FileMenu;

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
                if (!IsDataLoaded) return;

                EntityDataList.Instance.SaveData();
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
                        //if (EntityDataList.Instance.m_Objects == null) EntityDataList.Instance.m_Objects = new Dictionary<Hash, ObjectBase>();

                        //T ins = (T)Activator.CreateInstance(t);

                        //EntityDataList.Instance.m_Objects.Add(ins.Hash, ins);
                        //m_MainWindow.AddData(ins);
                        var drawer = m_MainWindow.Add(t);
                        m_MainWindow.m_DataListWindow.Add(drawer);

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
                GUILayout.FlexibleSpace();
            }
        }

        public sealed class DataListWindow
        {
            EntityWindow m_MainWindow;

            List<ObjectBaseDrawer> Drawers => m_MainWindow.ObjectBaseDrawers;

            private EntityListTreeView EntityListTreeView;
            private TreeViewState TreeViewState;

            public string SearchString
            {
                get => EntityListTreeView.searchString;
                set => EntityListTreeView.searchString = value;
            }

            public DataListWindow(EntityWindow window)
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
                if (Drawers.Count == 0) return;

                EntityListTreeView.Reload();
            }

            public void OnGUI(Rect pos, int unusedID)
            {
                EntityListTreeView.OnGUI(pos);
            }
        }
        public sealed class ViewWindow
        {
            EntityWindow m_MainWindow;
            Rect m_Position;
            Vector2 m_Scroll;

            //GUIStyle m_Style;

            public ViewWindow(EntityWindow window)
            {
                m_MainWindow = window;
                //m_Style = new GUIStyle();
                //m_Style.normal.background = new Texture2D(1, 1);
                //for (int i = 0; i < m_Style.normal.background.height; i++)
                //{
                //    for (int j = 0; j < m_Style.normal.background.width; j++)
                //    {
                //        m_Style.normal.background.SetPixel(i, j, ColorPalettes.PastelDreams.Yellow);
                //    }
                //}
                //m_Style.normal.background.Apply();
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
                        m_MainWindow.m_SelectedObject.OnGUI();
                    }
                    else
                    {
                        EditorGUILayout.LabelField("select object");
                    }
                }

                EditorGUILayout.EndScrollView();
            }
        }

        public sealed class EntityDrawer : ObjectBaseDrawer
        {
            public EntityDataBase Target => (EntityDataBase)m_TargetObject;

            GUIContent m_EnableCullName, m_DisableCullName;
            PrefabReferenceDrawer prefabReferenceDrawer = null;
            AttributeListDrawer attributeListDrawer;

            public EntityDrawer(ObjectBase objectBase) : base(objectBase)
            {
                m_EnableCullName = new GUIContent("Enable Cull");
                m_DisableCullName = new GUIContent("Disable Cull");

                if (objectBase is EntityBase)
                {
                    prefabReferenceDrawer = (PrefabReferenceDrawer)m_ObjectDrawers.Where((other) => other.Name.Equals("Prefab")).First();
                    prefabReferenceDrawer.DisableHeader = true;
                }
                
                attributeListDrawer = new AttributeListDrawer(objectBase,
                    TypeHelper.TypeOf<EntityDataBase>.Type.GetField("m_AttributeList", BindingFlags.NonPublic | BindingFlags.Instance));
            }

            protected override void DrawGUI()
            {
                EditorUtils.StringRich(Name + EditorUtils.String($": {Type.Name}", 11), 20);
                EditorGUILayout.Space(3);
                EditorUtils.Line();

                DrawDescription();

                Target.Name = EditorGUILayout.TextField("Name: ", Target.Name);
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("Hash: ", Target.Hash.ToString());
                EditorGUI.EndDisabledGroup();
                if (Target is EntityBase entity)
                {
                    using (new EditorUtils.BoxBlock(ColorPalettes.WaterFoam.Teal))
                    {
                        EditorUtils.StringRich("Prefab", 15);

                        GUIContent enableCullName = entity.m_EnableCull ? m_DisableCullName : m_EnableCullName;
                        Rect enableCullRect = GUILayoutUtility.GetRect(
                            enableCullName, 
                            EditorStyles.toolbarButton, GUILayout.ExpandWidth(true));
                        int enableCullID = GUIUtility.GetControlID(FocusType.Passive, enableCullRect);
                        switch (Event.current.GetTypeForControl(enableCullID))
                        {
                            case EventType.Repaint:
                                bool isHover = enableCullRect.Contains(Event.current.mousePosition);

                                Color origin = GUI.color;
                                GUI.color = entity.m_EnableCull ? ColorPalettes.PastelDreams.TiffanyBlue : ColorPalettes.PastelDreams.HotPink;

                                EditorStyles.toolbarButton.Draw(enableCullRect,
                                    isHover, isActive: true, on: true, false);
                                GUI.color = origin;

                                var temp = new GUIStyle(EditorStyles.label);
                                temp.alignment = TextAnchor.MiddleCenter;
                                temp.Draw(enableCullRect, enableCullName, enableCullID);
                                break;
                            case EventType.MouseDown:
                                if (!enableCullRect.Contains(Event.current.mousePosition)) break;

                                if (Event.current.button == 0)
                                {
                                    GUIUtility.hotControl = enableCullID;
                                    entity.m_EnableCull = !entity.m_EnableCull;
                                    Event.current.Use();
                                }

                                break;
                            case EventType.MouseUp:
                                if (GUIUtility.hotControl == enableCullID)
                                {
                                    GUIUtility.hotControl = 0;
                                }
                                break;
                            default:
                                break;
                        }

                        DrawField(prefabReferenceDrawer);
                    }
                }
                using (new EditorUtils.BoxBlock(Color.black))
                {
                    attributeListDrawer.OnGUI();
                }
                EditorUtils.Line();

                for (int i = 0; i < m_ObjectDrawers.Length; i++)
                {
                    if (m_ObjectDrawers[i] == null)
                    {
                        EditorGUILayout.LabelField("null");
                        continue;
                    }

                    if (m_ObjectDrawers[i].Name.Equals("Name") ||
                        m_ObjectDrawers[i].Name.Equals("Hash") ||
                        m_ObjectDrawers[i].Name.Equals("Prefab")
                        || m_ObjectDrawers[i].Name.Equals("EnableCull")
                        )
                    {
                        continue;
                    }

                    DrawField(m_ObjectDrawers[i]);
                }
            }
        }
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
                EditorGUILayout.BeginHorizontal();
                EditorUtils.StringRich(Name, 15);
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
                EditorGUILayout.EndHorizontal();

                using (new EditorUtils.BoxBlock(Color.black))
                {
                    for (int i = 0; i < currentValue.Length; i++)
                    {
                        EditorGUILayout.BeginHorizontal();

                        int idx = i;
                        EditorGUI.BeginChangeCheck();
                        idx = EditorGUILayout.DelayedIntField(idx, GUILayout.Width(40));
                        if (EditorGUI.EndChangeCheck())
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
                        }

                        idx = i;
                        ReflectionHelperEditor.DrawAttributeSelector(null, (attHash) =>
                        {
                            currentValue[idx] = new Reference<AttributeBase>(attHash);

                            AttributeBase targetAtt = currentValue[idx].GetObject();
                            if (targetAtt != null)
                            {
                                m_Drawers[idx] = ObjectBaseDrawer.GetDrawer(targetAtt);
                            }
                        }, currentValue[idx], TargetObject.GetType());

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

                            EditorGUILayout.EndHorizontal();
                            i--;
                            continue;
                        }

                        m_Open[i] = GUILayout.Toggle(m_Open[i], m_Open[i] ? EditorUtils.FoldoutOpendString : EditorUtils.FoldoutClosedString, EditorUtils.MiniButton, GUILayout.Width(20));

                        if (GUILayout.Button("C", GUILayout.Width(20)))
                        {
                            AttributeBase cloneAtt = (AttributeBase)EntityDataList.Instance.GetObject(currentValue[i]).Clone();

                            cloneAtt.Hash = Hash.NewHash();
                            cloneAtt.Name += "_Clone";
                            EntityDataList.Instance.m_Objects.Add(cloneAtt.Hash, cloneAtt);

                            currentValue[i] = new Reference<AttributeBase>(cloneAtt.Hash);
                            m_Drawers[i] = ObjectBaseDrawer.GetDrawer(cloneAtt);
                        }

                        EditorGUILayout.EndHorizontal();

                        if (m_Open[i])
                        {
                            Color color3 = Color.red;
                            color3.a = .7f;

                            using (new EditorUtils.BoxBlock(color3))
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

                            EditorUtils.Line();
                        }
                    }
                }

                return currentValue;
            }

        }

        public class ObjectBaseDrawer : ObjectDrawerBase
        {
            protected static readonly Dictionary<ObjectBase, ObjectBaseDrawer> Pool = new Dictionary<ObjectBase, ObjectBaseDrawer>();

            public readonly ObjectBase m_TargetObject;
            private Type m_Type;
            private ObsoleteAttribute m_Obsolete;
            private ReflectionDescriptionAttribute m_Description;

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
                m_Description = m_Type.GetCustomAttribute<ReflectionDescriptionAttribute>();

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

                DrawDescription();

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
            protected void DrawDescription()
            {
                if (m_Description == null) return;

                EditorGUILayout.HelpBox(m_Description.m_Description, MessageType.Info);
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
