using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Google.Apis.Sheets.v4.Data;
using Syadeu;
using Syadeu.Database;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SyadeuEditor
{
    public static class ValuePairEditor
    {
        public static void DrawValueContainer(this ValuePairContainer container
#if CORESYSTEM_GOOGLE
            , string syncSheetName = null
#endif
            )
        {
            using (new EditorGUILayout.VerticalScope("Box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorUtils.StringHeader("Values", 15);
#if CORESYSTEM_GOOGLE
                    if (!string.IsNullOrEmpty(syncSheetName) && GUILayout.Button("Sync", GUILayout.Width(50)))
                    {
                        container.SyncWithGoogleSheet(
                            container.Contains("Index") ? (int)container.GetValue("Index") : 1, syncSheetName);
                    }
#endif
                    if (GUILayout.Button("+", GUILayout.Width(20)))
                    {
                        GenericMenu typeMenu = new GenericMenu();
                        typeMenu.AddItem(new GUIContent("Int"), false, () =>
                        {
                            container.Add<int>("New Int Value", 0);
                        });
                        typeMenu.AddItem(new GUIContent("Double"), false, () =>
                        {
                            container.Add<double>("New Double Value", 0);
                        });
                        typeMenu.AddItem(new GUIContent("String"), false, () =>
                        {
                            container.Add<string>("New String Value", "");
                        });
                        typeMenu.AddItem(new GUIContent("Bool"), false, () =>
                        {
                            container.Add<bool>("New Bool Value", false);
                        });
                        typeMenu.AddItem(new GUIContent("Int Array"), false, () =>
                        {
                            container.Add<List<int>>("New Int Array", new List<int>());
                        });
                        typeMenu.AddItem(new GUIContent("Double Array"), false, () =>
                        {
                            container.Add<List<double>>("New Double Array", new List<double>());
                        });
                        typeMenu.AddItem(new GUIContent("Bool Array"), false, () =>
                        {
                            container.Add<List<bool>>("New Bool Array", new List<bool>());
                        });
                        typeMenu.AddItem(new GUIContent("String Array"), false, () =>
                        {
                            container.Add<List<string>>("New String Array", new List<string>());
                        });
                        typeMenu.AddItem(new GUIContent("Delegate"), false, () =>
                        {
                            container.Add<Action>("New Delegate Value", () => { });
                        });
                        //;
                        //GUIUtility.GUIToScreenPoint(Event.current.mousePosition)
                        //GUILayoutUtility.GetRect()
                        Rect rect = GUILayoutUtility.GetLastRect();
                        rect.position = Event.current.mousePosition;
                        //rect.width = 100; rect.height = 400;
                        typeMenu.DropDown(rect);
                    }
                }

                EditorGUI.indentLevel += 1;
                //if (Target.m_Values == null) Target.m_Values = new ValuePairContainer();
                for (int i = 0; i < container.Count; i++)
                {
                    Syadeu.Database.ValueType valueType = container[i].GetValueType();
                    if (valueType == Syadeu.Database.ValueType.Array)
                    {
                        IList list = (IList)container[i].GetValue();
                        EditorGUILayout.BeginHorizontal();
                        if (list == null || list.Count == 0)
                        {
                            container[i].m_Name = EditorGUILayout.TextField(container[i].m_Name);
                            if (GUILayout.Button("+", GUILayout.Width(20)))
                            {
                                list.Add(Activator.CreateInstance(list.GetType().GenericTypeArguments[0]));
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                        else
                        {
                            container[i].m_Name = EditorGUILayout.TextField(container[i].m_Name);
                            if (GUILayout.Button("+", GUILayout.Width(20)))
                            {
                                list.Add(Activator.CreateInstance(list.GetType().GenericTypeArguments[0]));
                            }
                            if (GUILayout.Button("-", GUILayout.Width(20)))
                            {
                                list.RemoveAt(list.Count - 1);
                            }
                            EditorGUILayout.EndHorizontal();
                            EditorGUI.indentLevel += 1;
                            for (int a = 0; a < list.Count; a++)
                            {
                                EditorGUILayout.BeginHorizontal();
                                if (list[a] is int intVal)
                                {
                                    list[a] = EditorGUILayout.IntField(intVal);
                                }
                                else if (list[a] is float floatVal)
                                {
                                    list[a] = EditorGUILayout.FloatField(floatVal);
                                }
                                else if (list[a] is bool boolVal)
                                {
                                    list[a] = EditorGUILayout.Toggle(boolVal);
                                }
                                else if (list[a] is string strVal)
                                {
                                    list[a] = EditorGUILayout.TextField(strVal);
                                }
                                if (GUILayout.Button("-", GUILayout.Width(20)))
                                {
                                    list.RemoveAt(a);
                                    a--;
                                    continue;
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                            
                            EditorGUI.indentLevel -= 1;
                        }
                        continue;
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        container[i].m_Name = EditorGUILayout.TextField(container[i].m_Name, GUILayout.Width(150));
                        switch (valueType)
                        {
                            case Syadeu.Database.ValueType.Int32:
                                int intFal = EditorGUILayout.IntField((int)container[i].GetValue());
                                if (!container[i].GetValue().Equals(intFal))
                                {
                                    container.SetValue(container[i].m_Name, intFal);
                                }
                                break;
                            case Syadeu.Database.ValueType.Double:
                                double doubleVal = EditorGUILayout.DoubleField((double)container[i].GetValue());
                                if (!container[i].GetValue().Equals(doubleVal))
                                {
                                    container.SetValue(container[i].m_Name, doubleVal);
                                }
                                break;
                            case Syadeu.Database.ValueType.String:
                                string stringVal = EditorGUILayout.TextField((string)container[i].GetValue());
                                if (!container[i].GetValue().Equals(stringVal))
                                {
                                    container.SetValue(container[i].m_Name, stringVal);
                                }
                                break;
                            case Syadeu.Database.ValueType.Boolean:
                                bool boolVal = EditorGUILayout.Toggle((bool)container[i].GetValue());
                                if (!container[i].GetValue().Equals(boolVal))
                                {
                                    container.SetValue(container[i].m_Name, boolVal);
                                }
                                break;
                            case Syadeu.Database.ValueType.Delegate:
                                EditorGUI.BeginDisabledGroup(true);
                                EditorGUILayout.TextField("Delegate");
                                EditorGUI.EndDisabledGroup();
                                break;
                            default:
                                EditorGUILayout.TextField($"{valueType}: {container[i].GetValue()}");
                                break;
                        }

                        if (GUILayout.Button("-", GUILayout.Width(20)))
                        {
                            container.RemoveAt(i);
                            i--;
                            continue;
                        }
                    }
                }
                EditorGUI.indentLevel -= 1;
            }
        }
#if CORESYSTEM_GOOGLE
        public static void SyncWithGoogleSheet(this ValuePairContainer container, int idx, string sheetName)
        {
            container.Clear();
            Sheet sheet = GoogleService.DownloadSheet(sheetName);
            container.AddRange(ToValuePairs(idx, sheet.Data[0]));

            ValuePair[] ToValuePairs(int idx, GridData data)
            {
                if (idx >= data.RowData.Count) return new ValuePair[0];

                List<string> names = new List<string>();

                for (int i = 0; i < sheet.Data[0].RowData[0].Values.Count; i++)
                {
                    if (string.IsNullOrEmpty(data.RowData[0].Values[i].FormattedValue)) break;
                    names.Add(data.RowData[0].Values[i].FormattedValue);
                }

                ValuePair[] valuePairs = new ValuePair[names.Count];
                for (int i = 0; i < valuePairs.Length; i++)
                {
                    if (1 < data.RowData.Count &&
                        i < data.RowData[/*1 + */idx].Values.Count)
                    {
                        string value = data.RowData[/*1 + */idx].Values[i].FormattedValue;

                        if (int.TryParse(value, out int intVal))
                        {
                            valuePairs[i] = ValuePair.New(names[i], intVal);
                        }
                        else if (float.TryParse(value, out float floatVal))
                        {
                            valuePairs[i] = ValuePair.New(names[i], floatVal);
                        }
                        else if (bool.TryParse(value, out bool boolVal))
                        {
                            valuePairs[i] = ValuePair.New(names[i], boolVal);
                        }
                        else
                        {
                            valuePairs[i] = ValuePair.New(names[i], value);
                        }
                    }
                }

                return valuePairs;
            }
        }
#endif
    }

    public static class ItemEditor
    {
        private static string[] m_ItemTypes = new string[0];
        private static string[] m_ItemEffectTypes = new string[0];

        static ItemEditor()
        {
            Validate();
        }
        private static void Validate()
        {
            if (m_ItemTypes.Length == ItemDataList.Instance.m_ItemTypes.Count + 1 &&
                m_ItemEffectTypes.Length == ItemDataList.Instance.m_ItemEffectTypes.Count + 1)
            {
                return;
            }

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

            DrawAssetReference(item, item.m_ImagePath);

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
                        int tSelected = EditorGUILayout.Popup(GetSelectedItemType(item.m_ItemTypes[i]), m_ItemTypes);
                        item.m_ItemTypes[i] = tSelected == 0 ? "" : ItemDataList.Instance.m_ItemTypes[tSelected - 1].m_Guid;

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
                        int teSelected = EditorGUILayout.Popup(GetSelectedItemEffectType(item.m_ItemEffectTypes[i]), m_ItemEffectTypes);
                        item.m_ItemEffectTypes[i] = teSelected == 0 ? "" : ItemDataList.Instance.m_ItemEffectTypes[teSelected - 1].m_Guid;

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

            item.m_Values.DrawValueContainer();

            
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

            static void DrawAssetReference(Item item, AssetReference refAsset)
            {
                UnityEngine.Object asset = refAsset?.editorAsset;
                var aaSettings = AddressableAssetSettingsDefaultObject.GetSettings(true);

                float iconHeight = EditorGUIUtility.singleLineHeight - EditorGUIUtility.standardVerticalSpacing * 3;
                Vector2 iconSize = EditorGUIUtility.GetIconSize();
                EditorGUIUtility.SetIconSize(new Vector2(iconHeight, iconHeight));
                string assetPath = AssetDatabase.GUIDToAssetPath(refAsset?.AssetGUID);
                Texture2D assetIcon = AssetDatabase.GetCachedIcon(assetPath) as Texture2D;

                var entry = refAsset == null ? null : aaSettings.FindAssetEntry(refAsset?.AssetGUID);
                string displayName = entry == null ? "Not Found" : entry.address.Split('/').Last();

                if (EditorGUILayout.DropdownButton(new GUIContent(displayName, assetIcon), FocusType.Passive/*, new GUIStyle("ObjectField")*/))
                {
                    Rect rect = GUILayoutUtility.GetLastRect();
                    rect.position = Event.current.mousePosition;

                    PopupWindow.Show(rect, 
                        new AssetReferencePopup(item, refAsset?.AssetGUID, displayName));
                }

                EditorGUIUtility.SetIconSize(iconSize);
            }


        }

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
                            m_Popup.m_Item.m_ImagePath = new AssetReference(assetRefItem.Guid);
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
    }
}
