// Copyright 2022 Ikina Games
// Author : Seung Ha Kim (Syadeu)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#if UNITY_2019_1_OR_NEWER && UNITY_ADDRESSABLES
#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !POINT_DISABLE_CHECKS
#define DEBUG_MODE
#endif
#define UNITYENGINE

using SyadeuEditor;
using SyadeuEditor.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.UIElements;

namespace Syadeu.Collections.ResourceControl.Editor
{
    [CustomEditor(typeof(ResourceHashMap))]
    internal sealed class ResourceHashMapEditor : InspectorEditor<ResourceHashMap>
    {
        private static SerializedObject s_SerializedObject;
        private static SerializedObject SerializedObject
        {
            get
            {
                if (s_SerializedObject == null)
                {
                    s_SerializedObject = new SerializedObject(ResourceHashMap.Instance);
                }
                s_SerializedObject.UpdateIfRequiredOrScript();
                return s_SerializedObject;
            }
        }
        private static string AssetPath => AssetDatabase.GetAssetPath(ResourceHashMap.Instance);

        private SerializedProperty m_SceneBindedLabelsProperty, m_ResourceListsProperty;

        private void OnEnable()
        {
            m_SceneBindedLabelsProperty = serializedObject.FindProperty("m_SceneBindedLabels");
            m_ResourceListsProperty = serializedObject.FindProperty("m_ResourceLists");
        }
        protected override void OnInspectorGUIContents()
        {
            EditorGUILayout.PropertyField(m_SceneBindedLabelsProperty);

            EditorGUILayout.Space();
            CoreGUI.Line();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (CoreGUI.BoxButton("+", Color.gray))
                {
                    int index = m_ResourceListsProperty.arraySize;
                    
                    ResourceList list = CreateInstance<ResourceList>();
                    list.name = "ResourceList " + index;
                    AssetDatabase.AddObjectToAsset(list, assetPath);
                    
                    m_ResourceListsProperty.InsertArrayElementAtIndex(index);
                    m_ResourceListsProperty.GetArrayElementAtIndex(index).objectReferenceValue = list;
                }
                if (CoreGUI.BoxButton("-", Color.gray))
                {
                    int index = m_ResourceListsProperty.arraySize - 1;
                    ResourceList list = m_ResourceListsProperty.GetArrayElementAtIndex(index).objectReferenceValue as ResourceList;
                    m_ResourceListsProperty.DeleteArrayElementAtIndex(index);

                    AssetDatabase.RemoveObjectFromAsset(list);
                }
            }
            using (new EditorGUI.DisabledGroupScope(true))
            {
                EditorGUILayout.PropertyField(m_ResourceListsProperty);
            }

            serializedObject.ApplyModifiedProperties();
            //base.OnInspectorGUIContents();
        }

        public static ResourceList Find(Func<ResourceList, bool> predicate)
        {
            if (predicate == null) return null;

            foreach (var item in ResourceHashMap.Instance.ResourceLists)
            {
                if (predicate.Invoke(item)) return item;
            }
            return null;
        }
        public static ResourceList Add(string name)
        {
            SerializedProperty
                m_ResourceListsProperty = SerializedObject.FindProperty("m_SceneBindedLabels");

            int index = m_ResourceListsProperty.arraySize;

            ResourceList list = CreateInstance<ResourceList>();
            if (name.IsNullOrEmpty())
            {
                list.name = "ResourceList " + index;
            }
            else list.name = name;

            AssetDatabase.AddObjectToAsset(list, AssetPath);
            EditorUtility.SetDirty(ResourceHashMap.Instance);

            m_ResourceListsProperty.InsertArrayElementAtIndex(index);
            m_ResourceListsProperty.GetArrayElementAtIndex(index).objectReferenceValue = list;

            SerializedObject.ApplyModifiedProperties();

            return ResourceHashMap.Instance.ResourceLists[index];
        }
        public static new void SetDirty()
        {
            EditorUtility.SetDirty(ResourceHashMap.Instance);
        }
    }

    [CustomEditor(typeof(ResourceList))]
    internal sealed class ResourceListEditor : InspectorEditor<ResourceList>
    {
        private SerializedProperty m_GroupProperty, m_GroupNameProperty;
        private SerializedProperty m_AssetListProperty;

        private bool m_IsBindedToCatalog = false;
        private bool m_RequireRebuild = false;

        private void OnEnable()
        {
            m_GroupProperty = serializedObject.FindProperty("m_Group");
            m_GroupNameProperty = GroupReferencePropertyDrawer.Helper.GetCatalogName(m_GroupProperty);
            m_AssetListProperty = serializedObject.FindProperty("m_AssetList");

            Validate();
        }
        private void Validate()
        {
            AddressableAssetGroup addressableAssetGroup = GetGroup(m_GroupNameProperty);
            if (addressableAssetGroup == null)
            {
                m_IsBindedToCatalog = false;
                m_RequireRebuild = false;

                return;
            }

            m_IsBindedToCatalog = true;
            if (!Validate(target))
            {
                m_RequireRebuild = true;
                return;
            }
            m_RequireRebuild = false;
        }
        public static void Rebuild(ResourceList list)
        {
            AddressableAssetGroup addressableAssetGroup = GetGroup(list.Group);
            List<AddressableAssetEntry> entries = new List<AddressableAssetEntry>();
            addressableAssetGroup.GatherAllAssets(entries, true, true, true);

            list.Clear();
            for (int i = 0; i < entries.Count; i++)
            {
                list.AddAsset(string.Empty, entries[i].TargetAsset);
            }
            EditorUtility.SetDirty(list);
        }
        private void Rebuild()
        {
            AddressableAssetGroup addressableAssetGroup = GetGroup(m_GroupNameProperty);
            List<AddressableAssetEntry> entries = new List<AddressableAssetEntry>();
            addressableAssetGroup.GatherAllAssets(entries, true, true, true);

            m_AssetListProperty.ClearArray();
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();

            for (int i = 0; i < entries.Count; i++)
            {
                target.AddAsset(string.Empty, entries[i].TargetAsset);
            }

            EditorUtility.SetDirty(target);
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }

        protected override void OnInspectorGUIContents()
        {
            bool catalogChanged = false;
            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.PropertyField(m_GroupProperty);
                catalogChanged = changed.changed;

                if (catalogChanged) Validate();
            }

            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                target.name = EditorGUILayout.DelayedTextField("Name", target.name);

                if (changed.changed)
                {
                    EditorUtility.SetDirty(target);
                    EditorUtility.SetDirty(ResourceHashMap.Instance);
                    AssetDatabase.ImportAsset(
                        AssetDatabase.GetAssetPath(ResourceHashMap.Instance), 
                        ImportAssetOptions.ForceUpdate);
                }
            }

            if (m_RequireRebuild)
            {
                EditorGUILayout.Space();
                if (GUILayout.Button("!! Require Rebuild !!"))
                {
                     Rebuild();
                     m_RequireRebuild = false;
                }
            }
            
            EditorGUILayout.Space();
            if (m_IsBindedToCatalog)
            {
                var groupName = SerializedPropertyHelper.ReadFixedString128Bytes(m_GroupNameProperty);
                CoreGUI.Label($"Binded to {groupName}", 15, TextAnchor.MiddleCenter);

                using (new CoreGUI.BoxBlock(Color.gray))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        CoreGUI.Label(m_AssetListProperty.displayName, 13, TextAnchor.MiddleLeft);
                        
                        if (GUILayout.Button("Rebuild", GUILayout.Width(100)))
                        {
                            Rebuild();
                            m_RequireRebuild = false;
                        }
                    }

                    CoreGUI.Line();

                    using (new EditorGUI.IndentLevelScope())
                    {
                        for (int i = 0; i < m_AssetListProperty.arraySize; i++)
                        {
                            var prop = m_AssetListProperty.GetArrayElementAtIndex(i);
                            string displayName;
                            {
                                var refAsset = target.GetAddressableAsset(i);

                                if (refAsset.EditorAsset != null)
                                {
                                    displayName = refAsset.FriendlyName.IsNullOrEmpty() ?
                                        refAsset.EditorAsset.name : refAsset.FriendlyName;

                                    displayName += $" ({AssetDatabase.GetAssetPath(refAsset.EditorAsset)})";
                                }
                                else displayName = prop.displayName;
                            }

                            prop.isExpanded = EditorGUILayout.Foldout(prop.isExpanded, displayName, true);
                            if (!prop.isExpanded) continue;

                            using (new EditorGUI.IndentLevelScope())
                            {
                                prop.Next(true);
                                EditorGUILayout.PropertyField(prop);
                                prop.Next(false);
                                using (new EditorGUI.DisabledGroupScope(true))
                                {
                                    EditorGUILayout.PropertyField(prop);
                                }
                            }
                            //
                        }
                    }
                }
            }
            else
            {
                EditorGUILayout.PropertyField(m_AssetListProperty);
            }

            serializedObject.ApplyModifiedProperties();
            //base.OnInspectorGUIContents();
        }

        #region Utils

        private static AddressableAssetGroup GetGroup(SerializedProperty groupNameProperty)
        {
            var catalogName = SerializedPropertyHelper.ReadFixedString128Bytes(groupNameProperty);
            return GetGroup(catalogName.IsEmpty ? string.Empty : catalogName.ToString());
        }
        private static AddressableAssetGroup GetGroup(string groupName)
        {
            if (groupName.IsNullOrEmpty()) return null;

            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);

            return settings.FindGroup(groupName.ToString());
        }

        public static bool Validate(ResourceList list)
        {
            AddressableAssetGroup addressableAssetGroup = GetGroup(list.Group);
            if (addressableAssetGroup == null)
            {
                return true;
            }

            List<AddressableAssetEntry> entries = new List<AddressableAssetEntry>();
            addressableAssetGroup.GatherAllAssets(entries, true, true, true);

            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (!list.Contains(entries[i].guid))
                {
                    return false;
                }
            }

            return true;
        }

        #endregion
    }
}

#endif