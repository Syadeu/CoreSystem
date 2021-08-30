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

        private readonly TreeViewItem m_Root;
        private readonly List<TreeViewItem> m_Rows = new List<TreeViewItem>();

        public event Action<EntityWindow.ObjectBaseDrawer> OnSelect;

        private SearchField m_SearchField;
        private Dictionary<Hash, ObjectBase> Objects => EntityDataList.Instance.m_Objects;

        public enum Column
        {
            Name
        }
        public static MultiColumnHeader CreateHeader()
        {
            var Columns = new MultiColumnHeaderState.Column[]
                {
                    new MultiColumnHeaderState.Column()
                    {
                        autoResize = false,
                        allowToggleVisibility = false,
                        headerContent = new GUIContent(Column.Name.ToString()),
                        headerTextAlignment = TextAlignment.Center,
                        minWidth = 150
                    }
                };
            var MultiColumnHeaderState = new MultiColumnHeaderState(Columns);
            return new MultiColumnHeader(MultiColumnHeaderState);
        }

        public EntityListTreeView(TreeViewState state) : base(state)
        {
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

            Reload();
        }
        public EntityListTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader) : base(state, multiColumnHeader)
        {
            m_SearchField = new SearchField()
            {
                autoSetFocusOnFindCommand = false,
            };

            m_Root = new TreeViewItem()
            {
                id = 0,
                depth = -1,
                displayName = "Root"
            };

            rowHeight = kRowHeights;
            columnIndexForTreeFoldouts = 0;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            customFoldoutYOffset = (kRowHeights - EditorGUIUtility.singleLineHeight) * 0.5f;
            extraSpaceBeforeIconAndLabel = kToggleWidth;

            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            m_Root.children?.Clear();
            m_Rows.Clear();

            if (Objects != null)
            {
                int id = 1;
                EntityWindow.ObjectBaseDrawer drawer;
                foreach (var item in Objects?.Values)
                {
                    drawer = EntityWindow.ObjectBaseDrawer.GetDrawer(item);

                    BuildBaseType(ref id, m_Root, drawer.Type);
                    TreeViewItem folder = GetFolder(drawer.Type);
                    //if (folder == null)
                    //{
                    //    folder = new FolderTreeElement(id, drawer.Type);
                    //    m_Root.AddChild(folder);
                    //    m_Rows.Add(folder);
                    //    id++;
                    //}

                    folder.AddChild(new ObjectTreeElement(id, drawer));
                    id++;
                }
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
        private void FindTypesRecursive(Type type, List<Type> types)
        {
            types.Add(type);
            if (type.BaseType.Equals(TypeHelper.TypeOf<ObjectBase>.Type) ||
                type.BaseType.Equals(TypeHelper.TypeOf<InstanceAction>.Type))
            {
                return;
            }

            FindTypesRecursive(type.BaseType, types);
        }
        private TreeViewItem BuildBaseType(ref int id, TreeViewItem root, Type type)
        {
            List<Type> types = new List<Type>();
            FindTypesRecursive(type, types);

            TreeViewItem typeRoot;
            Type baseType = types[types.Count - 1];
            TreeViewItem folder = GetFolder(baseType);
            if (folder == null)
            {
                folder = new FolderTreeElement(id, baseType);
                m_Root.AddChild(folder);
                m_Rows.Add(folder);
                id++;
            }
            typeRoot = folder;

            for (int i = types.Count - 2; i >= 0; i--)
            {
                folder = GetFolder(types[i]);
                if (folder == null)
                {
                    folder = new FolderTreeElement(id, types[i]);
                    typeRoot.AddChild(folder);
                    m_Rows.Add(folder);
                    id++;
                }

                typeRoot = folder;
            }

            return typeRoot;
        }

        public override void OnGUI(Rect rect)
        {
            rect.y += 5;
            rect.height -= 5;

            Rect searchField = new Rect(rect);
            searchField.height = kRowHeights;

            searchString = m_SearchField.OnGUI(searchField, searchString);
            int keyboard = GUIUtility.GetControlID(FocusType.Keyboard);
            switch (Event.current.GetTypeForControl(keyboard))
            {
                case EventType.KeyDown:
                    if (Event.current.keyCode == KeyCode.Return)
                    {
                        GUIUtility.hotControl = keyboard;

                        Event.current.Use();
                    }
                    break;
                default:
                    break;
            }
            

            rect.y += kRowHeights;
            rect.height -= kRowHeights;

            base.OnGUI(rect);
        }
        protected override void RowGUI(RowGUIArgs args)
        {
            if (multiColumnHeader == null)
            {
                base.RowGUI(args);
                return;
            }

            int visibleColumnCount = args.GetNumVisibleColumns();
            for (int i = 0; i < visibleColumnCount; i++)
            {
                CellGUI(args.GetCellRect(i), args.item, (Column)i, ref args);
            }
        }
        private void CellGUI(Rect cellRect, TreeViewItem item, Column column, ref RowGUIArgs args)
        {
            ObjectTreeElement element;
            switch (column)
            {
                case Column.Name:
                    cellRect = GetCellRectForTreeFoldouts(cellRect);
                    cellRect.x += GetContentIndent(item);
                    cellRect.width -= GetContentIndent(item);
                    CenterRectUsingSingleLineHeight(ref cellRect);

                    GUI.Label(cellRect, item.displayName);
                    break;
                default:
                    break;
            }

            //base.RowGUI(args);
        }

        protected override bool DoesItemMatchSearch(TreeViewItem item, string search)
        {
            return base.DoesItemMatchSearch(item, search);
        }
        protected override void SelectionChanged(IList<int> selectedIds)
        {
            var list = FindRows(selectedIds);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is FolderTreeElement) continue;

                ObjectTreeElement obj = (ObjectTreeElement)list[i];
                OnSelect?.Invoke(obj.Target);
            }

            base.SelectionChanged(selectedIds);
        }

        public class FolderTreeElement : TreeViewItem
        {
            private Type m_Type;
            private DisplayNameAttribute m_DisplayNameAttribute;

            public Type Type => m_Type;
            public override string displayName
            {
                get
                {
                    //if (m_DisplayNameAttribute != null)
                    //{
                    //    return m_DisplayNameAttribute.DisplayName;
                    //}
                    return TypeHelper.ToString(m_Type);
                }
            }

            public FolderTreeElement(int id, Type type)
            {
                this.id = id;
                m_Type = type;

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
                    if (m_DisplayNameAttribute != null)
                    {
                        return m_DisplayNameAttribute.DisplayName;
                    }

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
