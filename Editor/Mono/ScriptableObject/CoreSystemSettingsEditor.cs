﻿//using NUnit.Framework;
//using Syadeu;
//using Syadeu.Mono;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using UnityEditor;
//using UnityEngine;

//namespace SyadeuEditor
//{
//    [CustomEditor(typeof(CoreSystemSettings))]
//    public class CoreSystemSettingsEditor : Editor
//    {
//        public const string CORESYSTEM_UNSAFE = "CORESYSTEM_UNSAFE";
//        public const string CORESYSTEM_FMOD = "CORESYSTEM_FMOD";
//        public const string CORESYSTEM_UNITYAUDIO = "CORESYSTEM_UNITYAUDIO";
//        bool m_DefineUnsafe = true;
//        bool m_DefineFmod = true;
//        bool m_DefineUnityAudio = true;

//        bool m_EnableHelpbox = false;

//        bool m_GlobalOption = true;

//        bool m_ShowOriginalContents = false;

//        private void OnEnable()
//        {
//            if (CoreSystemSettings.Instance.m_UserTagNameModule == null)
//            {
//                var userTag = CreateInstance<UserTagNameModule>();
//                userTag.name = "UserTagNameModule";
//                AssetDatabase.AddObjectToAsset(userTag, CoreSystemSettings.Instance);
//                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(CoreSystemSettings.Instance));

//                CoreSystemSettings.Instance.m_UserTagNameModule = userTag;
//                EditorUtility.SetDirty(CoreSystemSettings.Instance);

//                AssetDatabase.SaveAssets();
//            }
//            if (CoreSystemSettings.Instance.m_CustomTagNameModule == null)
//            {
//                var customTag = CreateInstance<CustomTagNameModule>();
//                customTag.name = "CustomTagNameModule";
//                AssetDatabase.AddObjectToAsset(customTag, CoreSystemSettings.Instance);
//                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(CoreSystemSettings.Instance));

//                CoreSystemSettings.Instance.m_CustomTagNameModule = customTag;
//                EditorUtility.SetDirty(CoreSystemSettings.Instance);

//                AssetDatabase.SaveAssets();
//            }

//            m_DefineUnsafe = m_DefineFmod = m_DefineUnityAudio = true;
//            PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, out string[] temp);
//            if (!temp.Contains(CORESYSTEM_UNSAFE))
//            {
//                m_DefineUnsafe = false;
//            }
//            if (!temp.Contains(CORESYSTEM_FMOD))
//            {
//                m_DefineFmod = false;
//            }
//            if (!temp.Contains(CORESYSTEM_UNITYAUDIO))
//            {
//                m_DefineUnityAudio = false;
//            }
//        }
//        public override void OnInspectorGUI()
//        {
//            EditorUtils.StringHeader("CoreSystem Setting");
//            EditorUtils.SectorLine();

//            m_EnableHelpbox = EditorGUILayout.ToggleLeft("도움말 표시", m_EnableHelpbox);
//            EditorGUILayout.Space();

//            EditorGUI.BeginChangeCheck();

//            m_GlobalOption = EditorUtils.Foldout(m_GlobalOption, "Global Option", 15);
//            if (m_GlobalOption) GlobalSettings();
//            EditorUtils.SectorLine();

//            if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(CoreSystemSettings.Instance);

//            EditorGUILayout.Space();
//            m_ShowOriginalContents = EditorUtils.Foldout(m_ShowOriginalContents, "Original Contents");
//            if (m_ShowOriginalContents) base.OnInspectorGUI();
//        }

//        private void GlobalSettings()
//        {
//            CoreSystemSettings.Instance.m_DisplayLogChannel = (Channel)EditorGUILayout.EnumFlagsField("Display Log Channel: ", CoreSystemSettings.Instance.m_DisplayLogChannel);

//            EditorGUI.BeginChangeCheck();
//            if (m_EnableHelpbox)
//            {
//                EditorGUILayout.HelpBox(
//                    "PlayerSetting 스크립트 Define에 CORESYSTEM_UNSAFE 를 추가/제거합니다.\n" +
//                    "활성화시 포인터를 이용한 메소드를 사용가능합니다.",
//                    MessageType.Info);
//            }
//            EditorGUI.BeginDisabledGroup(!PlayerSettings.allowUnsafeCode);
//            m_DefineUnsafe = EditorGUILayout.ToggleLeft("DEFINE CORESYSTEM_UNSAFE", m_DefineUnsafe);
//            EditorGUI.EndDisabledGroup();
//            if (m_EnableHelpbox)
//            {
//                EditorGUILayout.HelpBox(
//                    "PlayerSetting 스크립트 Define에 CORESYSTEM_FMOD 를 추가/제거합니다.\n" +
//                    "활성화전 FMOD Unity Integration package를 설치하세요.\n" +
//                    "FMOD 사용을 위한 컬렉션이 제공됩니다.",
//                    MessageType.Info);
//            }
//            EditorGUI.BeginDisabledGroup(m_DefineUnityAudio);
//            m_DefineFmod = EditorGUILayout.ToggleLeft("DEFINE CORESYSTEM_FMOD", m_DefineFmod);
//            EditorGUI.EndDisabledGroup();
//            if (m_EnableHelpbox)
//            {
//                EditorGUILayout.HelpBox("", MessageType.Info);
//            }
//            EditorGUI.BeginDisabledGroup(m_DefineFmod);
//            m_DefineUnityAudio = EditorGUILayout.ToggleLeft("DEFINE CORESYSTEM_UNITYAUDIO", m_DefineUnityAudio);
//            EditorGUI.EndDisabledGroup();
//            if (EditorGUI.EndChangeCheck())
//            {
//                PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, out string[] temp);
//                List<string> temptemp = new List<string>(temp);
//                if (m_DefineUnsafe)
//                {
//                    if (!temp.Contains(CORESYSTEM_UNSAFE)) temptemp.Add(CORESYSTEM_UNSAFE);
//                }
//                else
//                {
//                    if (temp.Contains(CORESYSTEM_UNSAFE)) temptemp.Remove(CORESYSTEM_UNSAFE);
//                }

//                if (m_DefineFmod)
//                {
//                    if (!temp.Contains(CORESYSTEM_FMOD)) temptemp.Add(CORESYSTEM_FMOD);
//                }
//                else
//                {
//                    if (temp.Contains(CORESYSTEM_FMOD)) temptemp.Remove(CORESYSTEM_FMOD);
//                }

//                if (m_DefineUnityAudio)
//                {
//                    if (!temp.Contains(CORESYSTEM_UNITYAUDIO)) temptemp.Add(CORESYSTEM_UNITYAUDIO);
//                }
//                else
//                {
//                    if (temp.Contains(CORESYSTEM_UNITYAUDIO)) temptemp.Remove(CORESYSTEM_UNITYAUDIO);
//                }

//                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, temptemp.ToArray());
//                UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
//            }

//            EditorGUILayout.Space();
//            if (m_EnableHelpbox)
//            {
//                EditorGUILayout.HelpBox(
//                    "활성화시, StaticManager, MonoManager 의 HideInHierarchy 설정을 무시하고, " +
//                    "전부 강제로 Hierarchy에 표시시킵니다.", MessageType.Info);
//            }

//            CoreSystemSettings.Instance.m_VisualizeObjects =
//                EditorGUILayout.ToggleLeft("전부 Hierarchy에 표시", CoreSystemSettings.Instance.m_VisualizeObjects);

//            if (m_EnableHelpbox)
//            {
//                EditorGUILayout.HelpBox(
//                    "게임내 Exception이 raise 된 후, 게임을 강제로 크래쉬 시킵니다. " +
//                    "에디터에서는 작동하지 않고, 빌드에서만 작동합니다.", MessageType.Info);


//                CoreSystemSettings.Instance.m_CrashAfterException =
//                    EditorGUILayout.ToggleLeft("에러 발생 후 강제 크래쉬", CoreSystemSettings.Instance.m_CrashAfterException);

//                //EditorGUILayout.Space();
//                //for (int i = 0; i < m_ManagerNames.Count; i++)
//                //{
//                //    EditorGUILayout.LabelField(m_ManagerNames[i]);
//                //}
//            }
//        }

//        public static bool IsDefined(string defineWord)
//        {
//            PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, out string[] temp);
//            return temp.Contains(defineWord);
//        }
//        public static void Define(string defineWord)
//        {
//            PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, out string[] temp);
//            if (!temp.Contains(defineWord))
//            {
//                List<string> defines = new List<string>(temp);
//                defines.Add(defineWord);
//                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, defines.ToArray());
//            }
//        }
//    }
//}
