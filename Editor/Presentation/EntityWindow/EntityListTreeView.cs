using Syadeu;
using Syadeu.Collections;
using Syadeu.Internal;
using Syadeu.Presentation;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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
        private static readonly Color s_WarningColor = new Color32(255, 178, 102, 255);
        private static readonly Color s_ErrorColor = new Color32(255, 102, 102, 255);

        private int m_CreationID = 0;
        private SearchField m_SearchField;
        private readonly TreeViewItem m_Root;
        private readonly Dictionary<Type, FolderTreeElement> m_Folders = new Dictionary<Type, FolderTreeElement>();
        private readonly Dictionary<Hash, ElementBase> m_Rows = new Dictionary<Hash, ElementBase>();

        private readonly EntityWindow m_Window;

        public event Action<ObjectBase> OnSelect;

        private Dictionary<ulong, ObjectBase> Objects => EntityDataList.Instance.m_Objects;

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

            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            m_Root.children?.Clear();
            m_Rows.Clear();
            m_Folders.Clear();
            m_CreationID = 1;

            if (Objects != null && Objects.Count > 0)
            {
                foreach (var item in Objects.Values)
                {
                    FolderTreeElement folder = GetFolder(item.GetType());
                    if (folder == null)
                    {
                        folder = new FolderTreeElement(m_CreationID, item.GetType());
                        m_Root.AddChild(folder);
                        m_Folders.Add(item.GetType(), folder);
                        m_CreationID++;
                    }

                    var element = new ObjectTreeElement(m_CreationID, item);
                    folder.AddChild(element);
                    m_Rows.Add(item.Hash, element);

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
        public FolderTreeElement GetFolder(Type type)
        {
            if (m_Folders.TryGetValue(type, out var folder)) return folder;
            return null;
        }

        public void AddItem(ObjectBase entityObj)
        {
            FolderTreeElement folder = GetFolder(entityObj.GetType());
            if (folder == null)
            {
                folder = new FolderTreeElement(m_CreationID, entityObj.GetType());
                m_Root.AddChild(folder);
                m_Folders.Add(entityObj.GetType(), folder);
                
                m_CreationID++;
            }

            ObjectTreeElement element = new ObjectTreeElement(m_CreationID, entityObj);
            folder.AddChild(element);
            m_Rows.Add(entityObj.Hash, element);
            m_CreationID++;

            SetupDepthsFromParentsAndChildren(m_Root);
        }
        public void RemoveItem(ObjectBase entityObj)
        {
            m_Folders[entityObj.GetType()].children.Remove(m_Rows[entityObj.Hash]);

            m_Rows.Remove(entityObj.Hash);

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
            if (!(args.item is ElementBase element))
            {
                base.RowGUI(args);
                return;
            }

            Color origin = GUI.color;
            if (element.Obsolete != null)
            {
                if (element.Obsolete.IsError) GUI.color = s_ErrorColor;
                else GUI.color = s_WarningColor;
            }

            base.RowGUI(args);
            GUI.color = origin;
        }

        static Regex s_SearchReferencerRegex = new Regex(@"((ref:)[0-9]{18})");
        static Regex s_SearchWithHashRegex = new Regex(@"^([0-9]{18})");

        protected override bool DoesItemMatchSearch(TreeViewItem item, string search)
        {
            if (item is FolderTreeElement) return false;

            Match searchWithHash = s_SearchWithHashRegex.Match(search);
            if (searchWithHash.Success)
            {
                var temp = (ObjectTreeElement)item;
                if (temp.Target.Hash.ToString().StartsWith(searchWithHash.Value))
                {
                    return true;
                }
                return false;
            }

            Match searchReferencer = s_SearchReferencerRegex.Match(search);
            if (searchReferencer.Success)
            {
                string value = searchReferencer.Value.Replace("ref:", "");
                var temp = (ObjectTreeElement)item;
                string json = temp.Target.GetRawJson();

                if (!json.Contains(value))
                {
                    return false;
                }
                else return true;
            }

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
                    var drawer = EntityWindow.Instance.Add(folder.Type);

                    SetSelection(drawer);
                });
            }
            else if (item is ObjectTreeElement obj)
            {
                menu.AddItem(new GUIContent("Duplicate"), false, () =>
                {
                    ObjectBase clone = (ObjectBase)obj.Target.Clone();

                    clone.Hash = Hash.NewHash();
                    clone.Name += "_Clone";

                    EntityWindow.Instance.Add(clone);
                    SetSelection(clone);
                });

                menu.AddItem(new GUIContent("Find Referencers"), false, () =>
                {
                    m_Window.m_DataListWindow.SearchString = $"ref:{obj.Target.Hash}";
                });
                menu.AddItem(new GUIContent("Remove"), false, () =>
                {
                    EntityWindow.Instance.Remove(obj.Target);

                    Reload();
                });
            }
            menu.ShowAsContext();

            base.ContextClickedItem(id);
        }
        
        #region Dragging

        const string k_GenericDragID = "GenericDragColumnDragging";

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            if (args.draggedItem is FolderTreeElement) return false;

            return true;
        }
        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            if (hasSearch) return;

            DragAndDrop.PrepareStartDrag();
            var draggedRows = GetRows().Where(item => args.draggedItemIDs.Contains(item.id)).ToList();
            DragAndDrop.SetGenericData(k_GenericDragID, draggedRows);
            DragAndDrop.objectReferences = new UnityEngine.Object[] { }; // this IS required for dragging to work
            string title = draggedRows.Count == 1 ? draggedRows[0].displayName : "< Multiple >";
            DragAndDrop.StartDrag(title);
        }
        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            var draggedRows = DragAndDrop.GetGenericData(k_GenericDragID) as List<TreeViewItem>;
            if (draggedRows == null)
            {
                return DragAndDropVisualMode.None;
            }

            switch (args.dragAndDropPosition)
            {
                case DragAndDropPosition.UponItem:
                    if (args.parentItem != null &&
                        args.parentItem is FolderTreeElement)
                    {
                        return DragAndDropVisualMode.Move;
                    }

                    return DragAndDropVisualMode.Rejected;
                case DragAndDropPosition.BetweenItems:

                    return DragAndDropVisualMode.Move;
                case DragAndDropPosition.OutsideItems:


                    return DragAndDropVisualMode.Move;
                default:

                    return DragAndDropVisualMode.None;
            }
        }
        private static bool IsValidDrag(List<TreeViewItem> dragged, TreeViewItem target)
        {
            if (target == null) return false;

            return true;
        }

        #endregion

        public void SetSelection(ObjectBase entityObj)
        {
            int id = m_Rows[entityObj.Hash].id;
            SetSelection(new int[] { id });
            FrameItem(id);

            OnSelect?.Invoke(entityObj);
        }
        public void SetSelection(IFixedReference reference)
        {
            int id = m_Rows[reference.Hash].id;
            SetSelection(new int[] { id });
            FrameItem(id);

            OnSelect?.Invoke(((ObjectTreeElement)m_Rows[reference.Hash]).Target);
        }

        public abstract class ElementBase : TreeViewItem
        {
            public abstract Type Type { get; }
            public abstract ObsoleteAttribute Obsolete { get; }
        }
        public class FolderTreeElement : ElementBase
        {
            private Type m_Type;
            private ObsoleteAttribute m_ObsoleteAttribute;
            private DisplayNameAttribute m_DisplayNameAttribute;

            public override Type Type => m_Type;
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
            public override ObsoleteAttribute Obsolete => m_ObsoleteAttribute;

            public FolderTreeElement(int id, Type type)
            {
                this.id = id;
                m_Type = type;

                m_ObsoleteAttribute = type.GetCustomAttribute<ObsoleteAttribute>();
                m_DisplayNameAttribute = type.GetCustomAttribute<DisplayNameAttribute>();
            }
        }
        public sealed class ObjectTreeElement : ElementBase
        {
            private ObjectBase m_Target;
            private Type m_Type;
            private ObsoleteAttribute m_ObsoleteAttribute;
            private DisplayNameAttribute m_DisplayNameAttribute;

            public override string displayName
            {
                get
                {
                    return m_Target.Name;
                }
            }
            public ObjectBase Target => m_Target;
            public override Type Type => m_Type;
            public override ObsoleteAttribute Obsolete => m_ObsoleteAttribute;

            public ObjectTreeElement(int id, ObjectBase entityObj)
            {
                this.id = id;
                m_Target = entityObj;
                m_Type = entityObj.GetType();

                m_ObsoleteAttribute = m_Type.GetCustomAttribute<ObsoleteAttribute>();
                m_DisplayNameAttribute = m_Type.GetCustomAttribute<DisplayNameAttribute>();
            }
        }
    }
}
