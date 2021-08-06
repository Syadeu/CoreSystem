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

                Rebase(origin);

                EditorUtility.SetDirty(target);
                Repaint();
            }

            EditorGUI.BeginDisabledGroup(true);
            base.OnInspectorGUI();
            EditorGUI.EndDisabledGroup();
        }

        public static void Rebase(List<PrefabList.ObjectSetting> list)
        {
            Queue<int> invalidIndices = new Queue<int>();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].m_RefPrefab.editorAsset == null)
                {
                    CoreSystem.Logger.Log(Channel.Editor,
                        $"PrefabList found an valid asset at {i}:{list[i].m_Name}");

                    list[i].m_Name = $"!!INVALID!! {list[i].m_Name}";
                    invalidIndices.Enqueue(i);
                }
            }

            foreach (AddressableAssetEntry item in DefaultGroup.entries)
            {
                string name = item.address.Split('/').Last().Split('.').First();
                if (list.Where((other) => other.m_Name.Equals(name)).Any())
                {
                    continue;
                }

                AssetReference refObj = new AssetReference(item.guid);
                if (invalidIndices.Count > 0)
                {
                    int targetIdx = invalidIndices.Dequeue();
                    string previousName = list[targetIdx].m_Name;

                    list[targetIdx] = new PrefabList.ObjectSetting
                    {
                        m_Name = name,
                        m_RefPrefab = refObj
                    };

                    CoreSystem.Logger.Log(Channel.Editor,
                        $"PrefabList index at {targetIdx}:{previousName} was invalid but replaced to newly added prefab");
                }
                else
                {
                    list.Add(new PrefabList.ObjectSetting
                    {
                        m_Name = name,
                        m_RefPrefab = refObj
                    });
                }
            }
        }
    }
}
