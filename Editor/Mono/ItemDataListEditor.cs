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
        private VerticalTreeView m_TreeView;

        private static string[] m_ItemTypes = new string[0];
        private static string[] m_ItemEffectTypes = new string[0];

        private bool m_ShowOriginalContents = false;

        private void OnEnable()
        {
            OnValidate();
        }
        private void OnValidate()
        {
            List<object> tempList = new List<object>();
            tempList.AddRange(Asset.m_Items);
            tempList.AddRange(Asset.m_ItemTypes);
            tempList.AddRange(Asset.m_ItemEffectTypes);

            m_TreeView = new VerticalTreeView(Asset, serializedObject);
            m_TreeView
                .SetupElements(tempList, (other) =>
                {
                    if (other is Item item)
                    {
                        return new TreeItemElement(m_TreeView, item);
                    }
                    else if (other is ItemType type)
                    {
                        return new TreeItemTypeElement(m_TreeView, type);
                    }
                    else if (other is ItemEffectType effectType)
                    {
                        return new TreeItemEffectTypeElement(m_TreeView, effectType);
                    }
                    throw new Exception();
                })
                .MakeAddButton(() =>
                {
                    if (m_TreeView.SelectedToolbar == 0) Asset.m_Items.Add(new Item());
                    else if (m_TreeView.SelectedToolbar == 1) Asset.m_ItemTypes.Add(new ItemType());
                    else if (m_TreeView.SelectedToolbar == 2) Asset.m_ItemEffectTypes.Add(new ItemEffectType());

                    List<object> tempList = new List<object>();
                    tempList.AddRange(Asset.m_Items);
                    tempList.AddRange(Asset.m_ItemTypes);
                    tempList.AddRange(Asset.m_ItemEffectTypes);
                    return tempList;
                })
                .MakeRemoveButton((idx) =>
                {
                    if (m_TreeView.SelectedToolbar == 0)
                    {
                        Asset.m_Items.Remove((Item)m_TreeView.Data[idx]);
                    }
                    else if (m_TreeView.SelectedToolbar == 1)
                    {
                        Asset.m_ItemTypes.Remove((ItemType)m_TreeView.Data[idx]);
                    }
                    else if (m_TreeView.SelectedToolbar == 2)
                    {
                        Asset.m_ItemEffectTypes.Remove((ItemEffectType)m_TreeView.Data[idx]);
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
                    string name = "", guid = "";
                    if (e is TreeItemElement itemEle)
                    {
                        name = itemEle.Target.m_Name;
                        guid = itemEle.Target.m_Guid;
                    }
                    else if (e is TreeItemTypeElement typeEle)
                    {
                        name = typeEle.Target.m_Name;
                        guid = typeEle.Target.m_Guid;
                    }
                    else if (e is TreeItemEffectTypeElement effEle)
                    {
                        name = effEle.Target.m_Name;
                        guid = effEle.Target.m_Guid;
                    }

                    if (name.ToLower().Contains(searchTxt.ToLower()) || guid.Contains(searchTxt)) return true;
                    return false;
                });

            m_ItemTypes = new string[ItemDataList.Instance.m_ItemTypes.Count + 1];
            m_ItemTypes[0] = "None";
            for (int i = 1; i < m_ItemTypes.Length; i++)
            {
                m_ItemTypes[i] = ItemDataList.Instance.m_ItemTypes[i - 1].m_Name;
            }

            m_ItemEffectTypes = new string[ItemDataList.Instance.m_ItemEffectTypes.Count + 1];
            m_ItemEffectTypes[0] = "None";
            for (int i = 1; i < m_ItemEffectTypes.Length; i++)
            {
                m_ItemEffectTypes[i] = ItemDataList.Instance.m_ItemEffectTypes[i - 1].m_Name;
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
                EditorUtils.SetDirty(Asset);
                OnValidate();
            }
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load"))
            {
                Asset.LoadDatas();
                EditorUtils.SetDirty(Asset);
                OnValidate();
            }
            if (GUILayout.Button("Save"))
            {
                Asset.SaveDatas();
                EditorUtils.SetDirty(Asset);
                OnValidate();
            }
            EditorGUILayout.EndHorizontal();
            EditorUtils.SectorLine();
            EditorGUILayout.Space();

            m_TreeView.OnGUI();

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

                ////AddressableAssetSettingsDefaultObject.GetSettings(true).FindGroup("Images").GetAssetEntry(Target.m_ImagePath)
                //Target.m_Name = EditorGUILayout.TextField("Name: ", Target.m_Name);
                //EditorGUILayout.TextField("Guid: ", Target.m_Guid);
                ////Target.m_ImagePath = EditorGUILayout.TextField("Image Path: ", Target.m_ImagePath);
                


                //using (new EditorGUILayout.VerticalScope("Box"))
                //{
                //    using (new EditorGUILayout.HorizontalScope())
                //    {
                //        EditorUtils.StringHeader("ItemTypes", 15);
                //        if (GUILayout.Button("+", GUILayout.Width(20)))
                //        {
                //            var temp = Target.m_ItemTypes.ToList();
                //            temp.Add("");
                //            Target.m_ItemTypes = temp.ToArray();
                //        }
                //    }
                    
                //    EditorGUI.indentLevel += 1;
                //    for (int i = 0; i < Target.m_ItemTypes.Length; i++)
                //    {
                //        using(new EditorGUILayout.HorizontalScope())
                //        {
                //            int tSelected = EditorGUILayout.Popup(GetSelectedItemType(Target.m_ItemTypes[i]), m_ItemTypes);
                //            Target.m_ItemTypes[i] = tSelected == 0 ? "" : ItemDataList.Instance.m_ItemTypes[tSelected - 1].m_Guid;

                //            if (GUILayout.Button("-", GUILayout.Width(20)))
                //            {
                //                var temp = Target.m_ItemTypes.ToList();
                //                temp.RemoveAt(i);
                //                Target.m_ItemTypes = temp.ToArray();
                //                i--;
                //            }
                //        }
                //    }
                //    EditorGUI.indentLevel -= 1;
                //}

                //using (new EditorGUILayout.VerticalScope("Box"))
                //{
                //    using (new EditorGUILayout.HorizontalScope())
                //    {
                //        EditorUtils.StringHeader("ItemEffects", 15);
                //        if (GUILayout.Button("+", GUILayout.Width(20)))
                //        {
                //            var temp = Target.m_ItemEffectTypes.ToList();
                //            temp.Add("");
                //            Target.m_ItemEffectTypes = temp.ToArray();
                //        }
                //    }
                    
                //    EditorGUI.indentLevel += 1;
                //    for (int i = 0; i < Target.m_ItemEffectTypes.Length; i++)
                //    {
                //        using (new EditorGUILayout.HorizontalScope())
                //        {
                //            int teSelected = EditorGUILayout.Popup(GetSelectedItemEffectType(Target.m_ItemEffectTypes[i]), m_ItemEffectTypes);
                //            Target.m_ItemEffectTypes[i] = teSelected == 0 ? "" : ItemDataList.Instance.m_ItemEffectTypes[teSelected - 1].m_Guid;

                //            if (GUILayout.Button("-", GUILayout.Width(20)))
                //            {
                //                var temp = Target.m_ItemEffectTypes.ToList();
                //                temp.RemoveAt(i);
                //                Target.m_ItemEffectTypes = temp.ToArray();
                //                i--;
                //            }
                //        }
                //    }
                //    EditorGUI.indentLevel -= 1;
                //}

                //Target.m_Values.DrawValueContainer();
            }

            private int GetSelectedItemType(string guid)
            {
                if (string.IsNullOrEmpty(guid)) return 0;
                for (int i = 0; i < ItemDataList.Instance.m_ItemTypes.Count; i++)
                {
                    if (ItemDataList.Instance.m_ItemTypes[i].m_Guid.Equals(guid))
                    {
                        return i + 1;
                    }
                }
                return 0;
            }
            private int GetSelectedItemEffectType(string guid)
            {
                if (string.IsNullOrEmpty(guid)) return 0;
                for (int i = 0; i < ItemDataList.Instance.m_ItemEffectTypes.Count; i++)
                {
                    if (ItemDataList.Instance.m_ItemEffectTypes[i].m_Guid.Equals(guid))
                    {
                        return i + 1;
                    }
                }
                return 0;
            }
        }
        private class TreeItemTypeElement : VerticalTreeElement<ItemType>
        {
            public override string Name => Target.m_Name;
            public override bool HideElementInTree
                => Tree.SelectedToolbar != 1 || base.HideElementInTree;

            public TreeItemTypeElement(VerticalTreeView treeView, ItemType type) : base(treeView, type) { }
            public override void OnGUI()
            {
                Target.m_Name = EditorGUILayout.TextField("Name: ", Target.m_Name);
                EditorGUILayout.TextField("Guid: ", Target.m_Guid);

                EditorGUILayout.Space();
                Target.m_Values.DrawValueContainer();
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
                Target.m_Name = EditorGUILayout.TextField("Name: ", Target.m_Name);
                EditorGUILayout.TextField("Guid: ", Target.m_Guid);

                EditorGUILayout.Space();
                Target.m_Values.DrawValueContainer();
            }
        }
    }
}
