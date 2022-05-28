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

using Syadeu.Collections.Editor;
using Syadeu.Mono;
using System;
using UnityEditor;
using UnityEngine;

namespace Syadeu.Collections.ResourceControl.Editor
{
    internal sealed class AssetMenu : SetupWizardMenuItem
    {
        public override string Name => "Asset";
        public override int Order => -9997;

        //SerializedObject serializedObject;
        //SerializedProperty
        //    m_ObjectSettings;

        //FieldInfo objectSettingsFieldInfo;
        //List<PrefabList.ObjectSetting> objectSettings;

        //int m_AddressableCount = 0;
        //readonly List<int> m_InvalidIndices = new List<int>();

        //private GameObject[] m_BrokenPrefabs = Array.Empty<GameObject>();

        Vector2
            m_Scroll = Vector2.zero;

        public AssetMenu()
        {
            //serializedObject = new SerializedObject(PrefabList.Instance);
            //m_ObjectSettings = serializedObject.FindProperty("m_ObjectSettings");

            //objectSettingsFieldInfo = TypeHelper.TypeOf<PrefabList>.Type.GetField("m_ObjectSettings",
            //    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            //var temp = objectSettingsFieldInfo.GetValue(PrefabList.Instance);

            //if (temp == null)
            //{
            //    objectSettings = new List<PrefabList.ObjectSetting>();
            //    objectSettingsFieldInfo.SetValue(PrefabList.Instance, objectSettings);

            //    serializedObject.Update();
            //}
            //else objectSettings = (List<PrefabList.ObjectSetting>)temp;

            //HashSet<UnityEngine.Object> tempSet = new HashSet<UnityEngine.Object>();
            //for (int i = 0; i < objectSettings.Count; i++)
            //{
            //    UnityEngine.Object obj = objectSettings[i].GetEditorAsset();
            //    if (obj == null)
            //    {
            //        m_InvalidIndices.Add(i);
            //    }
            //    if (tempSet.Contains(obj))
            //    {
            //        objectSettings[i] = new PrefabList.ObjectSetting(objectSettings[i].m_Name, null, objectSettings[i].m_IsWorldUI);
            //        m_InvalidIndices.Add(i);
            //    }

            //    tempSet.Add(obj);
            //}

            //m_AddressableCount = PrefabListEditor.DefaultGroup.entries.Count;
            //var groups = PrefabListEditor.PrefabListGroups;
            //for (int i = 0; i < groups.Length; i++)
            //{
            //    m_AddressableCount += groups[i].entries.Count;
            //}

            //m_BrokenPrefabs = objectSettings.Where(t => PrefabUtility.IsPartOfAnyPrefab(t.GetEditorAsset())).Select(t => (GameObject)t.GetEditorAsset()).Where(t => t.GetComponentsInChildren<Component>(true).Any(t => t == null)).ToArray();
        }

        public override bool Predicate()
        {
            //if (objectSettings.Count - m_InvalidIndices.Count != m_AddressableCount) return false;

            //else if (m_BrokenPrefabs.Length > 0) return false;

            return true;
        }
        [Obsolete()]
        private void Migration()
        {
            var objectSettings = PrefabList.Instance.ObjectSettings;
            if (objectSettings == null || objectSettings.Count == 0)
            {
                return;
            }

            if (GUILayout.Button("Migration"))
            {
                foreach (var item in objectSettings)
                {
                    if (item.m_RefPrefab.editorAsset == null) continue;

                    var entry = item.m_RefPrefab.GetAssetEntry();
                    var list = ResourceHashMapEditor.Find(t => t.Group.Equals(entry.parentGroup.Name));
                    if (list == null)
                    {
                        list = ResourceHashMapEditor.Add(entry.parentGroup.Name);
                        list.Group = entry.parentGroup.Name;
                    }

                    list.AddAsset(String.Empty, item.m_RefPrefab);
                }

                ResourceHashMapEditor.SetDirty();
            }
        }
        public override void OnGUI()
        {
            Migration();

            if (CoreGUI.BoxButton("Locate HashMap", Color.gray))
            {
                EditorGUIUtility.PingObject(ResourceHashMap.Instance);
                Selection.activeObject = ResourceHashMap.Instance;

                //PrefabListEditor.Rebase();
                //serializedObject.Update();

                //m_InvalidIndices.Clear();
                //for (int i = 0; i < objectSettings.Count; i++)
                //{
                //    if (objectSettings[i].GetEditorAsset() == null)
                //    {
                //        m_InvalidIndices.Add(i);
                //    }
                //}
            }

            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);


            //
            EditorGUILayout.EndScrollView();
        }
        //
    }
}

#endif