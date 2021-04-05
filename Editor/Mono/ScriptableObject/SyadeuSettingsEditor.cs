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
        const string CORESYSTEM_UNSAFE = "CORESYSTEM_UNSAFE";
        const string CORESYSTEM_FMOD = "CORESYSTEM_FMOD";
        bool m_DefineUnsafe = true;
        bool m_DefineFmod = true;


        bool m_EnableHelpbox = false;

        bool m_GlobalOption = true;

        bool m_ShowOriginalContents = false;

        private void OnEnable()
        {
            if (SyadeuSettings.Instance.m_UserTagNameModule == null)
            {
                var userTag = CreateInstance<UserTagNameModule>();
                userTag.name = "UserTagNameModule";
                AssetDatabase.AddObjectToAsset(userTag, SyadeuSettings.Instance);
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(SyadeuSettings.Instance));

                SyadeuSettings.Instance.m_UserTagNameModule = userTag;
                EditorUtility.SetDirty(SyadeuSettings.Instance);

                AssetDatabase.SaveAssets();
            }
            if (SyadeuSettings.Instance.m_CustomTagNameModule == null)
            {
                var customTag = CreateInstance<CustomTagNameModule>();
                customTag.name = "CustomTagNameModule";
                AssetDatabase.AddObjectToAsset(customTag, SyadeuSettings.Instance);
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(SyadeuSettings.Instance));

                SyadeuSettings.Instance.m_CustomTagNameModule = customTag;
                EditorUtility.SetDirty(SyadeuSettings.Instance);

                AssetDatabase.SaveAssets();
            }

            m_DefineUnsafe = m_DefineFmod = true;
            PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, out string[] temp);
            if (!temp.Contains(CORESYSTEM_UNSAFE))
            {
                m_DefineUnsafe = false;
            }
            if (!temp.Contains(CORESYSTEM_FMOD))
            {
                m_DefineFmod = false;
            }

        }
        public override void OnInspectorGUI()
        {
            EditorUtils.StringHeader("CoreSystem Setting");
            EditorUtils.SectorLine();

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
            EditorGUI.BeginChangeCheck();
            m_DefineUnsafe = EditorGUILayout.ToggleLeft("Unsafe 설정", m_DefineUnsafe);
            m_DefineFmod = EditorGUILayout.ToggleLeft("Fmod 설정", m_DefineFmod);
            if (EditorGUI.EndChangeCheck())
            {
                PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, out string[] temp);
                List<string> temptemp = new List<string>(temp);
                if (m_DefineUnsafe)
                {
                    if (!temp.Contains(CORESYSTEM_UNSAFE))
                    {
                        temptemp.Add(CORESYSTEM_UNSAFE);
                    }
                }
                else
                {
                    if (temp.Contains(CORESYSTEM_UNSAFE))
                    {
                        temptemp.Remove(CORESYSTEM_UNSAFE);
                    }
                }

                if (m_DefineFmod)
                {
                    if (!temp.Contains(CORESYSTEM_FMOD))
                    {
                        temptemp.Add(CORESYSTEM_FMOD);
                    }
                }
                else
                {
                    if (temp.Contains(CORESYSTEM_FMOD))
                    {
                        temptemp.Remove(CORESYSTEM_FMOD);
                    }
                }

                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, temptemp.ToArray());
            }

            EditorGUILayout.Space();
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
