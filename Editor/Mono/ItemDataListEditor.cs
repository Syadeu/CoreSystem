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

        private bool m_ShowOriginalContents = false;

        private static string Path => $"{Application.dataPath}/{ItemDataList.c_ItemDataPath}";
        public static string TypePath => $"{Path}/ItemTypes";
        public static string EffectPath => $"{Path}/ItemEffects";

        private void OnEnable()
        {
            if (!Directory.Exists(Path)) Directory.CreateDirectory(Path);
            if (!Directory.Exists(TypePath)) Directory.CreateDirectory(TypePath);
            if (!Directory.Exists(EffectPath)) Directory.CreateDirectory(EffectPath);

            m_TreeView = new VerticalTreeView(Asset);
            m_TreeView
                .SetupElements(Asset.m_Items, (other) =>
                {
                    Item item = (Item)other;

                    return new TreeItemElement(m_TreeView, item);
                })
                ;
        }

        public override void OnInspectorGUI()
        {
            EditorUtils.StringHeader("Item Datas");
            EditorUtils.SectorLine();

            if (GUILayout.Button("Clear"))
            {
                Asset.m_Items = new Item[0];
                Asset.m_ItemTypes = new ItemType[0];
                Asset.m_ItemEffectTypes = new ItemEffectType[0];
                EditorUtils.SetDirty(target);
            }
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField(ItemDataList.c_ItemDataPath);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load"))
            {
                Asset.LoadDatas();
                EditorUtils.SetDirty(target);
            }
            if (GUILayout.Button("Save"))
            {
                Asset.SaveDatas();
                EditorUtils.SetDirty(target);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            m_TreeView.OnGUI();

            EditorGUILayout.Space();
            m_ShowOriginalContents = EditorUtils.Foldout(m_ShowOriginalContents, "Original Contents");
            if (m_ShowOriginalContents) base.OnInspectorGUI();
        }

        private class TreeItemElement : VerticalTreeElement<Item>
        {
            public override string Name => Target.m_Name;

            public TreeItemElement(VerticalTreeView treeView, Item item) : base(treeView, item)
            {
            }

            public override void OnGUI()
            {
            }
        }
    }
}
