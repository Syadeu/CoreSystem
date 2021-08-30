using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation;
using Syadeu.Presentation.Entities;
using System;
using System.Collections.Generic;
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

        private SearchField m_SearchField;
        private Dictionary<Hash, ObjectBase> Objects => EntityDataList.Instance.m_Objects;

        public enum Column
        {
            Type,
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
                        headerContent = new GUIContent(Column.Type.ToString()),
                        headerTextAlignment = TextAlignment.Center,
                        minWidth = 25
                    },
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
                ObjectBaseDrawer drawer;
                foreach (var item in Objects?.Values)
                {
                    drawer = ObjectBaseDrawer.GetDrawer(item);
                    TreeViewItem folder = GetFolder(drawer.Type);
                    if (folder == null)
                    {
                        folder = new FolderTreeElement(id, drawer.Type);
                        m_Root.AddChild(folder);
                        id++;
                    }

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
            //base.RowGUI(args);

            if (args.item is FolderTreeElement folder)
            {
                Rect temp = args.rowRect;
                temp.x += GetContentIndent(args.item);
                temp.width -= GetContentIndent(args.item);

                GUI.Label(temp, args.item.displayName);

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
                case Column.Type:
                    //if (item is ObjectTreeElement) break;

                    //cellRect = GetCellRectForTreeFoldouts(cellRect);
                    //cellRect.x += GetContentIndent(item);
                    //cellRect.width -= GetContentIndent(item);
                    //CenterRectUsingSingleLineHeight(ref cellRect);

                    //GUI.Label(cellRect, item.displayName);
                    break;
                case Column.Name:
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

        #region Data Drawers
        public sealed class EntityDrawer : ObjectBaseDrawer
        {
            public EntityDataBase Target => (EntityDataBase)m_TargetObject;
            readonly ReflectionHelperEditor.AttributeListDrawer m_AttributeDrawer;

            public EntityDrawer(ObjectBase objectBase) : base(objectBase)
            {
                m_AttributeDrawer = ReflectionHelperEditor.GetAttributeDrawer(Type, Target.Attributes);
            }

            protected override void DrawGUI()
            {
                EditorUtils.StringRich(Name + EditorUtils.String($": {Type.Name}", 11), 20);
                EditorGUILayout.Space(3);
                EditorUtils.Line();

                Target.Name = EditorGUILayout.TextField("Name: ", Target.Name);
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("Hash: ", Target.Hash.ToString());
                EditorGUI.EndDisabledGroup();
                if (Target is EntityBase entityBase)
                {
                    ReflectionHelperEditor.DrawPrefabReference("Prefab: ",
                        (idx) =>
                        {
                            entityBase.Prefab = idx;
                            if (idx >= 0)
                            {
                                GameObject temp = (GameObject)entityBase.Prefab.GetObjectSetting().m_RefPrefab.editorAsset;
                                Transform tr = temp.transform;

                                AABB aabb = new AABB(tr.position, float3.zero);
                                foreach (var item in tr.GetComponentsInChildren<Renderer>())
                                {
                                    aabb.Encapsulate(item.bounds);
                                }
                                entityBase.Center = aabb.center - ((float3)tr.position);
                                entityBase.Size = aabb.size;
                            }
                        }
                        , entityBase.Prefab);
                }
                using (new EditorUtils.BoxBlock(Color.black))
                {
                    m_AttributeDrawer.OnGUI();
                }
                EditorUtils.Line();

                for (int i = 0; i < m_ObjectDrawers.Length; i++)
                {
                    if (m_ObjectDrawers[i] == null) continue;

                    if (m_ObjectDrawers[i].Name.Equals("Name") ||
                        m_ObjectDrawers[i].Name.Equals("Hash") ||
                        m_ObjectDrawers[i].Name.Equals("Prefab") ||
                        m_ObjectDrawers[i].Name.Equals("Attributes"))
                    {
                        continue;
                    }

                    DrawField(m_ObjectDrawers[i]);
                }
            }
        }
        public class ObjectBaseDrawer : ObjectDrawerBase
        {
            protected static readonly Dictionary<ObjectBase, ObjectBaseDrawer> Pool = new Dictionary<ObjectBase, ObjectBaseDrawer>();

            public readonly ObjectBase m_TargetObject;
            private Type m_Type;
            private ObsoleteAttribute m_Obsolete;

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
        }
        #endregion

        public class FolderTreeElement : TreeViewItem
        {
            private Type m_Type;

            public Type Type => m_Type;
            public override string displayName => TypeHelper.ToString(m_Type);

            public FolderTreeElement(int id, Type type)
            {
                this.id = id;
                m_Type = type;
            }
        }
        public sealed class ObjectTreeElement : TreeViewItem
        {
            private ObjectBaseDrawer m_Target;

            public override string displayName => m_Target.Name;
            public ObjectBaseDrawer Target => m_Target;

            public ObjectTreeElement(int id, ObjectBaseDrawer drawer)
            {
                this.id = id;
                m_Target = drawer;
            }
        }
    }
}
