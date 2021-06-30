using System;
using System.Collections.Generic;
using System.Linq;
using Syadeu.Database;
using UnityEditor;

using UnityEditor.IMGUI.Controls;
using UnityEngine;

#if CORESYSTEM_GOOGLE
using Google.Apis.Sheets.v4.Data;
#endif

#if UNITY_ADDRESSABLES
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine.AddressableAssets;
using Syadeu;
#endif

namespace SyadeuEditor
{
    public static class ItemEditor
    {
        private static string[] m_ItemTypes = new string[0];
        private static string[] m_ItemEffectTypes = new string[0];

#if UNITY_ADDRESSABLES
        private static readonly AddressableAssetSettings m_DefaultAddressableSettings = AddressableAssetSettingsDefaultObject.GetSettings(true);
#endif

        static ItemEditor()
        {
            Validate();
        }
        private static void Validate()
        {
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

        public static void DrawItem(this Item item)
        {
            const string c_Box = "Box";
            Validate();

            //AddressableAssetSettingsDefaultObject.GetSettings(true).FindGroup("Images").GetAssetEntry(item.m_ImagePath)
            item.m_Name = EditorGUILayout.TextField("Name: ", item.m_Name);
            EditorGUILayout.TextField("Guid: ", item.m_Guid);

#if UNITY_ADDRESSABLES
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Image: ");
            DrawAssetReference(item, item.m_ImagePath);
            EditorGUILayout.EndHorizontal();
            EditorUtils.Line();
#endif
            #region ItemTypes
            using (new EditorGUILayout.VerticalScope(c_Box))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorUtils.StringHeader("ItemTypes", 15);
                    if (GUILayout.Button("+", GUILayout.Width(20)))
                    {
                        var temp = item.m_ItemTypes.ToList();
                        temp.Add("");
                        item.m_ItemTypes = temp.ToArray();
                    }
                }

                EditorGUI.indentLevel += 1;
                for (int i = 0; i < item.m_ItemTypes.Length; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUI.BeginChangeCheck();
                        int tSelected = EditorGUILayout.Popup(GetSelectedItemType(item.m_ItemTypes[i]), m_ItemTypes);
                        if (EditorGUI.EndChangeCheck())
                        {
                            if (tSelected == 0) item.m_ItemTypes[i] = "";
                            else
                            {
                                ItemTypeEntity selectedItemType = ItemDataList.Instance.m_ItemTypes[tSelected - 1];

                                if (item.m_ItemTypes.Where((other) => ItemDataList.Instance.GetItemType(other) is ItemUseableType).Count() != 0 &&
                                    selectedItemType is ItemUseableType)
                                {
                                    $"이 타입은 한 개 이상 존재할 수 없습니다.".ToLog();
                                }
                                else if (item.m_ItemTypes.Contains(selectedItemType.m_Guid))
                                {
                                    $"이미 해당 타입을 포함하고 있습니다.".ToLog();
                                }
                                else item.m_ItemTypes[i] = ItemDataList.Instance.m_ItemTypes[tSelected - 1].m_Guid;
                            }
                        }

                        if (GUILayout.Button("-", GUILayout.Width(20)))
                        {
                            var temp = item.m_ItemTypes.ToList();
                            temp.RemoveAt(i);
                            item.m_ItemTypes = temp.ToArray();
                            i--;
                        }
                    }
                }
                EditorGUI.indentLevel -= 1;
            }
            #endregion
            EditorUtils.Line();
            #region ItemEffects
            using (new EditorGUILayout.VerticalScope(c_Box))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorUtils.StringHeader("ItemEffects", 15);
                    if (GUILayout.Button("+", GUILayout.Width(20)))
                    {
                        var temp = item.m_ItemEffectTypes.ToList();
                        temp.Add("");
                        item.m_ItemEffectTypes = temp.ToArray();
                    }
                }

                EditorGUI.indentLevel += 1;
                for (int i = 0; i < item.m_ItemEffectTypes.Length; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUI.BeginChangeCheck();
                        int teSelected = EditorGUILayout.Popup(GetSelectedItemEffectType(item.m_ItemEffectTypes[i]), m_ItemEffectTypes);
                        if (EditorGUI.EndChangeCheck())
                        {
                            if (teSelected == 0) item.m_ItemEffectTypes[i] = "";
                            else
                            {
                                ItemEffectType selectedEffectType = ItemDataList.Instance.m_ItemEffectTypes[teSelected - 1];

                                if (item.m_ItemEffectTypes.Contains(selectedEffectType.m_Guid))
                                {
                                    $"이미 해당 타입을 포함하고 있습니다.".ToLog();
                                }
                                else item.m_ItemEffectTypes[i] = ItemDataList.Instance.m_ItemEffectTypes[teSelected - 1].m_Guid;
                            }
                        }

                        if (GUILayout.Button("-", GUILayout.Width(20)))
                        {
                            var temp = item.m_ItemEffectTypes.ToList();
                            temp.RemoveAt(i);
                            item.m_ItemEffectTypes = temp.ToArray();
                            i--;
                        }
                    }
                }
                EditorGUI.indentLevel -= 1;
            }
            #endregion
            EditorUtils.Line();
            item.m_Values.DrawValueContainer("Values");
            
            int GetSelectedItemType(string guid)
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
            int GetSelectedItemEffectType(string guid)
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

#if UNITY_ADDRESSABLES
            static void DrawAssetReference(Item item, AssetReference refAsset)
            {
                float iconHeight = EditorGUIUtility.singleLineHeight - EditorGUIUtility.standardVerticalSpacing * 3;
                Vector2 iconSize = EditorGUIUtility.GetIconSize();
                EditorGUIUtility.SetIconSize(new Vector2(iconHeight, iconHeight));
                string assetPath = AssetDatabase.GUIDToAssetPath(refAsset?.AssetGUID);
                Texture2D assetIcon = AssetDatabase.GetCachedIcon(assetPath) as Texture2D;

                var entry = refAsset == null ? null : m_DefaultAddressableSettings.FindAssetEntry(refAsset?.AssetGUID);
                string displayName = entry == null ? "Not Found" : entry.address.Split('/').Last();
                //Rect rect = GUILayoutUtility.GetLastRect();
                Rect rect = rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                rect = EditorGUI.IndentedRect(rect);
                //rect.width = EditorGUIUtility.currentViewWidth

                if (EditorGUI.DropdownButton(rect, new GUIContent(displayName, assetIcon), FocusType.Passive, new GUIStyle("ObjectField")))
                {
                    rect = GUILayoutUtility.GetLastRect();
                    rect.position = Event.current.mousePosition;

                    PopupWindow.Show(rect, 
                        new AssetReferencePopup(item, refAsset?.AssetGUID, displayName));
                }

                EditorGUIUtility.SetIconSize(iconSize);
            }
#endif
        }

#if UNITY_ADDRESSABLES
        class AssetReferencePopup : PopupWindowContent
        {
            Item m_Item;
            AssetReferenceTreeView m_Tree;
            TreeViewState m_TreeState;
            bool m_ShouldClose;

            void ForceClose()
            {
                m_ShouldClose = true;
            }

            string m_CurrentName = string.Empty;
            string m_GUID;
            string m_NonAddressedAsset;

            SearchField m_SearchField;

            internal AssetReferencePopup(Item item, string guid, string nonAddressedAsset)
            {
                m_Item = item;
                m_GUID = guid;
                m_NonAddressedAsset = nonAddressedAsset;
                m_SearchField = new SearchField();
                m_ShouldClose = false;
            }

            public override void OnOpen()
            {
                m_SearchField.SetFocus();
                base.OnOpen();
            }

            public override Vector2 GetWindowSize()
            {
                Vector2 result = base.GetWindowSize();
                result.x += 40;
                return result;
            }

            public override void OnGUI(Rect rect)
            {
                int border = 4;
                int topPadding = 12;
                int searchHeight = 20;
                var searchRect = new Rect(border, topPadding, rect.width - border * 2, searchHeight);
                var remainTop = topPadding + searchHeight + border;
                var remainingRect = new Rect(border, topPadding + searchHeight + border, rect.width - border * 2, rect.height - remainTop - border);
                m_CurrentName = m_SearchField.OnGUI(searchRect, m_CurrentName);

                if (m_Tree == null)
                {
                    if (m_TreeState == null)
                        m_TreeState = new TreeViewState();
                    m_Tree = new AssetReferenceTreeView(m_TreeState, this, m_GUID, m_NonAddressedAsset);
                    m_Tree.Reload();
                }

                m_Tree.searchString = m_CurrentName;
                m_Tree.OnGUI(remainingRect);

                if (m_ShouldClose)
                {
                    GUIUtility.hotControl = 0;
                    editorWindow.Close();
                }
            }

            sealed class AssetRefTreeViewItem : TreeViewItem
            {
                public string AssetPath;

                private string m_Guid;
                public string Guid
                {
                    get
                    {
                        if (string.IsNullOrEmpty(m_Guid))
                            m_Guid = AssetDatabase.AssetPathToGUID(AssetPath);
                        return m_Guid;
                    }
                }

                public AssetRefTreeViewItem(int id, int depth, string displayName, string path)
                    : base(id, depth, displayName)
                {
                    AssetPath = path;
                    icon = AssetDatabase.GetCachedIcon(path) as Texture2D;
                }
            }

            internal class AssetReferenceTreeView : TreeView
            {
                AssetReferencePopup m_Popup;
                string m_GUID;
                string m_NonAddressedAsset;
                Texture2D m_WarningIcon;

                public AssetReferenceTreeView(TreeViewState state, AssetReferencePopup popup, string guid, string nonAddressedAsset)
                    : base(state)
                {
                    m_Popup = popup;
                    showBorder = true;
                    showAlternatingRowBackgrounds = true;
                    m_GUID = guid;
                    m_NonAddressedAsset = nonAddressedAsset;
                    m_WarningIcon = EditorGUIUtility.FindTexture("console.warnicon");
                }

                protected override bool CanMultiSelect(TreeViewItem item)
                {
                    return false;
                }

                protected override void SelectionChanged(IList<int> selectedIds)
                {
                    if (selectedIds != null && selectedIds.Count == 1)
                    {
                        var assetRefItem = FindItem(selectedIds[0], rootItem) as AssetRefTreeViewItem;
                        if (assetRefItem != null && !string.IsNullOrEmpty(assetRefItem.AssetPath))
                        {
                            //m_Drawer.newGuid = assetRefItem.Guid;
                            m_Popup.m_Item.m_ImagePath = new AssetReferenceSprite(assetRefItem.Guid);
                        }
                        else
                        {
                            //m_Drawer.newGuid = AssetReferenceDrawer.noAssetString;
                            m_Popup.m_Item.m_ImagePath = null;
                        }

                        m_Popup.ForceClose();
                    }
                }

                protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
                {
                    if (string.IsNullOrEmpty(searchString))
                    {
                        return base.BuildRows(root);
                    }

                    List<TreeViewItem> rows = new List<TreeViewItem>();

                    foreach (var child in rootItem.children)
                    {
                        if (child.displayName.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                            rows.Add(child);
                    }

                    return rows;
                }

                internal const string noAssetString = "None (AddressableAsset)";
                protected override TreeViewItem BuildRoot()
                {
                    var root = new TreeViewItem(-1, -1);

                    var aaSettings = AddressableAssetSettingsDefaultObject.Settings;
                    if (aaSettings == null)
                    {
                        var message = "Use 'Window->Addressables' to initialize.";
                        root.AddChild(new AssetRefTreeViewItem(message.GetHashCode(), 0, message, string.Empty));
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(m_NonAddressedAsset))
                        {
                            var item = new AssetRefTreeViewItem(m_NonAddressedAsset.GetHashCode(), 0,
                                "Make Addressable - " + m_NonAddressedAsset, string.Empty);
                            item.icon = m_WarningIcon;
                            root.AddChild(item);
                        }

                        root.AddChild(new AssetRefTreeViewItem(noAssetString.GetHashCode(), 0, noAssetString, string.Empty));
                        var allAssets = new List<AddressableAssetEntry>();
                        aaSettings.GetAllAssets(allAssets, false);
                        foreach (var entry in allAssets)
                        {
                            if (!entry.IsInResources 
                                /*&& m_Drawer.ValidateAsset(entry.AssetPath)*/)
                            {
                                var child = new AssetRefTreeViewItem(entry.AssetPath.GetHashCode(), 0, entry.address, entry.AssetPath);
                                root.AddChild(child);
                            }
                        }
                    }

                    return root;
                }
            }
        }
#endif
    }
}
