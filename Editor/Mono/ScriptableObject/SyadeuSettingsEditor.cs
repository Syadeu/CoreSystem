using NUnit.Framework;
using Syadeu;
using Syadeu.Mono;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor
{
    [CustomEditor(typeof(SyadeuSettings))]
    public class SyadeuSettingsEditor : Editor
    {
        bool m_EnableHelpbox = false;
        //public List<string> m_ManagerNames = new List<string>();

        bool m_UserTag = false;
        bool m_CustomTag = false;

        bool m_GlobalOption = true;

        bool m_ShowOriginalContents = false;

        //private void OnEnable()
        //{
        //    if (SyadeuSettings.Instance.m_UserTagNameModule == null)
        //    {
        //        var userTag = CreateInstance<UserTagNameModule>();
        //        userTag.name = "UserTagNameModule";
        //        AssetDatabase.AddObjectToAsset(userTag, SyadeuSettings.Instance);
        //        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(SyadeuSettings.Instance));

        //        SyadeuSettings.Instance.m_UserTagNameModule = userTag;
        //        EditorUtility.SetDirty(SyadeuSettings.Instance);

        //        AssetDatabase.SaveAssets();
        //    }
        //    if (SyadeuSettings.Instance.m_CustomTagNameModule == null)
        //    {
        //        var customTag = CreateInstance<CustomTagNameModule>();
        //        customTag.name = "CustomTagNameModule";
        //        AssetDatabase.AddObjectToAsset(customTag, SyadeuSettings.Instance);
        //        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(SyadeuSettings.Instance));

        //        SyadeuSettings.Instance.m_CustomTagNameModule = customTag;
        //        EditorUtility.SetDirty(SyadeuSettings.Instance);

        //        AssetDatabase.SaveAssets();
        //    }

        //    //Type[] types = typeof(CoreSystem).Assembly.GetTypes().Where(TheType => TheType.IsClass && !TheType.IsAbstract && TheType.GetInterface("IStaticManager") != null).ToArray();
        //    //{
        //    //    m_ManagerNames.Clear();
        //    //    for (int i = 0; i < types.Length; i++)
        //    //    {
        //    //        m_ManagerNames.Add(types[i].Name);
        //    //    }
        //    //}

        //}
        public override void OnInspectorGUI()
        {
            EditorUtils.StringHeader("CoreSystem Setting");
            EditorUtils.SectorLine();

            if (SyadeuSettings.Instance.m_UserTagNameModule == null)
            {
                if (GUILayout.Button("Add UserTagNameModule"))
                {
                    var userTag = CreateInstance<UserTagNameModule>();
                    userTag.name = "UserTagNameModule";
                    AssetDatabase.AddObjectToAsset(userTag, SyadeuSettings.Instance);
                    AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(SyadeuSettings.Instance));

                    SyadeuSettings.Instance.m_UserTagNameModule = userTag;
                    EditorUtility.SetDirty(SyadeuSettings.Instance);

                    AssetDatabase.SaveAssets();
                }
            }
            if (SyadeuSettings.Instance.m_CustomTagNameModule == null)
            {
                if (GUILayout.Button("Add CustomTagNameModule"))
                {
                    var customTag = CreateInstance<CustomTagNameModule>();
                    customTag.name = "CustomTagNameModule";
                    AssetDatabase.AddObjectToAsset(customTag, SyadeuSettings.Instance);
                    AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(SyadeuSettings.Instance));

                    SyadeuSettings.Instance.m_CustomTagNameModule = customTag;
                    EditorUtility.SetDirty(SyadeuSettings.Instance);

                    AssetDatabase.SaveAssets();
                }
            }

            m_EnableHelpbox = EditorGUILayout.ToggleLeft("도움말 표시", m_EnableHelpbox);
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();

            m_GlobalOption = EditorUtils.Foldout(m_GlobalOption, "Global Option", 15);
            if (m_GlobalOption) GlobalSettings();
            EditorUtils.SectorLine();

            if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(SyadeuSettings.Instance);

            EditorGUILayout.Space();
            m_ShowOriginalContents = EditorUtils.Foldout(m_ShowOriginalContents, "Original Contents");
            if (m_ShowOriginalContents) base.OnInspectorGUI();
        }

        private void GlobalSettings()
        {
            if (m_EnableHelpbox)
            {
                EditorGUILayout.HelpBox(
                    "활성화시, StaticManager, MonoManager 의 HideInHierarchy 설정을 무시하고, " +
                    "전부 강제로 Hierarchy에 표시시킵니다.", MessageType.Info);
            }

            SyadeuSettings.Instance.m_VisualizeObjects =
                EditorGUILayout.ToggleLeft("전부 Hierarchy에 표시", SyadeuSettings.Instance.m_VisualizeObjects);

            if (m_EnableHelpbox)
            {
                EditorGUILayout.HelpBox(
                    "게임내 Exception이 raise 된 후, 게임을 강제로 크래쉬 시킵니다. " +
                    "에디터에서는 작동하지 않고, 빌드에서만 작동합니다.", MessageType.Info);
            }

            SyadeuSettings.Instance.m_CrashAfterException =
                EditorGUILayout.ToggleLeft("에러 발생 후 강제 크래쉬", SyadeuSettings.Instance.m_CrashAfterException);

            //EditorGUILayout.Space();
            //for (int i = 0; i < m_ManagerNames.Count; i++)
            //{
            //    EditorGUILayout.LabelField(m_ManagerNames[i]);
            //}
        }
    }
}
