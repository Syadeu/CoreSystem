﻿using Syadeu;
using Syadeu.Mono;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System;
using SyadeuEditor.Presentation;

using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine.AddressableAssets;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using SyadeuEditor.Utilities;
using Syadeu.Collections.Editor;

namespace SyadeuEditor
{
    //[CustomEditor(typeof(PrefabList))]
    [Obsolete("", true)]
    public class PrefabListEditor : InspectorEditor<PrefabList>
    {
        private static AddressableAssetSettings DefaultSettings => AddressableAssetSettingsDefaultObject.GetSettings(true);
        private static AddressableAssetGroup m_DefaultGroup;
        public static AddressableAssetGroup DefaultGroup
        {
            get
            {
                if (m_DefaultGroup == null)
                {
                    var tempList = DefaultSettings.groups.Where((other) => other.Name.Equals("PrefabList"));
                    if (!tempList.Any())
                    {
                        m_DefaultGroup = DefaultSettings.CreateGroup("PrefabList", false, false, true, null);
                        m_DefaultGroup.AddSchema(new ContentUpdateGroupSchema()
                        {
                            StaticContent = true
                        });
                        m_DefaultGroup.AddSchema(new BundledAssetGroupSchema());
                        EditorUtility.SetDirty(m_DefaultGroup);
                        EditorUtility.SetDirty(DefaultSettings);
                    }
                    else m_DefaultGroup = tempList.First();
                }
                return m_DefaultGroup;
            }
        }
        public static AddressableAssetGroup[] PrefabListGroups
        {
            get
            {
                var tempList = DefaultSettings.groups.Where((other) =>
                {
                    if (!other.Equals(DefaultGroup) && other.HasSchema<PrefabListGroupSchema>()) return true;
                    return false;
                });
                if (!tempList.Any()) return Array.Empty<AddressableAssetGroup>();

                return tempList.ToArray();
            }
        }

        private bool m_Lock = true;

        protected override void OnInspectorGUIContents()
        {
            //EditorUtilities.StringHeader("Prefab List");
            CoreGUI.SectorLine();

            if (GUILayout.Button("Build"))
            {
                AddressableAssetSettings.BuildPlayerContent();
            }
            if (GUILayout.Button("Rebase"))
            {
                //var objSetField = GetField("m_ObjectSettings");
                //List<PrefabList.ObjectSetting> origin = (List<PrefabList.ObjectSetting>)objSetField.GetValue(Asset);

                //Rebase(origin);
                Rebase();

                //EditorUtility.SetDirty(target);
                Repaint();
            }

            m_Lock = EditorGUILayout.Toggle("Lock", m_Lock);

            using (new EditorGUI.DisabledGroupScope(m_Lock))
            {
                base.OnInspectorGUI();
            }
        }

        public static void Rebase()
        {
            List<PrefabList.ObjectSetting> list = PrefabList.Instance.ObjectSettings;

            Queue<int> invalidIndices = new Queue<int>();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].GetEditorAsset() == null)
                {
                    CoreSystem.Logger.Log(LogChannel.Editor,
                        $"PrefabList found an invalid asset at {i}:{list[i].m_Name}");

                    //list[i].m_Name = $"!!INVALID!! {list[i].m_Name}";
                    invalidIndices.Enqueue(i);
                    continue;
                }

                if (list[i].GetEditorAsset() is GameObject gameobj &&
                    gameobj.GetComponent<RectTransform>() != null)
                {
                    list[i].m_IsWorldUI = true;
                }
            }

            bool changed = UpdateGroup(DefaultGroup, invalidIndices);
            AddressableAssetGroup[] groups = PrefabListGroups;
            for (int i = 0; i < groups.Length; i++)
            {
                changed |= UpdateGroup(groups[i], invalidIndices);
            }

            EditorUtility.SetDirty(PrefabList.Instance);
            //if (changed)
            //{
            //    AddressableAssetSettings.BuildPlayerContent();
            //}

            static bool UpdateGroup(AddressableAssetGroup group, Queue<int> invalidIndices)
            {
                bool changed = false;

                List<PrefabList.ObjectSetting> list = PrefabList.Instance.ObjectSettings;
                foreach (AddressableAssetEntry item in group.entries)
                {
                    string name = item.address.Split('/').Last().Split('.').First();
                    if (list.Where(other => other.m_Name.Equals(name) && other.m_RefPrefab != null && other.m_RefPrefab.editorAsset != null).Any())
                    {
                        continue;
                    }

                    AssetReference refObj = new AssetReference(item.guid);
                    if (invalidIndices.Count > 0)
                    {
                        int targetIdx = invalidIndices.Dequeue();
                        string previousName = list[targetIdx].m_Name;

                        bool isWorldUI = false;
                        if (item.TargetAsset is GameObject gameobj &&
                            gameobj.GetComponent<RectTransform>() != null)
                        {
                            isWorldUI = true;
                        }

                        list[targetIdx] = new PrefabList.ObjectSetting(name, refObj, isWorldUI);

                        CoreSystem.Logger.Log(LogChannel.Editor,
                            $"PrefabList index at {targetIdx}:{previousName} was invalid " +
                            $"but replaced to newly added prefab({name})");

                        changed = true;
                    }
                    else
                    {
                        list.Add(new PrefabList.ObjectSetting(name, refObj, false));
                        changed = true;
                    }
                }
                return changed;
            }
        }
    }
}
