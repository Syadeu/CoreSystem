using Syadeu.Database;
using Syadeu.Database.CreatureData;
using Syadeu.Database.CreatureData.Attributes;
using Syadeu.Internal;
using Syadeu.Presentation;
using SyadeuEditor.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor
{
    [CustomEditor(typeof(EntityDataList))]
    public sealed class EntityDataListEditor : EditorEntity<EntityDataList>
    {
        private VerticalTreeView treeView;

        private static string[] m_AttributeNames;

        private void OnEnable()
        {
            Asset.m_Entites = null;
            Asset.m_Attributes = null;
            EditorUtility.SetDirty(target);

            LuaEditor.Reload();
            Asset.LoadData();

            if (Asset.m_Attributes == null) m_AttributeNames = Array.Empty<string>();
            else
            {
                var temp = Asset.m_Attributes.Select((other) => other.Name).ToList();
                temp.Insert(0, "None");
                m_AttributeNames = temp.ToArray();
            }

            List<object> tempList = new List<object>();
            if (Asset.m_Entites != null) tempList.AddRange(Asset.m_Entites);
            if (Asset.m_Attributes != null) tempList.AddRange(Asset.m_Attributes);

            treeView = new VerticalTreeView(Asset, serializedObject);
            treeView.OnDirty += RefreshTreeView;
            treeView
                .SetupElements(tempList, (other) =>
                {
                    if (other is EntityBase entity)
                    {
                        return new TreeEntityElement(treeView, entity);
                    }
                    else if (other is AttributeBase attribute)
                    {
                        return new TreeAttributeElement(treeView, attribute);
                    }
                    else throw new Exception();
                })
                .MakeAddButton(() =>
                {
                    if (treeView.SelectedToolbar == 0)
                    {
                        Type[] types = TypeHelper.GetTypes((other) => !other.IsAbstract && TypeHelper.TypeOf<EntityBase>.Type.IsAssignableFrom(other));

                        Rect rect = GUILayoutUtility.GetLastRect();
                        rect.position = Event.current.mousePosition;

                        PopupWindow.Show(rect, SelectorPopup<Type, Type>.GetWindow(types, (t) =>
                        {
                            if (Asset.m_Entites == null) Asset.m_Entites = new List<EntityBase>();
                            Asset.m_Entites.Add((EntityBase)Activator.CreateInstance(t));
                            RefreshTreeView();
                        },
                        (t) => t, (t) => t.Name));
                    }
                    else if (treeView.SelectedToolbar == 1)
                    {
                        Type[] types = TypeHelper.GetTypes((other) => !other.IsAbstract && TypeHelper.TypeOf<AttributeBase>.Type.IsAssignableFrom(other));

                        Rect rect = GUILayoutUtility.GetLastRect();
                        rect.position = Event.current.mousePosition;

                        PopupWindow.Show(rect, SelectorPopup<Type, Type>.GetWindow(types, (t) =>
                        {
                            if (Asset.m_Attributes == null) Asset.m_Attributes = new List<AttributeBase>();
                            Asset.m_Attributes.Add((AttributeBase)Activator.CreateInstance(t));
                            RefreshTreeView();
                        },
                        (t) => t, (t) => t.Name));
                    }

                    List<object> tempList = new List<object>();
                    if (Asset.m_Entites != null) tempList.AddRange(Asset.m_Entites);
                    if (Asset.m_Attributes != null) tempList.AddRange(Asset.m_Attributes);
                    return tempList;
                })
                .MakeRemoveButton((idx) =>
                {
                    if (treeView.SelectedToolbar == 0) Asset.m_Entites.Remove((EntityBase)treeView.Data[idx]);
                    else if (treeView.SelectedToolbar == 1)
                    {
                        Asset.m_Attributes.Remove((AttributeBase)treeView.Data[idx]);
                    }

                    List<object> tempList = new List<object>();
                    if (Asset.m_Entites != null) tempList.AddRange(Asset.m_Entites);
                    if (Asset.m_Attributes != null) tempList.AddRange(Asset.m_Attributes);
                    return tempList;
                })
                .MakeToolbar("Entities", "Attributes");
        }
        private void RefreshTreeView()
        {
            var temp = Asset.m_Attributes.Select((other) => other.Name).ToList();
            temp.Insert(0, "None");
            m_AttributeNames = temp.ToArray();

            List<object> tempList = new List<object>();
            if (Asset.m_Entites != null) tempList.AddRange(Asset.m_Entites);
            if (Asset.m_Attributes != null) tempList.AddRange(Asset.m_Attributes);
            treeView.Refresh(tempList);
        }
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Clear"))
            {
                Asset.m_Entites?.Clear();
                Asset.m_Attributes?.Clear();
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

                RefreshTreeView();
            }
            EditorGUILayout.EndHorizontal();
            EditorUtils.SectorLine();
            EditorGUILayout.Space();

            if (treeView == null) OnEnable();
            treeView.OnGUI();

            EditorGUILayout.Space();
            base.OnInspectorGUI();
        }

        private class TreeEntityElement : VerticalTreeElement<EntityBase>
        {
            public override string Name => Target.Name;
            public override bool HideElementInTree
                => Tree.SelectedToolbar != 0 || base.HideElementInTree;

            ReflectionHelperEditor.Drawer m_Drawer;

            public TreeEntityElement(VerticalTreeView treeView, EntityBase entity) : base(treeView, entity)
            {
                m_Drawer = ReflectionHelperEditor.GetDrawer(entity, new string[]
                    {
                        "Name", "Hash", "PrefabIdx", "Attributes"
                    });
            }
            public override void OnGUI()
            {
                EditorUtils.StringRich(Target.GetType().Name, 15);

                Target.Name = EditorGUILayout.TextField("Name: ", Target.Name);
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("Hash: ", Target.Hash.ToString());
                EditorGUI.EndDisabledGroup();
                ReflectionHelperEditor.DrawPrefabReference("Prefab: ", (idx) => Target.PrefabIdx = idx, Target.PrefabIdx);
                //Target.PrefabIdx = PrefabListEditor.DrawPrefabSelector(Target.PrefabIdx);

                EditorGUILayout.BeginVertical(EditorUtils.Box);
                {
                    if (Target.Attributes == null) Target.Attributes = new List<Hash>();
                    EditorGUILayout.BeginHorizontal();
                    EditorUtils.StringRich("Attributes", 15);
                    if (GUILayout.Button("+", GUILayout.Width(20)))
                    {
                        Target.Attributes.Add(Hash.Empty);
                        return;
                    }
                    if (Target.Attributes.Count > 0 && GUILayout.Button("-", GUILayout.Width(20)))
                    {
                        Target.Attributes.RemoveAt(Target.Attributes.Count - 1);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUI.indentLevel += 1;
                    for (int i = 0; i < Target.Attributes.Count; i++)
                    {
                        EditorGUILayout.BeginHorizontal();

                        int idx = i;
                        EditorGUI.BeginChangeCheck();
                        idx = EditorGUILayout.DelayedIntField(idx, GUILayout.Width(80));
                        if (EditorGUI.EndChangeCheck())
                        {
                            if (idx >= Target.Attributes.Count) idx = Target.Attributes.Count - 1;

                            Hash cache = Target.Attributes[i];
                            Target.Attributes.RemoveAt(i);
                            Target.Attributes.Insert(idx, cache);

                            //EditorGUILayout.EndHorizontal();
                            //break;
                        }

                        ReflectionHelperEditor.DrawAttributeSelector((attHash) => Target.Attributes[i] = attHash, Target.Attributes[i]);

                        if (GUILayout.Button("-", GUILayout.Width(20)))
                        {
                            Target.Attributes.RemoveAt(i);
                            i--;
                        }

                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUI.indentLevel -= 1;
                }
                EditorGUILayout.EndVertical();

                EditorUtils.Line();

                m_Drawer.OnGUI(false);
                //Target.m_HP = EditorGUILayout.FloatField("HP", Target.m_HP);
                //Target.m_Values.DrawValueContainer("Values");
            }

            private int GetSelectedAttributeIdx(Hash attHash)
            {
                if (attHash.Equals(Hash.Empty)) return 0;

                for (int i = 0; i < EntityDataList.Instance.m_Attributes.Count; i++)
                {
                    if (EntityDataList.Instance.m_Attributes[i].Hash.Equals(attHash)) return i + 1;
                }
                return 0;
            }
        }
        private class TreeAttributeElement : VerticalTreeElement<AttributeBase>
        {
            public override string Name
            {
                get
                {
                    m_Name = $"{Target.GetType().Name}: {Target.Name}";
                    return m_Name;
                }
            }
            public override bool HideElementInTree
                => Tree.SelectedToolbar != 1 || base.HideElementInTree;

            ReflectionHelperEditor.Drawer m_Drawer;

            public TreeAttributeElement(VerticalTreeView treeView, AttributeBase att) : base(treeView, att)
            {
                m_Drawer = ReflectionHelperEditor.GetDrawer(att);

            }
            public override void OnGUI()
            {
                m_Drawer.OnGUI();
            }
        }
    }
}
