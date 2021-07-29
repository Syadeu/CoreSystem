using Syadeu;
using Syadeu.Mono;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

#if UNITY_ADDRESSABLES
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine.AddressableAssets;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
#endif

namespace SyadeuEditor
{
    [CustomEditor(typeof(PrefabList))]
    public class PrefabListEditor : EditorEntity<PrefabList>
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
                    if (tempList.Count() == 0)
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

        private bool m_ShowOriginalContents = false;

        public override void OnInspectorGUI()
        {
            EditorUtils.StringHeader("Prefab List");
            EditorUtils.SectorLine();

            var objSettings = serializedObject.FindProperty("m_ObjectSettings");

            if (GUILayout.Button("Rebase"))
            {
                var objSetField = GetField("m_ObjectSettings");
                List<PrefabList.ObjectSetting> tempList = new List<PrefabList.ObjectSetting>();

                foreach (AddressableAssetEntry item in DefaultGroup.entries)
                {
                    AssetReferenceGameObject refObj = new AssetReferenceGameObject(item.guid);

                    tempList.Add(new PrefabList.ObjectSetting
                    {
                        m_Name = item.address.Split('/').Last().Split('.').First(),
                        m_RefPrefab = refObj
                    });
                }

                objSetField.SetValue(Asset, tempList);
                EditorUtility.SetDirty(target);
                Repaint();
            }

            EditorGUI.BeginChangeCheck();

            
            EditorGUILayout.PropertyField(objSettings);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.Space();
            m_ShowOriginalContents = EditorUtils.Foldout(m_ShowOriginalContents, "Original Contents");
            if (m_ShowOriginalContents) base.OnInspectorGUI();
        }
    }
}
