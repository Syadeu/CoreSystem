﻿using Syadeu.Database;
using Syadeu.Database.CreatureData;
using SyadeuEditor.Tree;
using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

namespace SyadeuEditor
{
    [CustomEditor(typeof(CreatureDataList))]
    public sealed class CreatureDataListEditor : EditorEntity<CreatureDataList>
    {
        private VerticalTreeView treeView;

        private void OnEnable()
        {
            Asset.m_Entites = null;
            Asset.m_Attributes = null;
            EditorUtility.SetDirty(target);

            LuaEditor.Reload();
            Asset.LoadData();

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
                    else if (other is CreatureAttributeEntity attribute)
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
                        menu.AddItem(new GUIContent("Test1"), false, () =>
                        {
                            //Asset.m_ItemTypes.Add(new ItemType());
                            RefreshTreeView();
                        });
                        menu.AddItem(new GUIContent("Test2"), false, () =>
                        {
                            //Asset.m_ItemTypes.Add(new ItemType());
                            RefreshTreeView();
                        });
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

            if (treeView == null) return;
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
                Target.m_Values.DrawValueContainer("Values");

                Target.m_OnSpawn.DrawGUI("OnSpawn");
            }
        }
        private class TreeCreatureAttributeElement : VerticalTreeElement<CreatureAttributeEntity>
        {
            public override string Name => Target.m_Name;
            public override bool HideElementInTree
                => Tree.SelectedToolbar != 1 || base.HideElementInTree;

            public TreeCreatureAttributeElement(VerticalTreeView treeView, CreatureAttributeEntity att) : base(treeView, att) { }
            public override void OnGUI()
            {
                //Target.DrawItem();
            }
        }
    }
}
