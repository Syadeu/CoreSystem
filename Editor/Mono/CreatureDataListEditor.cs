using Syadeu.Database;
using Syadeu.Database.CreatureData;
using Syadeu.Internal;
using SyadeuEditor.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor
{
    [CustomEditor(typeof(CreatureDataList))]
    public sealed class CreatureDataListEditor : EditorEntity<CreatureDataList>
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
                    if (other is Creature creature)
                    {
                        return new TreeCreatureElement(treeView, creature);
                    }
                    else if (other is ICreatureAttribute attribute)
                    {
                        return new TreeCreatureAttributeElement(treeView, attribute);
                    }
                    else throw new Exception();
                })
                .MakeAddButton(() =>
                {
                    if (treeView.SelectedToolbar == 0) Asset.m_Entites.Add(new Creature());
                    else if (treeView.SelectedToolbar == 1)
                    {
                        GenericMenu menu = new GenericMenu();
                        Type[] types = TypeHelper.GetTypes((other) => other.GetInterface("ICreatureAttribute") != null);
                        for (int i = 0; i < types.Length; i++)
                        {
                            Type target = types[i];
                            menu.AddItem(new GUIContent(target.Name), false, () =>
                            {
                                if (Asset.m_Attributes == null) Asset.m_Attributes = new List<ICreatureAttribute>();

                                Asset.m_Attributes.Add((ICreatureAttribute)Activator.CreateInstance(target));
                                RefreshTreeView();
                            });
                        }
                        
                        Rect rect = GUILayoutUtility.GetLastRect();
                        rect.position = Event.current.mousePosition;
                        menu.DropDown(rect);
                    }

                    List<object> tempList = new List<object>();
                    if (Asset.m_Entites != null) tempList.AddRange(Asset.m_Entites);
                    if (Asset.m_Attributes != null) tempList.AddRange(Asset.m_Attributes);
                    return tempList;
                })
                .MakeRemoveButton((idx) =>
                {
                    if (treeView.SelectedToolbar == 0) Asset.m_Entites.Remove((Creature)treeView.Data[idx]);
                    else if (treeView.SelectedToolbar == 1)
                    {
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
            EditorUtils.StringHeader("Creature Data");
            EditorUtils.SectorLine();

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
            }
            EditorGUILayout.EndHorizontal();
            EditorUtils.SectorLine();
            EditorGUILayout.Space();

            if (treeView == null) OnEnable();
            treeView.OnGUI();

            EditorGUILayout.Space();
            base.OnInspectorGUI();
        }

        private class TreeCreatureElement : VerticalTreeElement<Creature>
        {
            public override string Name => Target.m_Name;
            public override bool HideElementInTree
                => Tree.SelectedToolbar != 0 || base.HideElementInTree;

            public TreeCreatureElement(VerticalTreeView treeView, Creature creature) : base(treeView, creature) { }
            public override void OnGUI()
            {
                Target.m_Name = EditorGUILayout.TextField("Name: ", Target.m_Name);
                EditorGUILayout.TextField("Hash: ", Target.m_Hash.ToString());
                Target.m_PrefabIdx = PrefabListEditor.DrawPrefabSelector(Target.m_PrefabIdx);

                EditorGUILayout.BeginVertical(EditorUtils.Box);
                {
                    if (Target.m_Attributes == null) Target.m_Attributes = new List<Hash>();
                    EditorGUILayout.BeginHorizontal();
                    EditorUtils.StringRich("Attributes", 15);
                    if (GUILayout.Button("+", GUILayout.Width(20)))
                    {
                        Target.m_Attributes.Add(Hash.Empty);
                        return;
                    }
                    if (Target.m_Attributes.Count > 0 && GUILayout.Button("-", GUILayout.Width(20)))
                    {
                        Target.m_Attributes.RemoveAt(Target.m_Attributes.Count - 1);
                    }
                    EditorGUILayout.EndHorizontal();
                    for (int i = 0; i < Target.m_Attributes.Count; i++)
                    {
                        int idx = EditorGUILayout.Popup(GetSelectedAttributeIdx(Target.m_Attributes[i]), m_AttributeNames);
                        if (idx == 0)
                        {
                            Target.m_Attributes[i] = Hash.Empty;
                        }
                        else Target.m_Attributes[i] = CreatureDataList.Instance.m_Attributes[idx - 1].Hash;
                    }
                }
                EditorGUILayout.EndVertical();

                Target.m_Values.DrawValueContainer("Values");

                Target.m_OnSpawn.DrawGUI("OnSpawn");
            }

            private int GetSelectedAttributeIdx(Hash attHash)
            {
                if (attHash.Equals(Hash.Empty)) return 0;

                for (int i = 0; i < CreatureDataList.Instance.m_Attributes.Count; i++)
                {
                    if (CreatureDataList.Instance.m_Attributes[i].Hash.Equals(attHash)) return i + 1;
                }
                return 0;
            }
        }
        private class TreeCreatureAttributeElement : VerticalTreeElement<ICreatureAttribute>
        {
            public override string Name => $"{Target.GetType().Name}: {Target.Name}";
            public override bool HideElementInTree
                => Tree.SelectedToolbar != 1 || base.HideElementInTree;

            MemberInfo[] m_Members;
            
            public TreeCreatureAttributeElement(VerticalTreeView treeView, ICreatureAttribute att) : base(treeView, att)
            {
                m_Members = ReflectionHelper.GetSerializeMemberInfos(att.GetType());

            }
            public override void OnGUI()
            {
                EditorUtils.StringRich(Target.GetType().Name, 15, true);

                for (int i = 0; i < m_Members.Length; i++)
                {
                    //string name = ReflectionHelper.SerializeMemberInfoName(m_Members[i]);
                    //EditorGUILayout.LabelField($"{name}: {m_Members[i].MemberType}");
                    ReflectionHelperEditor.DrawMember(Target, m_Members[i]);
                }
            }
        }
    }
}
