using Syadeu;
using Syadeu.Collections;
using Syadeu.Internal;
using Syadeu.Presentation;
using Syadeu.Presentation.Data;
using Syadeu.Presentation.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    public sealed class DebuggerListTreeView : TreeView
    {
        const float kRowHeights = 20f;
        const float kToggleWidth = 18f;

        private int m_CreationID = 0;
        private SearchField m_SearchField;
        private readonly TreeViewItem m_Root;
        private readonly List<TreeViewItem> 
            m_Rows = new List<TreeViewItem>(),
            
            m_EntitiesRow = new List<TreeViewItem>(),
            m_DataRow = new List<TreeViewItem>(),
            m_OtherRow = new List<TreeViewItem>();

        private readonly EntityWindow m_Window;

        private EntitySystem m_EntitySystem;
        private Dictionary<InstanceID, ObjectBase> m_ObjectEntities;
        public EntitySystem EntitySystem
        {
            get
            {
                if (m_EntitySystem == null)
                {
                    if (!Application.isPlaying || !PresentationSystem<DefaultPresentationGroup, EntitySystem>.IsValid()) return null;

                    m_EntitySystem = PresentationSystem<DefaultPresentationGroup, EntitySystem>.System;
                }
                return m_EntitySystem;
            }
        }
        public Dictionary<InstanceID, ObjectBase> ObjectEntities
        {
            get
            {
                if (m_ObjectEntities == null)
                {
                    if (EntitySystem == null) return null;

                    m_ObjectEntities = (Dictionary<InstanceID, ObjectBase>)TypeHelper.TypeOf<EntitySystem>.Type.GetField("m_ObjectEntities", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(EntitySystem);
                }

                return m_ObjectEntities;
            }
        }

        public DebuggerListTreeView(EntityWindow mainWindow, TreeViewState state) : base(state)
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
            m_EntitiesRow.Clear();
            m_DataRow.Clear();
            m_OtherRow.Clear();
            m_CreationID = 1;

            if (ObjectEntities != null)
            {
                var list = ObjectEntities.Values.ToArray();
                foreach (var item in list)
                {
                    List<TreeViewItem> targetList;
                    if (item is EntityDataBase)
                    {
                        if (m_EntitiesRow.Count == 0)
                        {
                            m_EntitiesRow.Add(new TreeViewItem(m_CreationID, 0, "Entity"));
                            m_CreationID++;
                        }
                        targetList = m_EntitiesRow;
                    }
                    else if (item is DataObjectBase)
                    {
                        if (m_DataRow.Count == 0)
                        {
                            m_DataRow.Add(new TreeViewItem(m_CreationID, 0, "Data"));
                            m_CreationID++;
                        }
                        targetList = m_DataRow;
                    }
                    else
                    {
                        if (m_OtherRow.Count == 0)
                        {
                            m_OtherRow.Add(new TreeViewItem(m_CreationID, 0, "Other"));
                            m_CreationID++;
                        }
                        targetList = m_OtherRow;
                    }

                    targetList.Add(new ObjectTreeViewItem(m_CreationID, 1, item.Name, item));
                    m_CreationID++;
                }

                m_Rows.AddRange(m_EntitiesRow);
                m_Rows.AddRange(m_DataRow);
                m_Rows.AddRange(m_OtherRow);

                SetupParentsAndChildrenFromDepths(m_Root, m_Rows);
            }
            else
            {
                m_Root.AddChild(new TreeViewItem(m_CreationID, 0, "None"));
            }

            return m_Root;
        }
        public override void OnGUI(Rect rect)
        {
            rect.y += 5;
            rect.height -= 5;

            {
                Rect fieldRect = new Rect(rect);
                fieldRect.height = kRowHeights;

                if (GUI.Button(fieldRect, "Capture"))
                {
                    Reload();
                }

                rect.y += kRowHeights;
                rect.height -= kRowHeights;
            }
            {
                Rect fieldRect = new Rect(rect);
                fieldRect.height = kRowHeights;

                searchString = m_SearchField.OnGUI(fieldRect, searchString);

                rect.y += kRowHeights;
                rect.height -= kRowHeights;
            }

            base.OnGUI(rect);
        }
        protected override void SelectionChanged(IList<int> selectedIds)
        {
            var list = FindRows(selectedIds);
            if (list.Count > 0 && list[0] is ObjectTreeViewItem objitem)
            {
                m_Window.m_DebuggerViewWindow.Selected = objitem.m_ObjectBase;
            }

            base.SelectionChanged(selectedIds);
        }

        public void Select(IInstance instance)
        {
            var iter = GetRows().Where((other) => other is ObjectTreeViewItem item && item.m_ObjectBase.Idx.Equals(instance.Idx));

            if (!iter.Any())
            {
                "asdasd".ToLog();
                return;
            }

            var target = iter.First();
            int idx = FindRowOfItem(target);

            SetExpanded(target.parent.id, true);
            FrameItem(target.id);
            SetSelection(new int[] { target.id });
        }

        private class ObjectTreeViewItem : TreeViewItem
        {
            public Instance<ObjectBase> m_ObjectBase;

            public ObjectTreeViewItem(int id, int depth, string displayName, ObjectBase obj) : base(id, depth, displayName)
            {
                m_ObjectBase = new Instance<ObjectBase>(obj);
            }
        }
    }
}
