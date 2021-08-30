using Syadeu;
using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    public sealed class EntityListTreeView : TreeView
    {
        const float kRowHeights = 20f;
        const float kToggleWidth = 18f;

        private int m_CreationID = 0;
        private SearchField m_SearchField;
        private readonly TreeViewItem m_Root;
        private readonly List<TreeViewItem> m_Rows = new List<TreeViewItem>();

        private readonly EntityWindow m_Window;

        public event Action<EntityWindow.ObjectBaseDrawer> OnSelect;

        private Dictionary<Hash, ObjectBase> Objects => EntityDataList.Instance.m_Objects;

        public EntityListTreeView(EntityWindow mainWindow, TreeViewState state) : base(state)
        {
            m_Window = mainWindow;
            m_SearchField = new SearchField();

            m_Root = new TreeViewItem()
            {
                id = 0,
                depth = -1,
                displayName = "Root"
            };

            rowHeight = kRowHeights;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            customFoldoutYOffset = (kRowHeights - EditorGUIUtility.singleLineHeight) * 0.5f;

            //if (Objects == null || Objects.Count == 0) return;
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            m_Root.children?.Clear();
            m_Rows.Clear();
            m_CreationID = 1;

            if (Objects != null && Objects.Count > 0)
            {
                EntityWindow.ObjectBaseDrawer drawer;
                foreach (var item in Objects?.Values)
                {
                    drawer = EntityWindow.ObjectBaseDrawer.GetDrawer(item);

                    TreeViewItem folder = GetFolder(drawer.Type);
                    if (folder == null)
                    {
                        folder = new FolderTreeElement(m_CreationID, drawer.Type);
                        m_Root.AddChild(folder);
                        m_Rows.Add(folder);
                        m_CreationID++;
                    }

                    folder.AddChild(new ObjectTreeElement(m_CreationID, drawer));
                    m_CreationID++;
                }
            }
            else
            {
                m_Root.AddChild(new TreeViewItem(m_CreationID, 0, "None"));
            }

            SetupDepthsFromParentsAndChildren(m_Root);

            return m_Root;
        }
        private TreeViewItem GetFolder(Type type)
        {
            var iter = m_Rows.Where((other) => 
                other is FolderTreeElement folder &&
                folder.Type.Equals(type));

            if (iter.Any()) return iter.First();
            return null;
        }

        public void AddItem(EntityWindow.ObjectBaseDrawer drawer)
        {
            TreeViewItem folder = GetFolder(drawer.Type);
            if (folder == null)
            {
                folder = new FolderTreeElement(m_CreationID, drawer.Type);
                m_Root.AddChild(folder);
                m_Rows.Add(folder);
                m_CreationID++;
            }

            folder.AddChild(new ObjectTreeElement(m_CreationID, drawer));
            m_CreationID++;

            SetupDepthsFromParentsAndChildren(m_Root);
        }

        public override void OnGUI(Rect rect)
        {
            rect.y += 5;
            rect.height -= 5;

            Rect searchField = new Rect(rect);
            searchField.height = kRowHeights;

            searchString = m_SearchField.OnGUI(searchField, searchString);

            rect.y += kRowHeights;
            rect.height -= kRowHeights;

            if (rootItem?.children == null || rootItem.children.Count == 0) return;
            base.OnGUI(rect);
        }
        protected override void RowGUI(RowGUIArgs args)
        {
            base.RowGUI(args);
        }

        protected override bool DoesItemMatchSearch(TreeViewItem item, string search)
        {
            if (item is FolderTreeElement) return false;

            return base.DoesItemMatchSearch(item, search);
        }
        protected override void SelectionChanged(IList<int> selectedIds)
        {
            var list = FindRows(selectedIds);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is FolderTreeElement) continue;

                if (list[i] is ObjectTreeElement obj)
                {
                    OnSelect?.Invoke(obj.Target);
                }
            }

            base.SelectionChanged(selectedIds);
        }

        protected override void ContextClickedItem(int id)
        {
            TreeViewItem item = FindItem(id, rootItem);

            GenericMenu menu = new GenericMenu();
            menu.AddDisabledItem(new GUIContent(item.displayName));
            menu.AddSeparator(string.Empty);
            if (item is FolderTreeElement folder)
            {
                menu.AddItem(new GUIContent("Add"), false, () =>
                {
                    if (EntityDataList.Instance.m_Objects == null) EntityDataList.Instance.m_Objects = new Dictionary<Hash, ObjectBase>();

                    ObjectBase ins = (ObjectBase)Activator.CreateInstance(folder.Type);

                    EntityDataList.Instance.m_Objects.Add(ins.Hash, ins);
                    var drawer = m_Window.AddData(ins);
                    AddItem(drawer);

                    Reload();
                });
            }
            else if (item is ObjectTreeElement obj)
            {
                menu.AddItem(new GUIContent("Remove"), false, () =>
                {
                    m_Window.Remove(obj.Target);
                    item.parent.children.Remove(item);

                    Reload();
                });
            }
            menu.ShowAsContext();

            base.ContextClickedItem(id);
        }

        public class FolderTreeElement : TreeViewItem
        {
            private Type m_Type;
            private ObsoleteAttribute m_ObsoleteAttribute;
            private DisplayNameAttribute m_DisplayNameAttribute;

            public Type Type => m_Type;
            public override string displayName
            {
                get
                {
                    string output = string.Empty;

                    if (m_ObsoleteAttribute != null)
                    {
                        output += "[Deprecated] ";
                    }
                    if (m_DisplayNameAttribute != null)
                    {
                        output += m_DisplayNameAttribute.DisplayName;
                    }
                    else output += TypeHelper.ToString(m_Type);

                    return output;
                }
            }

            public FolderTreeElement(int id, Type type)
            {
                this.id = id;
                m_Type = type;

                m_ObsoleteAttribute = type.GetCustomAttribute<ObsoleteAttribute>();
                m_DisplayNameAttribute = type.GetCustomAttribute<DisplayNameAttribute>();
            }
        }
        public sealed class ObjectTreeElement : TreeViewItem
        {
            private EntityWindow.ObjectBaseDrawer m_Target;
            private DisplayNameAttribute m_DisplayNameAttribute;

            public override string displayName
            {
                get
                {
                    return m_Target.Name;
                }
            }
            public EntityWindow.ObjectBaseDrawer Target => m_Target;

            public ObjectTreeElement(int id, EntityWindow.ObjectBaseDrawer drawer)
            {
                this.id = id;
                m_Target = drawer;

                m_DisplayNameAttribute = drawer.Type.GetCustomAttribute<DisplayNameAttribute>();
            }
        }
    }
}
