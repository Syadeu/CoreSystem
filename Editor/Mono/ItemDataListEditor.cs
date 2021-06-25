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
                    if (m_TreeView.SelectedToolbar == 0) Asset.m_Items.RemoveAt(idx);
                    else if (m_TreeView.SelectedToolbar == 1) Asset.m_ItemTypes.RemoveAt(idx);
                    else if (m_TreeView.SelectedToolbar == 2) Asset.m_ItemEffectTypes.RemoveAt(idx);

                    List<object> tempList = new List<object>();
                    tempList.AddRange(Asset.m_Items);
                    tempList.AddRange(Asset.m_ItemTypes);
                    tempList.AddRange(Asset.m_ItemEffectTypes);
                    return tempList;
                })
                .MakeToolbar("Items", "Types", "EffectTypes");

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

            //EditorUtils.StringHeader("Items", 15, true);
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
                Target.m_Name = EditorGUILayout.TextField("Name: ", Target.m_Name);
                EditorGUILayout.TextField("Guid: ", Target.m_Guid);
                
                using (new EditorGUILayout.VerticalScope("Box"))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("ItemTypes");
                        if (GUILayout.Button("+", GUILayout.Width(20)))
                        {
                            var temp = Target.m_ItemTypes.ToList();
                            temp.Add("");
                            Target.m_ItemTypes = temp.ToArray();
                        }
                    }
                    
                    EditorGUI.indentLevel += 1;
                    for (int i = 0; i < Target.m_ItemTypes.Length; i++)
                    {
                        using(new EditorGUILayout.HorizontalScope())
                        {
                            int tSelected = EditorGUILayout.Popup(GetSelectedItemType(Target.m_ItemTypes[i]), m_ItemTypes);
                            Target.m_ItemTypes[i] = tSelected == 0 ? "" : ItemDataList.Instance.m_ItemTypes[tSelected - 1].m_Guid;

                            if (GUILayout.Button("-", GUILayout.Width(20)))
                            {
                                var temp = Target.m_ItemTypes.ToList();
                                temp.RemoveAt(i);
                                Target.m_ItemTypes = temp.ToArray();
                                i--;
                            }
                        }
                    }
                    EditorGUI.indentLevel -= 1;
                }

                using (new EditorGUILayout.VerticalScope("Box"))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("ItemEffects");
                        if (GUILayout.Button("+", GUILayout.Width(20)))
                        {
                            var temp = Target.m_ItemEffectTypes.ToList();
                            temp.Add("");
                            Target.m_ItemEffectTypes = temp.ToArray();
                        }
                    }
                    
                    EditorGUI.indentLevel += 1;
                    for (int i = 0; i < Target.m_ItemEffectTypes.Length; i++)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            int teSelected = EditorGUILayout.Popup(GetSelectedItemEffectType(Target.m_ItemEffectTypes[i]), m_ItemEffectTypes);
                            Target.m_ItemEffectTypes[i] = teSelected == 0 ? "" : ItemDataList.Instance.m_ItemEffectTypes[teSelected - 1].m_Guid;

                            if (GUILayout.Button("-", GUILayout.Width(20)))
                            {
                                var temp = Target.m_ItemEffectTypes.ToList();
                                temp.RemoveAt(i);
                                Target.m_ItemEffectTypes = temp.ToArray();
                                i--;
                            }
                        }
                    }
                    EditorGUI.indentLevel -= 1;
                }

                DrawValueContainer(Target.m_Values);

                //using (new EditorGUILayout.VerticalScope("Box"))
                //{
                //    using (new EditorGUILayout.HorizontalScope())
                //    {
                //        EditorGUILayout.LabelField("Values");
                //        if (GUILayout.Button("+", GUILayout.Width(20)))
                //        {
                //            GenericMenu typeMenu = new GenericMenu();
                //            typeMenu.AddItem(new GUIContent("Int"), false, () =>
                //            {
                //                Target.m_Values.Add<int>("New Int Value", 0);
                //            });
                //            typeMenu.AddItem(new GUIContent("Double"), false, () =>
                //            {
                //                Target.m_Values.Add<double>("New Double Value", 0);
                //            });
                //            typeMenu.AddItem(new GUIContent("String"), false, () =>
                //            {
                //                Target.m_Values.Add<string>("New String Value", "");
                //            });
                //            typeMenu.AddItem(new GUIContent("Bool"), false, () =>
                //            {
                //                Target.m_Values.Add<bool>("New Bool Value", false);
                //            });
                //            typeMenu.AddItem(new GUIContent("Delegate"), false, () =>
                //            {
                //                Target.m_Values.Add<Action>("New Delegate Value", () => { });
                //            });
                //            //;
                //            //GUIUtility.GUIToScreenPoint(Event.current.mousePosition)
                //            //GUILayoutUtility.GetRect()
                //            Rect rect = GUILayoutUtility.GetLastRect();
                //            rect.position = Event.current.mousePosition;
                //            //rect.width = 100; rect.height = 400;
                //            typeMenu.DropDown(rect);
                //        }
                //    }

                //    EditorGUI.indentLevel += 1;
                //    if (Target.m_Values == null) Target.m_Values = new ValuePairContainer();
                //    for (int i = 0; i < Target.m_Values?.Count; i++)
                //    {
                //        using (new EditorGUILayout.HorizontalScope())
                //        {
                //            Target.m_Values[i].m_Name = EditorGUILayout.TextField(Target.m_Values[i].m_Name, GUILayout.Width(150));
                //            switch (Target.m_Values[i].GetValueType())
                //            {
                //                case Syadeu.Database.ValueType.Int32:
                //                    int intFal = EditorGUILayout.IntField((int)Target.m_Values[i].GetValue());
                //                    if (!Target.m_Values[i].GetValue().Equals(intFal))
                //                    {
                //                        Target.m_Values.SetValue(Target.m_Values[i].m_Name, intFal);
                //                    }
                //                    break;
                //                case Syadeu.Database.ValueType.Double:
                //                    double doubleVal = EditorGUILayout.DoubleField((double)Target.m_Values[i].GetValue());
                //                    if (!Target.m_Values[i].GetValue().Equals(doubleVal))
                //                    {
                //                        Target.m_Values.SetValue(Target.m_Values[i].m_Name, doubleVal);
                //                    }
                //                    break;
                //                case Syadeu.Database.ValueType.String:
                //                    string stringVal = EditorGUILayout.TextField((string)Target.m_Values[i].GetValue());
                //                    if (!Target.m_Values[i].GetValue().Equals(stringVal))
                //                    {
                //                        Target.m_Values.SetValue(Target.m_Values[i].m_Name, stringVal);
                //                    }
                //                    break;
                //                case Syadeu.Database.ValueType.Boolean:
                //                    bool boolVal = EditorGUILayout.Toggle((bool)Target.m_Values[i].GetValue());
                //                    if (!Target.m_Values[i].GetValue().Equals(boolVal))
                //                    {
                //                        Target.m_Values.SetValue(Target.m_Values[i].m_Name, boolVal);
                //                    }
                //                    break;
                //                case Syadeu.Database.ValueType.Delegate:
                //                    EditorGUI.BeginDisabledGroup(true);
                //                    EditorGUILayout.TextField("Delegate");
                //                    EditorGUI.EndDisabledGroup();
                //                    break;
                //                default:
                //                    break;
                //            }

                //            if (GUILayout.Button("-", GUILayout.Width(20)))
                //            {
                //                Target.m_Values.RemoveAt(i);
                //                i--;
                //                continue;
                //            }
                //        }
                //    }
                //    EditorGUI.indentLevel -= 1;
                //}
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
                DrawValueContainer(Target.m_Values);
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
                DrawValueContainer(Target.m_Values);
            }
        }

        private static void DrawValueContainer(ValuePairContainer container)
        {
            using (new EditorGUILayout.VerticalScope("Box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Values");
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
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        container[i].m_Name = EditorGUILayout.TextField(container[i].m_Name, GUILayout.Width(150));
                        switch (container[i].GetValueType())
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
    }
}
