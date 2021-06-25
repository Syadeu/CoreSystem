using System;
using System.IO;
using System.Linq;
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

        private string[] m_CreatableTypes = new string[] { "Int", "Float", "String", "Bool" };

        private bool m_ShowOriginalContents = false;

        private void OnEnable()
        {
            m_TreeView = new VerticalTreeView(Asset, serializedObject);
            OnValidate();
        }
        private void OnValidate()
        {
            if (m_TreeView == null) m_TreeView = new VerticalTreeView(Asset, serializedObject);
            m_TreeView
                .SetupElements(Asset.m_Items, (other) =>
                {
                    Item item = (Item)other;

                    return new TreeItemElement(m_TreeView, item);
                })
                ;

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
            if (GUILayout.Button("+"))
            {
                
            }
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
                                Target.m_Values.Add<int>("New Int Value", 0);
                            });
                            typeMenu.AddItem(new GUIContent("Double"), false, () =>
                            {
                                Target.m_Values.Add<double>("New Double Value", 0);
                            });
                            typeMenu.AddItem(new GUIContent("String"), false, () =>
                            {
                                Target.m_Values.Add<string>("New String Value", "");
                            });
                            typeMenu.AddItem(new GUIContent("Bool"), false, () =>
                            {
                                Target.m_Values.Add<bool>("New Bool Value", false);
                            });
                            typeMenu.AddItem(new GUIContent("Delegate"), false, () =>
                            {
                                Target.m_Values.Add<Action>("New Delegate Value", () => { });
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
                    if (Target.m_Values == null) Target.m_Values = new ValuePairContainer();
                    for (int i = 0; i < Target.m_Values?.Count; i++)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            Target.m_Values[i].m_Name = EditorGUILayout.TextField(Target.m_Values[i].m_Name, GUILayout.Width(150));
                            switch (Target.m_Values[i].GetValueType())
                            {
                                case Syadeu.Database.ValueType.Int32:
                                    int intFal = EditorGUILayout.IntField((int)Target.m_Values[i].GetValue());
                                    if (!Target.m_Values[i].GetValue().Equals(intFal))
                                    {
                                        Target.m_Values.SetValue(Target.m_Values[i].m_Name, intFal);
                                    }
                                    break;
                                case Syadeu.Database.ValueType.Double:
                                    double doubleVal = EditorGUILayout.DoubleField((double)Target.m_Values[i].GetValue());
                                    if (!Target.m_Values[i].GetValue().Equals(doubleVal))
                                    {
                                        Target.m_Values.SetValue(Target.m_Values[i].m_Name, doubleVal);
                                    }
                                    break;
                                case Syadeu.Database.ValueType.String:
                                    string stringVal = EditorGUILayout.TextField((string)Target.m_Values[i].GetValue());
                                    if (!Target.m_Values[i].GetValue().Equals(stringVal))
                                    {
                                        Target.m_Values.SetValue(Target.m_Values[i].m_Name, stringVal);
                                    }
                                    break;
                                case Syadeu.Database.ValueType.Boolean:
                                    bool boolVal = EditorGUILayout.Toggle((bool)Target.m_Values[i].GetValue());
                                    if (!Target.m_Values[i].GetValue().Equals(boolVal))
                                    {
                                        Target.m_Values.SetValue(Target.m_Values[i].m_Name, boolVal);
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
                                Target.m_Values.RemoveAt(i);
                                i--;
                                continue;
                            }
                        }
                    }
                    EditorGUI.indentLevel -= 1;
                }
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
    }
}
