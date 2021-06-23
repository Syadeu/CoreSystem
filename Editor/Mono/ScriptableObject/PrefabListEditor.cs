using Syadeu;
using Syadeu.Mono;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;

#if UNITY_ADDRESSABLES
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine.AddressableAssets;
#endif

namespace SyadeuEditor
{
    [CustomEditor(typeof(PrefabList))]
    public class PrefabListEditor : EditorEntity<PrefabList>
    {
#if UNITY_ADDRESSABLES
        private static AddressableAssetSettings DefaultSettings => AddressableAssetSettingsDefaultObject.GetSettings(true);
        private static AddressableAssetGroup m_DefaultGroup;
        private static AddressableAssetGroup DefaultGroup
        {
            get
            {
                if (m_DefaultGroup == null)
                {
                    string[] assetGuids = AssetDatabase.FindAssets("PrefabList t:AddressableAssetGroup", new string[] { "Assets/AddressableAssetsData/AssetGroups" });
                    if (assetGuids == null || assetGuids.Length == 0)
                    {
                        m_DefaultGroup = CreateInstance<AddressableAssetGroup>();
                        m_DefaultGroup.Name = "PrefabList";
                        AssetDatabase.CreateAsset(m_DefaultGroup, "Assets/AddressableAssetsData/AssetGroups/PrefabList.asset");

                        m_DefaultGroup = AssetDatabase.LoadAssetAtPath<AddressableAssetGroup>("Assets/AddressableAssetsData/AssetGroups/PrefabList.asset");
                    }
                    else
                    {
                        m_DefaultGroup = AssetDatabase.LoadAssetAtPath<AddressableAssetGroup>(AssetDatabase.GUIDToAssetPath(assetGuids[0]));
                    }
                }
                return m_DefaultGroup;
            }
        }
        const string c_PrefabListAssetPath = "Assets/AddressableAssetsData/PrefabList";
#endif

        private bool m_ShowOriginalContents = false;
        private static string[] m_PrefabNames = null;

        public void OnEnable()
        {
            CheckConditions();
        }
        public override void OnInspectorGUI()
        {
            EditorUtils.StringHeader("Prefab List");
            EditorUtils.SectorLine();

            EditorGUI.BeginChangeCheck();

            var objSettings = serializedObject.FindProperty("m_ObjectSettings");
            EditorGUILayout.PropertyField(objSettings);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
#if UNITY_ADDRESSABLES
                List<PrefabList.ObjectSetting> list = PrefabList.Instance.ObjectSettings;
                for (int i = 0; i < list.Count; i++)
                {
                    if (!list[i].RefPrefab.IsValid())
                    {
                        if (list[i].Prefab == null)
                        {
                            $"{list[i].m_Name}: prefab null".ToLog();
                            continue;
                        }
                        string path = AssetDatabase.GetAssetPath(list[i].Prefab);
                        if (!Path.GetDirectoryName(path).Equals(c_PrefabListAssetPath))
                        {
                            if (!Directory.Exists(c_PrefabListAssetPath)) Directory.CreateDirectory(c_PrefabListAssetPath);

                            AssetDatabase.MoveAsset(path, c_PrefabListAssetPath + "/" + Path.GetFileName(path));
                            AssetDatabase.Refresh();
                            path = AssetDatabase.GetAssetPath(list[i].Prefab);
                        }

                        string guid = AssetDatabase.AssetPathToGUID(path);

                        var entry = DefaultSettings.CreateOrMoveEntry(guid, DefaultGroup);
                        list[i].RefPrefab = new AssetReferenceGameObject(guid);
                    }
                    else "none".ToLog();
                }
                EditorUtility.SetDirty(target);
#endif
            }

            EditorGUILayout.Space();
            m_ShowOriginalContents = EditorUtils.Foldout(m_ShowOriginalContents, "Original Contents");
            if (m_ShowOriginalContents) base.OnInspectorGUI();
        }
        private static void CheckConditions()
        {
            if (m_PrefabNames == null || m_PrefabNames.Length != PrefabList.Instance.ObjectSettings.Count)
            {
                IReadOnlyList<PrefabList.ObjectSetting> list = PrefabList.Instance.ObjectSettings;
                m_PrefabNames = new string[list.Count];
                for (int i = 0; i < m_PrefabNames.Length; i++)
                {
                    m_PrefabNames[i] = string.IsNullOrEmpty(list[i].m_Name) ? list[i].Prefab.name : list[i].m_Name;


                }
            }
        }

        public static bool HasPrefab(GameObject obj)
        {
            for (int i = 0; i < PrefabList.Instance.ObjectSettings.Count; i++)
            {
                if (PrefabList.Instance.ObjectSettings[i].Prefab.Equals(obj))
                {
                    return true;
                }
            }
            return false;
        }
        public static int DrawPrefabSelector(int i)
        {
            CheckConditions();
            i = EditorGUILayout.Popup("Prefab: ", i, m_PrefabNames);

            return i;
        }
        public static void DrawPrefabAdder(GameObject obj)
        {
            if (!PrefabUtility.IsPartOfAnyPrefab(obj))
            {
                if (GUILayout.Button("Create Prefab"))
                {
                    //string temp = EditorUtility.OpenFolderPanel("Set prefab save path", Application.)
                }

                return;
            }
            GameObject prefab = PrefabUtility.GetCorrespondingObjectFromSource(obj);
            EditorGUI.BeginDisabledGroup(HasPrefab(prefab));

            if (GUILayout.Button("Add Prefab"))
            {
                PrefabManager.AddRecycleObject(new PrefabList.ObjectSetting()
                {
                    m_Name = prefab.name,
                    Prefab = prefab,
                });
            }

            EditorGUI.EndDisabledGroup();
        }
    }
}
