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

        public override void OnInspectorGUI()
        {
            EditorUtils.StringHeader("Prefab List");
            EditorUtils.SectorLine();

            if (GUILayout.Button("Rebase"))
            {
                var objSetField = GetField("m_ObjectSettings");
                List<PrefabList.ObjectSetting> origin = (List<PrefabList.ObjectSetting>)objSetField.GetValue(Asset);

                for (int i = 0; i < origin.Count; i++)
                {
                    if (origin[i].m_RefPrefab.editorAsset == null)
                    {
                        origin[i].m_Name = $"!!INVALID!! {origin[i].m_Name}";
                    }
                }

                //List<PrefabList.ObjectSetting> tempList = new List<PrefabList.ObjectSetting>();

                foreach (AddressableAssetEntry item in DefaultGroup.entries)
                {
                    string name = item.address.Split('/').Last().Split('.').First();
                    if (origin.Where((other) => other.m_Name.Equals(name)).Any())
                    {
                        continue;
                    }

                    //if (item.TargetAsset is GameObject)
                    {
                        AssetReference refObj = new AssetReference(item.guid);

                        origin.Add(new PrefabList.ObjectSetting
                        {
                            m_Name = name,
                            m_RefPrefab = refObj
                        });
                    }
                }

                //objSetField.SetValue(Asset, tempList);
                EditorUtility.SetDirty(target);
                Repaint();
            }

            base.OnInspectorGUI();
        }
    }
}
