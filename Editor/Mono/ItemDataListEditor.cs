using System;
using System.IO;
using Syadeu.Database;
using SyadeuEditor.Tree;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor
{
    [CustomEditor(typeof(ItemDataList))]
    public sealed class ItemDataListEditor : EditorEntity<ItemDataList>
    {
        private VerticalTreeView m_TreeView;

        private static string[] m_ItemTypes = new string[0];

        private bool m_ShowOriginalContents = false;

        private void OnEnable()
        {
            m_TreeView = new VerticalTreeView(Asset);
            OnValidate();
        }
        private void OnValidate()
        {
            if (m_TreeView == null) m_TreeView = new VerticalTreeView(Asset);
            m_TreeView
                .SetupElements(Asset.m_Items, (other) =>
                {
                    Item item = (Item)other;

                    return new TreeItemElement(m_TreeView, item);
                })
                ;

            m_ItemTypes = new string[ItemDataList.Instance.m_ItemTypes.Count];
            for (int i = 0; i < m_ItemTypes.Length; i++)
            {
                m_ItemTypes[i] = ItemDataList.Instance.m_ItemTypes[i].m_Name;
            }
        }

        public override void OnInspectorGUI()
        {
            EditorUtils.StringHeader("Item Datas");
            EditorUtils.SectorLine();

            if (GUILayout.Button("Clear"))
            {
                Asset.m_Items.Clear();
                Asset.m_ItemTypes.Clear();
                Asset.m_ItemEffectTypes.Clear();
                EditorUtils.SetDirty(target);
                OnValidate();
            }
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load"))
            {
                Asset.LoadDatas();
                EditorUtils.SetDirty(target);
                OnValidate();
            }
            if (GUILayout.Button("Save"))
            {
                Asset.SaveDatas();
                EditorUtils.SetDirty(target);
                OnValidate();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            m_TreeView.OnGUI();

            EditorGUILayout.Space();
            m_ShowOriginalContents = EditorUtils.Foldout(m_ShowOriginalContents, "Original Contents");
            if (m_ShowOriginalContents) base.OnInspectorGUI();
        }

        private ItemType GetItemType(string guid)
            => ItemDataList.Instance.GetItemType(guid);

        private class TreeItemElement : VerticalTreeElement<Item>
        {
            public override string Name => Target.m_Name;

            public TreeItemElement(VerticalTreeView treeView, Item item) : base(treeView, item)
            {
            }

            public override void OnGUI()
            {
                Target.m_Name = EditorGUILayout.TextField("Name: ", Target.m_Name);
                
                using (new EditorGUILayout.VerticalScope("Box"))
                {
                    EditorGUILayout.LabelField("ItemTypes");
                    EditorGUI.indentLevel += 1;

                    for (int i = 0; i < Target.m_ItemTypes.Length; i++)
                    {
                        int selected = EditorGUILayout.Popup(GetSelectedItemType(Target.m_ItemTypes[i]), m_ItemTypes);

                        Target.m_ItemTypes[i] = ItemDataList.Instance.m_ItemTypes[selected].m_Guid;
                    }

                    EditorGUI.indentLevel -= 1;
                }
            }

            private int GetSelectedItemType(string guid)
            {
                for (int i = 0; i < ItemDataList.Instance.m_ItemTypes.Count; i++)
                {
                    if (ItemDataList.Instance.m_ItemTypes[i].m_Guid.Equals(guid))
                    {
                        return i;
                    }
                }
                return 0;
            }
        }
    }
}
