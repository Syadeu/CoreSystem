#if CORESYSTEM_FMOD
using Syadeu.FMOD;
#elif CORESYSTEM_UNITYAUDIO
#endif

using Syadeu.Collections;
using Syadeu.Collections.Editor;
using Syadeu.Mono;
using SyadeuEditor.Presentation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor
{
    [Obsolete("")]
    internal sealed class PrefabMenu : SetupWizardMenuItem
    {
        public override string Name => "Prefab";
        public override int Order => -9997;

        SerializedObject serializedObject;
        SerializedProperty
            m_ObjectSettings;

        FieldInfo objectSettingsFieldInfo;
        List<PrefabList.ObjectSetting> objectSettings;

        int m_AddressableCount = 0;
        readonly List<int> m_InvalidIndices = new List<int>();

        private GameObject[] m_BrokenPrefabs = Array.Empty<GameObject>();

        Vector2
            m_Scroll = Vector2.zero;

        public PrefabMenu()
        {
            serializedObject = new SerializedObject(PrefabList.Instance);
            m_ObjectSettings = serializedObject.FindProperty("m_ObjectSettings");

            objectSettingsFieldInfo = TypeHelper.TypeOf<PrefabList>.Type.GetField("m_ObjectSettings",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var temp = objectSettingsFieldInfo.GetValue(PrefabList.Instance);

            if (temp == null)
            {
                objectSettings = new List<PrefabList.ObjectSetting>();
                objectSettingsFieldInfo.SetValue(PrefabList.Instance, objectSettings);

                serializedObject.Update();
            }
            else objectSettings = (List<PrefabList.ObjectSetting>)temp;

            HashSet<UnityEngine.Object> tempSet = new HashSet<UnityEngine.Object>();
            for (int i = 0; i < objectSettings.Count; i++)
            {
                UnityEngine.Object obj = objectSettings[i].GetEditorAsset();
                if (obj == null)
                {
                    m_InvalidIndices.Add(i);
                }
                if (tempSet.Contains(obj))
                {
                    objectSettings[i] = new PrefabList.ObjectSetting(objectSettings[i].m_Name, null, objectSettings[i].m_IsWorldUI);
                    m_InvalidIndices.Add(i);
                }

                tempSet.Add(obj);
            }

            m_AddressableCount = PrefabListEditor.DefaultGroup.entries.Count;
            var groups = PrefabListEditor.PrefabListGroups;
            for (int i = 0; i < groups.Length; i++)
            {
                m_AddressableCount += groups[i].entries.Count;
            }

            m_BrokenPrefabs = objectSettings.Where(t => PrefabUtility.IsPartOfAnyPrefab(t.GetEditorAsset())).Select(t => (GameObject)t.GetEditorAsset()).Where(t => t.GetComponentsInChildren<Component>(true).Any(t => t == null)).ToArray();
        }

        public override bool Predicate()
        {
            if (objectSettings.Count - m_InvalidIndices.Count != m_AddressableCount) return false;

            else if (m_BrokenPrefabs.Length > 0) return false;

            return true;
        }
        public override void OnGUI()
        {
            if (GUILayout.Button("Rebase"))
            {
                PrefabListEditor.Rebase();
                serializedObject.Update();

                m_InvalidIndices.Clear();
                for (int i = 0; i < objectSettings.Count; i++)
                {
                    if (objectSettings[i].GetEditorAsset() == null)
                    {
                        m_InvalidIndices.Add(i);
                    }
                }
            }

            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);

            using (new CoreGUI.BoxBlock(Color.black))
            {
                if (!Predicate())
                {
                    CoreGUI.Label($"Require Rebase {objectSettings.Count} - {m_InvalidIndices.Count} != {m_AddressableCount}", TextAnchor.MiddleCenter);
                }
                else
                {
                    CoreGUI.Label("Asset matched with Addressable", TextAnchor.MiddleCenter);
                }
            }

            using (new CoreGUI.BoxBlock(Color.black))
            {
                if (m_InvalidIndices.Count > 0)
                {
                    EditorGUILayout.HelpBox("We\'ve found invalid assets in PrefabList but normally " +
                    "it is not an issue. You can ignore this", MessageType.Info);
                    CoreGUI.Label("Invalid prefab found");
                    EditorGUI.indentLevel++;

                    EditorGUI.BeginDisabledGroup(true);
                    for (int i = 0; i < m_InvalidIndices.Count; i++)
                    {
                        EditorGUILayout.PropertyField(
                            m_ObjectSettings.GetArrayElementAtIndex(m_InvalidIndices[i]),
                            new GUIContent($"Index at {m_InvalidIndices[i]}"));
                    }
                    EditorGUI.EndDisabledGroup();

                    EditorGUI.indentLevel--;
                }
                else if (m_BrokenPrefabs.Length > 0)
                {
                    EditorGUILayout.HelpBox("We\'ve found invalid assets in PrefabList with missing monoscript. This is not allowed.", MessageType.Error);

                    EditorGUI.BeginDisabledGroup(true);
                    for (int i = 0; i < m_BrokenPrefabs.Length; i++)
                    {
                        EditorGUILayout.ObjectField(
                            new GUIContent(m_BrokenPrefabs[i].name),
                            m_BrokenPrefabs[i],
                            TypeHelper.TypeOf<GameObject>.Type,
                            false);
                    }
                    EditorGUI.EndDisabledGroup();
                }
                else
                {
                    EditorGUILayout.HelpBox("All prefabs nominal", MessageType.Info);
                }
            }


            //
            EditorGUILayout.EndScrollView();
        }
        //
    }
}
