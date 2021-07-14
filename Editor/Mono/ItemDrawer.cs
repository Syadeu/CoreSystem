using System.Linq;
using Syadeu.Database;
using UnityEditor;
using UnityEngine;

#if CORESYSTEM_GOOGLE
using Google.Apis.Sheets.v4.Data;
#endif

using Syadeu;

#if UNITY_ADDRESSABLES

#endif

namespace SyadeuEditor
{
    public sealed class ItemDrawer
    {
        internal Item m_Item;

        public ItemDrawer(Item item)
        {
            m_Item = item;
        }
        public void OnGUI()
        {
            ItemEditor.Validate();

            //AddressableAssetSettingsDefaultObject.GetSettings(true).FindGroup("Images").GetAssetEntry(item.m_ImagePath)
            m_Item.m_Name = EditorGUILayout.TextField("Name: ", m_Item.m_Name);
            EditorGUILayout.TextField("Hash: ", m_Item.m_Hash.ToString());

#if UNITY_ADDRESSABLES
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Image: ");
            ItemEditor.DrawAssetReference(m_Item, m_Item.m_ImagePath);
            EditorGUILayout.EndHorizontal();
            m_Item.m_PrefabIdx = PrefabListEditor.DrawPrefabSelector(m_Item.m_PrefabIdx);
            EditorUtils.Line();
#endif
            #region ItemTypes
            using (new EditorGUILayout.VerticalScope(EditorUtils.Box))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorUtils.StringHeader("ItemTypes", 15);
                    if (GUILayout.Button("+", GUILayout.Width(20)))
                    {
                        var temp = m_Item.m_ItemTypes.ToList();
                        temp.Add(0);
                        m_Item.m_ItemTypes = temp.ToArray();
                    }
                }

                EditorGUI.indentLevel += 1;
                for (int i = 0; i < m_Item.m_ItemTypes.Length; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUI.BeginChangeCheck();
                        int tSelected = EditorGUILayout.Popup(GetSelectedItemType(m_Item.m_ItemTypes[i]), ItemEditor.m_ItemTypes);
                        if (EditorGUI.EndChangeCheck())
                        {
                            if (tSelected == 0) m_Item.m_ItemTypes[i] = 0;
                            else
                            {
                                ItemTypeEntity selectedItemType = ItemDataList.Instance.m_ItemTypes[tSelected - 1];

                                if (m_Item.m_ItemTypes.Where((other) => ItemDataList.Instance.GetItemType(other) is ItemUseableType).Any() &&
                                    selectedItemType is ItemUseableType)
                                {
                                    $"이 타입은 한 개 이상 존재할 수 없습니다.".ToLog();
                                }
                                else if (m_Item.m_ItemTypes.Contains(selectedItemType.m_Hash))
                                {
                                    $"이미 해당 타입을 포함하고 있습니다.".ToLog();
                                }
                                else m_Item.m_ItemTypes[i] = ItemDataList.Instance.m_ItemTypes[tSelected - 1].m_Hash;
                            }
                        }

                        if (GUILayout.Button("-", GUILayout.Width(20)))
                        {
                            var temp = m_Item.m_ItemTypes.ToList();
                            temp.RemoveAt(i);
                            m_Item.m_ItemTypes = temp.ToArray();
                            i--;
                        }
                    }
                }
                EditorGUI.indentLevel -= 1;
            }
            #endregion
            EditorUtils.Line();
            #region ItemEffects
            using (new EditorGUILayout.VerticalScope(EditorUtils.Box))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorUtils.StringHeader("ItemEffects", 15);
                    if (GUILayout.Button("+", GUILayout.Width(20)))
                    {
                        var temp = m_Item.m_ItemEffectTypes.ToList();
                        temp.Add(0);
                        m_Item.m_ItemEffectTypes = temp.ToArray();
                    }
                }

                EditorGUI.indentLevel += 1;
                for (int i = 0; i < m_Item.m_ItemEffectTypes.Length; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUI.BeginChangeCheck();
                        int teSelected = EditorGUILayout.Popup(GetSelectedItemEffectType(m_Item.m_ItemEffectTypes[i]), ItemEditor.m_ItemEffectTypes);
                        if (EditorGUI.EndChangeCheck())
                        {
                            if (teSelected == 0) m_Item.m_ItemEffectTypes[i] = 0;
                            else
                            {
                                ItemEffectType selectedEffectType = ItemDataList.Instance.m_ItemEffectTypes[teSelected - 1];

                                if (m_Item.m_ItemEffectTypes.Contains(selectedEffectType.m_Hash))
                                {
                                    $"이미 해당 타입을 포함하고 있습니다.".ToLog();
                                }
                                else m_Item.m_ItemEffectTypes[i] = ItemDataList.Instance.m_ItemEffectTypes[teSelected - 1].m_Hash;
                            }
                        }

                        if (GUILayout.Button("-", GUILayout.Width(20)))
                        {
                            var temp = m_Item.m_ItemEffectTypes.ToList();
                            temp.RemoveAt(i);
                            m_Item.m_ItemEffectTypes = temp.ToArray();
                            i--;
                        }
                    }
                }
                EditorGUI.indentLevel -= 1;
            }
            #endregion
            EditorUtils.Line();
            m_Item.m_Values.DrawValueContainer("Values");

            int GetSelectedItemType(ulong hash)
            {
                if (hash == 0) return 0;
                for (int i = 0; i < ItemDataList.Instance.m_ItemTypes.Count; i++)
                {
                    if (ItemDataList.Instance.m_ItemTypes[i].m_Hash.Equals(hash))
                    {
                        return i + 1;
                    }
                }
                return 0;
            }
            int GetSelectedItemEffectType(ulong hash)
            {
                if (hash == 0) return 0;
                for (int i = 0; i < ItemDataList.Instance.m_ItemEffectTypes.Count; i++)
                {
                    if (ItemDataList.Instance.m_ItemEffectTypes[i].m_Hash.Equals(hash))
                    {
                        return i + 1;
                    }
                }
                return 0;
            }
        }
    }
}
