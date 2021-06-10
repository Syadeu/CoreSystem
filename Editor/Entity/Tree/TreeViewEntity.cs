using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace SyadeuEditor.Tree
{
    public class TreeViewEntity : TreeView
    {
        const float kRowHeights = 20f;
        const float kToggleWidth = 18f;

        private TreeEntity m_Tree;
        private readonly List<TreeViewItem> m_Rows = new List<TreeViewItem>(100);

        public TreeEntity Tree => m_Tree;
        public event System.Action<IList<TreeViewItem>> beforeDroppingDraggedItems;

        public TreeViewEntity(TreeViewState state, TreeEntity tree) : base(state)
        {
            Initialize(tree);
        }
        public TreeViewEntity(TreeViewState state, MultiColumnHeader multiColumnHeader, TreeEntity tree) : base(state, multiColumnHeader)
        {
            Initialize(tree);

            //Assert.AreEqual(m_SortOptions.Length, Enum.GetValues(typeof(MyColumns)).Length, "Ensure number of sort options are in sync with number of MyColumns enum values");

            // Custom setup
            rowHeight = kRowHeights;
            columnIndexForTreeFoldouts = 0;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            customFoldoutYOffset = (kRowHeights - EditorGUIUtility.singleLineHeight) * 0.5f; // center foldout in the row since we also center content. See RowGUI
            extraSpaceBeforeIconAndLabel = kToggleWidth;
            //multiColumnHeader.sortingChanged += OnSortingChanged;

            Reload();
        }
        private void Initialize(TreeEntity tree)
        {
            m_Tree = tree;
        }

        #region Overrides

        protected override TreeViewItem BuildRoot()
        {
            int depthForHiddenRoot = -1;
            return new TreeViewItem<TreeFolderElement>(m_Tree.Root.ID, depthForHiddenRoot, m_Tree.Root.Name, m_Tree.Root);
        }
        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            if (m_Tree.Root == null)
            {
                Debug.LogError("tree model root is null. did you call SetData()?");
            }
            m_Rows.Clear();

            if (m_Tree.Root.HasChilds) AddChildrenRecursive(m_Tree.Root, 0, m_Rows);

            // We still need to setup the child parent information for the rows since this 
            // information is used by the TreeView internal logic (navigation, dragging etc)
            SetupParentsAndChildrenFromDepths(root, m_Rows);

            return m_Rows;
        }
        public override void OnGUI(Rect rect)
        {
            base.OnGUI(rect);
        }
        protected override void RowGUI(RowGUIArgs args)
        {
            var item = (TreeViewItem<TreeElementEntity>)args.item;
            for (int i = 0; i < args.GetNumVisibleColumns(); i++)
            {
                CellGUI(args.GetCellRect(i), item, args.GetColumn(i), ref args);
            }

            //base.RowGUI(args);
        }

        protected override void BeforeRowsGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                const string style = "miniButton";
                if (GUILayout.Button("Expand All", style))
                {
                    ExpandAll();
                }

                if (GUILayout.Button("Collapse All", style))
                {
                    CollapseAll();
                }

                GUILayout.FlexibleSpace();

                //if (GUILayout.Button("Add Item", style))
                //{
                //    Undo.RecordObject(asset, "Add Item To Asset");

                //    // Add item as child of selection
                //    var selection = GetSelection();
                //    var parent = (selection.Count == 1 ? m_Tree.Find(selection[0]) : null) ?? m_Tree.Root;
                //    int depth = parent != null ? parent.Depth + 1 : 0;
                //    int id = m_Tree.GetUniqueID();
                //    var element = new MyTreeElement("Item " + id, depth, id);
                //    m_Tree.AddElements(element, parent, 0);

                //    // Select newly created element
                //    m_TreeView.SetSelection(new[] { id }, TreeViewSelectionOptions.RevealAndFrame);
                //}

                //if (GUILayout.Button("Remove Item", style))
                //{
                //    Undo.RecordObject(asset, "Remove Item From Asset");
                //    var selection = m_TreeView.GetSelection();
                //    m_TreeView.treeModel.RemoveElements(selection);
                //}
            }
            base.BeforeRowsGUI();
        }

        protected override IList<int> GetAncestors(int id) => m_Tree.GetAncestors(id);
        protected override IList<int> GetDescendantsThatHaveChildren(int id) => m_Tree.GetDescendantsThatHaveChildren(id);

        #endregion

        private void CellGUI(Rect cellRect, TreeViewItem<TreeElementEntity> item, int column, ref RowGUIArgs args)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);


            switch (column)
            {
                case 0:
                    //Rect toggleRect = cellRect;
                    //toggleRect.x += GetContentIndent(item);
                    //toggleRect.width = kToggleWidth;
                    //if (toggleRect.xMax < cellRect.xMax)
                    //    item.Data.enabled = EditorGUI.Toggle(toggleRect, item.Data.enabled); // hide when outside cell rect
                    //Rect temp = cellRect;
                    //temp.x += GetContentIndent(item);
                    ////EditorGUI.LabelField(temp, $"{item.Data.Name}");

                    args.rowRect = cellRect;
                    base.RowGUI(args);
                    break;
                default:
                    item.Data.DrawGUI(cellRect, column);
                    break;
            }
        }


        private void AddChildrenRecursive(TreeElementEntity parent, int depth, IList<TreeViewItem> newRows)
        {
            foreach (TreeElementEntity child in parent.Childs)
            {
                var item = new TreeViewItem<TreeElementEntity>(child.ID, depth, child.Name, child);
                newRows.Add(item);

                if (child.HasChilds)
                {
                    if (IsExpanded(child.ID))
                    {
                        AddChildrenRecursive(child, depth + 1, newRows);
                    }
                    else
                    {
                        item.children = CreateChildListForCollapsedParent();
                    }
                }
            }
        }

        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState(float treeViewWidth)
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent(EditorGUIUtility.FindTexture("FilterByLabel"), "Lorem ipsum dolor sit amet, consectetur adipiscing elit. "),
                    contextMenuText = "Asset",
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = 30,
                    minWidth = 30,
                    maxWidth = 60,
                    autoResize = false,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent(EditorGUIUtility.FindTexture("FilterByType"), "Sed hendrerit mi enim, eu iaculis leo tincidunt at."),
                    contextMenuText = "Type",
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = 30,
                    minWidth = 30,
                    maxWidth = 60,
                    autoResize = false,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Name"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 150,
                    minWidth = 60,
                    autoResize = false,
                    allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Multiplier", "In sed porta ante. Nunc et nulla mi."),
                    headerTextAlignment = TextAlignment.Right,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = 110,
                    minWidth = 60,
                    autoResize = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Material", "Maecenas congue non tortor eget vulputate."),
                    headerTextAlignment = TextAlignment.Right,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = 95,
                    minWidth = 60,
                    autoResize = true,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Note", "Nam at tellus ultricies ligula vehicula ornare sit amet quis metus."),
                    headerTextAlignment = TextAlignment.Right,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = 70,
                    minWidth = 60,
                    autoResize = true
                }
            };

            //Assert.AreEqual(columns.Length, Enum.GetValues(typeof(MyColumns)).Length, "Number of columns should match number of enum values: You probably forgot to update one of them.");

            var state = new MultiColumnHeaderState(columns);
            return state;
        }

        #region Drag

        const string k_GenericDragID = "GenericDragColumnDragging";
        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            // Check if we can handle the current drag data (could be dragged in from other areas/windows in the editor)
            var draggedRows = DragAndDrop.GetGenericData(k_GenericDragID) as List<TreeViewItem>;
            if (draggedRows == null)
                return DragAndDropVisualMode.None;

            // Parent item is null when dragging outside any tree view items.
            switch (args.dragAndDropPosition)
            {
                case DragAndDropPosition.UponItem:
                case DragAndDropPosition.BetweenItems:
                    {
                        bool validDrag = ValidDrag(args.parentItem, draggedRows);
                        if (args.performDrop && validDrag)
                        {
                            TreeElementEntity parentData = ((TreeViewItem<TreeElementEntity>)args.parentItem).Data;
                            OnDropDraggedElementsAtIndex(draggedRows, parentData, args.insertAtIndex == -1 ? 0 : args.insertAtIndex);
                        }
                        return validDrag ? DragAndDropVisualMode.Move : DragAndDropVisualMode.None;
                    }

                case DragAndDropPosition.OutsideItems:
                    {
                        if (args.performDrop)
                            OnDropDraggedElementsAtIndex(draggedRows, m_Tree.Root, m_Tree.Root.Childs.Count);

                        return DragAndDropVisualMode.Move;
                    }
                default:
                    Debug.LogError("Unhandled enum " + args.dragAndDropPosition);
                    return DragAndDropVisualMode.None;
            }
        }
        public virtual void OnDropDraggedElementsAtIndex<T>(List<TreeViewItem> draggedRows, T parent, int insertIndex) where T : TreeElementEntity
        {
            if (beforeDroppingDraggedItems != null)
                beforeDroppingDraggedItems(draggedRows);

            var draggedElements = new List<TreeElementEntity>();
            foreach (var x in draggedRows)
                draggedElements.Add(((TreeViewItem<T>)x).Data);

            var selectedIDs = draggedElements.Select(x => x.ID).ToArray();
            m_Tree.MoveElements(parent, insertIndex, draggedElements);
            SetSelection(selectedIDs, TreeViewSelectionOptions.RevealAndFrame);
        }
        private bool ValidDrag(TreeViewItem parent, List<TreeViewItem> draggedItems)
        {
            TreeViewItem currentParent = parent;
            while (currentParent != null)
            {
                if (draggedItems.Contains(currentParent))
                    return false;
                currentParent = currentParent.parent;
            }
            return true;
        }

        #endregion
    }
    internal class TreeViewItem<T> : TreeViewItem where T : TreeElementEntity
    {
        public T Data { get; set; }
        public TreeViewItem(int id, int depth, string displayName, T data) : base(id, depth, displayName)
        {
            Data = data;
        }
    }

    public class VerticalTreeView
    {
        private SearchField m_SearchField;
        private string m_SearchString = null;

        private List<VerticalTreeElement> m_Elements;

        public VerticalTreeView(params VerticalTreeElement[] elements)
        {
            m_SearchField = new SearchField();
            SetupElements(elements);
        }
        private void SetupElements(params VerticalTreeElement[] elements)
        {
            List<VerticalTreeElement> temp = new List<VerticalTreeElement>(elements);
            for (int i = temp.Count - 1; i >= 0; i--)
            {
                if (temp[i].Parent != null)
                {
                    if (!temp[i].m_Childs.Contains(temp[i]))
                    {
                        temp[i].m_Childs.Add(temp[i]);
                    }
                    temp.RemoveAt(i);
                }
            }

            m_Elements = temp;
        }

        public void OnGUI()
        {
            BeforeDraw();
            m_SearchString = m_SearchField.OnGUI(GUILayoutUtility.GetRect(Screen.width, 20), m_SearchString);
            BeforeDrawChilds();
            for (int i = 0; i < m_Elements.Count; i++)
            {
                if (!string.IsNullOrEmpty(m_SearchString))
                {
                    if (!m_Elements[i].Name.Contains(m_SearchString)) continue;
                }
                DrawChild(m_Elements[i]);

                if (i + 1 < m_Elements.Count) EditorUtils.SectorLine();
            }
            AfterDraw();
        }
        public void RemoveElements(params VerticalTreeElement[] elements)
        {
            m_Elements = m_Elements.Where((other) => !elements.Contains(other)).ToList();
        }

        protected virtual void BeforeDraw() { }
        protected virtual void BeforeDrawChilds() { }
        protected virtual void AfterDraw() { }

        protected virtual void DrawChild(VerticalTreeElement e)
        {
            if (e.HasChilds)
            {
                e.m_Opened = EditorUtils.Foldout(e.m_Opened, e.Name);
                if (e.m_Opened)
                {
                    e.OnGUI();

                    EditorGUILayout.Space();

                    EditorGUI.indentLevel += 1;
                    for (int i = 0; i < e.Childs.Count; i++)
                    {
                        DrawChild(e.Childs[i]);
                    }
                    EditorGUI.indentLevel -= 1;
                }
            }
            else
            {
                e.OnGUI();
            }
        }
    }
    public abstract class VerticalTreeElement
    {
        private VerticalTreeView m_Tree;

        [SerializeField] protected string m_Name;

        [NonSerialized] internal VerticalTreeElement m_Parent;
        [NonSerialized] internal List<VerticalTreeElement> m_Childs;

        internal bool m_Opened = false;

        public virtual string Name => m_Name;

        public bool HasChilds => Childs != null && Childs.Count > 0;

        public VerticalTreeElement Parent => m_Parent;
        public IReadOnlyList<VerticalTreeElement> Childs => m_Childs;

        public VerticalTreeElement(VerticalTreeView tree)
        {
            m_Tree = tree;
        }

        public abstract void OnGUI();
        public void SetParent(VerticalTreeElement parent)
        {
            if (m_Parent != null)
            {
                RemoveParent();
            }
            else m_Tree.RemoveElements(this);

            if (parent.m_Childs == null) parent.m_Childs = new List<VerticalTreeElement>();
            parent.m_Childs.Add(this);
            m_Parent = parent;
        }
        public void RemoveParent()
        {
            if (m_Parent == null) return;

            if (m_Parent.Childs.Count == 1)
            {
                m_Parent.m_Childs = null;
            }
            else m_Parent.m_Childs.Remove(this);
            m_Parent = null;
        }

        protected Rect GetRect(float width, float height) => GUILayoutUtility.GetRect(width, height);
    }
    public sealed class VerticalLabelTreeElement : VerticalTreeElement
    {
        private string m_Label1 = null;
        private string m_Label2 = null;

        public VerticalLabelTreeElement(VerticalTreeView tree, string label) : base(tree)
        {
            m_Label1 = label;
        }
        public VerticalLabelTreeElement(VerticalTreeView tree, string label1, string label2) : base(tree)
        {
            m_Label1 = label1; m_Label2 = label2;
        }

        public override void OnGUI() => EditorGUILayout.LabelField(m_Label1, m_Label2);
    }
}