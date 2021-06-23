using Syadeu;
using Syadeu.Mono;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace SyadeuEditor
{
#if UNITY_ADDRESSABLES
    [CustomEditor(typeof(AsyncPrefabList))]
    public sealed class AsyncPrefabListEditor : EditorEntity<AsyncPrefabList>
    {
        private AddressableAssetGroup m_DefaultGroup;

        private bool m_ShowOriginalContents = false;

        private void OnEnable()
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

            $"{m_DefaultGroup.name}".ToLog();
        }

        public override void OnInspectorGUI()
        {
            m_ShowOriginalContents = EditorUtils.Foldout(m_ShowOriginalContents, "Original Contents");
            if (m_ShowOriginalContents) base.OnInspectorGUI();
        }
    }
#endif
}
