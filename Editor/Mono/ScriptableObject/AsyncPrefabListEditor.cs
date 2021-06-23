using Syadeu;
using Syadeu.Mono;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SyadeuEditor
{
#if UNITY_ADDRESSABLES
    [CustomEditor(typeof(AsyncPrefabList))]
    public sealed class AsyncPrefabListEditor : EditorEntity<AsyncPrefabList>
    {
        private AddressableAssetSettings m_DefaultSettings;
        private AddressableAssetGroup m_DefaultGroup;

        private bool m_ShowOriginalContents = false;

        private void OnEnable()
        {
            m_DefaultSettings = AddressableAssetSettingsDefaultObject.GetSettings(false);

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

            $"{m_DefaultGroup.name}".ToLog();
        }

        GameObject testObj;
        public override void OnInspectorGUI()
        {
            testObj = (GameObject)EditorGUILayout.ObjectField("test: ", testObj, typeof(GameObject), false);
            if (GUILayout.Button("Test"))
            {
                add(testObj);
            }

            m_ShowOriginalContents = EditorUtils.Foldout(m_ShowOriginalContents, "Original Contents");
            if (m_ShowOriginalContents) base.OnInspectorGUI();
        }

        private void add(GameObject gameObject)
        {
            string path = AssetDatabase.GetAssetPath(gameObject);
            string guid = AssetDatabase.AssetPathToGUID(path);

            m_DefaultSettings.CreateOrMoveEntry(guid, m_DefaultGroup);
        }
    }
#endif
}
