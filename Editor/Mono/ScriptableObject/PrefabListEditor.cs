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
        //#if UNITY_ADDRESSABLES
        private static AddressableAssetSettings DefaultSettings => AddressableAssetSettingsDefaultObject.GetSettings(true);
        private static AddressableAssetGroup m_DefaultGroup;
        private static AddressableAssetGroup DefaultGroup
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
        //#endif

        private bool m_ShowOriginalContents = false;
        private static string[] m_PrefabNames = null;

        //public void OnEnable()
        //{
        //    CheckConditions();
        //}
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
//#if UNITY_ADDRESSABLES
//                List<PrefabList.ObjectSetting> list = PrefabList.Instance.ObjectSettings;
//                for (int i = 0; i < list.Count; i++)
//                {
//                    string path = AssetDatabase.GetAssetPath(list[i].m_Prefab);
//                    AddressableAssetEntry entry = null;

//                    if (!list[i].m_RefPrefab.IsValid() || !Path.GetDirectoryName(path).Equals(c_PrefabListAssetPath))
//                    {
//                        if (list[i].m_Prefab == null)
//                        {
//                            $"{list[i].m_Name}: prefab null".ToLog();
//                            continue;
//                        }
                        
//                        if (!Path.GetDirectoryName(path).Equals(c_PrefabListAssetPath))
//                        {
//                            if (!Directory.Exists(c_PrefabListAssetPath)) Directory.CreateDirectory(c_PrefabListAssetPath);

//                            AssetDatabase.MoveAsset(path, c_PrefabListAssetPath + "/" + Path.GetFileName(path));
//                            AssetDatabase.Refresh();
//                            path = AssetDatabase.GetAssetPath(list[i].m_Prefab);
//                        }

//                        string guid = AssetDatabase.AssetPathToGUID(path);

//                        entry = DefaultSettings.CreateOrMoveEntry(guid, DefaultGroup);
//                        list[i].m_RefPrefab = new AssetReferenceGameObject(guid);
//                    }
//                    else "none".ToLog();

//                    entry = DefaultGroup.GetAssetEntry(list[i].m_RefPrefab.AssetGUID);
//                    string dirName = Path.GetDirectoryName(entry.address);
//                    if (!dirName.Equals(c_PrefabListAssetPath))
//                    {
//                        entry.address = $"{c_PrefabListAssetPath}/{Path.GetFileName(path)}";
//                    }
//                }
//                EditorUtility.SetDirty(DefaultSettings);
//                EditorUtility.SetDirty(DefaultGroup);
//                EditorUtility.SetDirty(target);
//#endif
            }

            EditorGUILayout.Space();
            m_ShowOriginalContents = EditorUtils.Foldout(m_ShowOriginalContents, "Original Contents");
            if (m_ShowOriginalContents) base.OnInspectorGUI();
        }
        //private static void CheckConditions()
        //{
        //    if (m_PrefabNames == null || m_PrefabNames.Length != PrefabList.Instance.ObjectSettings.Count)
        //    {
        //        m_PrefabNames = new string[PrefabList.Instance.ObjectSettings.Count];
        //    }
        //    for (int i = 0; i < m_PrefabNames.Length; i++)
        //    {
        //        m_PrefabNames[i] = string.IsNullOrEmpty(PrefabList.Instance.ObjectSettings[i].m_Name) ? 
        //            PrefabList.Instance.ObjectSettings[i].m_Prefab.name : PrefabList.Instance.ObjectSettings[i].m_Name;
        //    }
        //}

        //public static bool HasPrefab(GameObject obj)
        //{
        //    for (int i = 0; i < PrefabList.Instance.ObjectSettings.Count; i++)
        //    {
        //        if (PrefabList.Instance.ObjectSettings[i].m_Prefab.Equals(obj))
        //        {
        //            return true;
        //        }
        //    }
        //    return false;
        //}
        //public static int DrawPrefabSelector(int i)
        //{
        //    CheckConditions();
        //    i = EditorGUILayout.Popup("Prefab: ", i, m_PrefabNames);

        //    return i;
        //}
        //public static void DrawPrefabAdder(GameObject obj)
        //{
        //    if (!PrefabUtility.IsPartOfAnyPrefab(obj))
        //    {
        //        if (GUILayout.Button("Create Prefab"))
        //        {
        //            string temp = EditorUtility.SaveFilePanel("Set prefab save path", Application.dataPath, "NewCreaturePrefab", "prefab");
        //            $"{temp} : not implemented".ToLog();
        //        }

        //        return;
        //    }
        //    GameObject prefab = PrefabUtility.GetCorrespondingObjectFromSource(obj);
        //    EditorGUI.BeginDisabledGroup(HasPrefab(prefab));

        //    if (GUILayout.Button("Add Prefab"))
        //    {
        //        PrefabList.Instance.ObjectSettings.Add(new PrefabList.ObjectSetting()
        //        {
        //            m_Name = prefab.name,
        //            m_Prefab = prefab,
        //        });
        //        EditorUtility.SetDirty(PrefabList.Instance);
        //    }

        //    EditorGUI.EndDisabledGroup();
        //}
    }
}
