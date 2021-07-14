using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Syadeu;
using Syadeu.Database;
using SyadeuEditor.Tree;
using UnityEditor;
using UnityEngine;

#if UNITY_ADDRESSABLES
using UnityEditor.AddressableAssets;
using UnityEngine.AddressableAssets;
#endif

namespace SyadeuEditor
{
    [CustomEditor(typeof(ItemDataList))]
    public sealed class ItemDataListEditor : EditorEntity<ItemDataList>
    {
        private static VerticalTreeView m_TreeView;
        private VerticalTreeView TreeView
        {
            get
            {
                if (m_TreeView == null)
                {
                    Asset.LoadData();

                    List<object> tempList = new List<object>();
                    tempList.AddRange(Asset.m_Items);
                    tempList.AddRange(Asset.m_ItemTypes);
                    tempList.AddRange(Asset.m_ItemEffectTypes);

                    m_TreeView = new VerticalTreeView(Asset, serializedObject);
                    m_TreeView.OnDirty += RefreshTreeView;
                    m_TreeView
                        .SetupElements(tempList, (other) =>
                        {
                            if (other is Item item)
                            {
                                return new TreeItemElement(TreeView, item);
                            }
                            else if (other is ItemTypeEntity type)
                            {
                                return new TreeItemTypeElement(TreeView, type);
                            }
                            else if (other is ItemEffectType effectType)
                            {
                                return new TreeItemEffectTypeElement(TreeView, effectType);
                            }
                            throw new Exception();
                        })
                        .MakeAddButton(() =>
                        {
                            if (TreeView.SelectedToolbar == 0) Asset.m_Items.Add(new Item());
                            else if (TreeView.SelectedToolbar == 1)
                            {
                                GenericMenu typeMenu = new GenericMenu();
                                typeMenu.AddItem(new GUIContent("Common"), false, () =>
                                {
                                    Asset.m_ItemTypes.Add(new ItemType());

                                    RefreshTreeView();
                                });
                                typeMenu.AddItem(new GUIContent("Useable"), false, () =>
                                {
                                    Asset.m_ItemTypes.Add(new ItemUseableType());

                                    RefreshTreeView();
                                });
                                Rect rect = GUILayoutUtility.GetLastRect();
                                rect.position = Event.current.mousePosition;
                                typeMenu.DropDown(rect);
                            }
                            else if (TreeView.SelectedToolbar == 2) Asset.m_ItemEffectTypes.Add(new ItemEffectType());

                            List<object> tempList = new List<object>();
                            tempList.AddRange(Asset.m_Items);
                            tempList.AddRange(Asset.m_ItemTypes);
                            tempList.AddRange(Asset.m_ItemEffectTypes);
                            return tempList;
                        })
                        .MakeRemoveButton((idx) =>
                        {
                            if (TreeView.SelectedToolbar == 0)
                            {
                                Asset.m_Items.Remove((Item)TreeView.Data[idx]);
                            }
                            else if (TreeView.SelectedToolbar == 1)
                            {
                                Asset.m_ItemTypes.Remove((ItemTypeEntity)TreeView.Data[idx]);
                            }
                            else if (TreeView.SelectedToolbar == 2)
                            {
                                Asset.m_ItemEffectTypes.Remove((ItemEffectType)TreeView.Data[idx]);
                            }

                            List<object> tempList = new List<object>();
                            tempList.AddRange(Asset.m_Items);
                            tempList.AddRange(Asset.m_ItemTypes);
                            tempList.AddRange(Asset.m_ItemEffectTypes);
                            return tempList;
                        })
                        .MakeToolbar("Items", "Types", "EffectTypes")
                        .MakeCustomSearchFilter((e, searchTxt) =>
                        {
                            string name = "", hash = "";
                            if (e is TreeItemElement itemEle)
                            {
                                name = itemEle.Target.m_Name;
                                hash = itemEle.Target.m_Hash.ToString();
                            }
                            else if (e is TreeItemTypeElement typeEle)
                            {
                                name = typeEle.Target.m_Name;
                                hash = typeEle.Target.m_Hash.ToString();
                            }
                            else if (e is TreeItemEffectTypeElement effEle)
                            {
                                name = effEle.Target.m_Name;
                                hash = effEle.Target.m_Hash.ToString();
                            }

                            if (name.ToLower().Contains(searchTxt.ToLower()) || hash.Contains(searchTxt)) return true;
                            return false;
                        });
                }
                return m_TreeView;
            }
        }

        private bool m_ShowOriginalContents = false;

        private void OnEnable()
        {
            m_TreeView = null;

            Asset.m_Items?.Clear();
            Asset.m_ItemTypes?.Clear();
            Asset.m_ItemEffectTypes?.Clear();
            EditorUtils.SetDirty(Asset);
        }
        private void RefreshTreeView()
        {
            List<object> tempList = new List<object>();
            if (Asset.m_Items != null) tempList.AddRange(Asset.m_Items);
            if (Asset.m_ItemTypes != null) tempList.AddRange(Asset.m_ItemTypes);
            if (Asset.m_ItemEffectTypes != null) tempList.AddRange(Asset.m_ItemEffectTypes);
            m_TreeView.Refresh(tempList);
        }
        //private void OnValidate()
        //{
        //    RefreshTreeView();
        //}

        public override void OnInspectorGUI()
        {
            if (m_TreeView == null)
            {
                Asset.LoadData();
                EditorUtils.SetDirty(Asset);
                //RefreshTreeView();

                List<object> tempList = new List<object>();
                if (Asset.m_Items != null) tempList.AddRange(Asset.m_Items);
                if (Asset.m_ItemTypes != null) tempList.AddRange(Asset.m_ItemTypes);
                if (Asset.m_ItemEffectTypes != null) tempList.AddRange(Asset.m_ItemEffectTypes);
                TreeView.Refresh();
            }

            EditorUtils.StringHeader("Item Data");
            EditorUtils.SectorLine();

            if (GUILayout.Button("Clear"))
            {
                Asset.m_Items.Clear();
                Asset.m_ItemTypes.Clear();
                Asset.m_ItemEffectTypes.Clear();
                EditorUtils.SetDirty(Asset);
                RefreshTreeView();
            }
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load"))
            {
                Asset.LoadData();
                EditorUtils.SetDirty(Asset);
                RefreshTreeView();
            }
            if (GUILayout.Button("Save"))
            {
                EditorUtils.SetDirty(Asset);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Asset.SaveData();
            }
            EditorGUILayout.EndHorizontal();
            EditorUtils.SectorLine();
            EditorGUILayout.Space();

            TreeView.OnGUI();

            EditorGUILayout.Space();
            m_ShowOriginalContents = EditorUtils.Foldout(m_ShowOriginalContents, "Original Contents");
            if (m_ShowOriginalContents) base.OnInspectorGUI();
        }

        private class TreeItemElement : VerticalTreeElement<Item>
        {
            public override string Name => Target.m_Name;
            public override bool HideElementInTree
                => Tree.SelectedToolbar != 0 || base.HideElementInTree;

            public TreeItemElement(VerticalTreeView treeView, Item item) : base(treeView, item) { }
            public override void OnGUI()
            {
                Target.DrawItem();
            }
        }
        private class TreeItemTypeElement : VerticalTreeElement<ItemTypeEntity>
        {
            public override string Name => Target.m_Name;
            public override bool HideElementInTree
                => Tree.SelectedToolbar != 1 || base.HideElementInTree;

            public TreeItemTypeElement(VerticalTreeView treeView, ItemTypeEntity type) : base(treeView, type) { }
            public override void OnGUI()
            {
                Target.DrawItemType();
            }
        }
        private class TreeItemEffectTypeElement : VerticalTreeElement<ItemEffectType>
        {
            public override string Name => Target.m_Name;
            public override bool HideElementInTree
                => Tree.SelectedToolbar != 2 || base.HideElementInTree;

            public TreeItemEffectTypeElement(VerticalTreeView treeView, ItemEffectType effectType) : base(treeView, effectType) { }
            public override void OnGUI()
            {
                Target.DrawItemEffectType();
            }
        }
    }
}
