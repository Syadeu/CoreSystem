#if CORESYSTEM_FMOD
using Syadeu.FMOD;
#elif CORESYSTEM_UNITYAUDIO
#endif

using Syadeu;
using Syadeu.Collections;
using Syadeu.Internal;
using Syadeu.Mono;
using Syadeu.Presentation;
using SyadeuEditor.Presentation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SyadeuEditor
{
    public sealed class CoreSystemSetupWizard : EditorWindow, IStaticInitializer
    {
        static CoreSystemSetupWizard()
        {
            EditorApplication.delayCall -= Startup;
            EditorApplication.delayCall += Startup;
        }
        static void Startup()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;

            if (!new GeneralMenu().Predicate() ||
                !new SceneMenu().Predicate() ||
                !new PrefabMenu().Predicate())
            {
                CoreSystemMenuItems.CoreSystemSetupWizard();
                return;
            }

            if (!CoreSystemSettings.Instance.m_HideSetupWizard)
            {
                CoreSystemMenuItems.CoreSystemSetupWizard();
            }
        }
        public enum ToolbarNames
        {
            General,
            Scene,
            Prefab,
            Test2,
            Test3,
            Test4,
        }

        private Texture2D m_EnableTexture;
        private Texture2D m_DisableTexture;

        GUIStyle titleStyle;
        GUIStyle iconStyle;

        private GeneralMenu m_GeneralMenu;
        private SceneMenu m_SceneMenu;
        private PrefabMenu m_PrefabMenu;
        private Rect m_CopyrightRect = new Rect(175, 475, 245, 20);

        private void OnEnable()
        {
            m_DisableTexture = EditorUtils.LoadAsset<Texture2D>("CrossYellow", "CoreSystemEditor");
            m_EnableTexture = EditorUtils.LoadAsset<Texture2D>("TickGreen", "CoreSystemEditor");

            titleStyle = new GUIStyle();
            titleStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            titleStyle.wordWrap = true;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.MiddleCenter;

            iconStyle = new GUIStyle();
            iconStyle.alignment = TextAnchor.MiddleCenter;

            m_GeneralMenu = new GeneralMenu();
            m_SceneMenu = new SceneMenu();
            m_PrefabMenu = new PrefabMenu();
            AddSetup(ToolbarNames.General, m_GeneralMenu.Predicate);
            AddSetup(ToolbarNames.Scene, m_SceneMenu.Predicate);
            AddSetup(ToolbarNames.Prefab, m_PrefabMenu.Predicate);

            CoreSystemSettings.Instance.m_HideSetupWizard = true;
            EditorUtility.SetDirty(CoreSystemSettings.Instance);
        }
        private void OnGUI()
        {
            GUILayout.Space(20);
            EditorUtils.StringHeader("Setup", 30, true);
            GUILayout.Space(10);
            EditorUtils.Line();
            GUILayout.Space(10);

            DrawToolbar();

            EditorUtils.Line();

            using (new EditorUtils.BoxBlock(Color.black))
            {
                switch ((ToolbarNames)m_SelectedToolbar)
                {
                    case ToolbarNames.General:
                        m_GeneralMenu.OnGUI();
                        break;
                    case ToolbarNames.Scene:
                        m_SceneMenu.OnGUI();
                        break;
                    case ToolbarNames.Prefab:
                        m_PrefabMenu.OnGUI();
                        break;
                    default:
                        break;
                }
            }

            EditorGUI.LabelField(m_CopyrightRect, EditorUtils.String("Copyright 2021 Syadeu. All rights reserved.", 11), EditorUtils.CenterStyle);
        }

        public ToolbarNames SelectedToolbar => (ToolbarNames)m_SelectedToolbar;
        private void AddSetup(ToolbarNames toolbar, Func<bool> predictate)
        {
            m_IsSetupDone.Add(toolbar, predictate);
        }

        private int m_SelectedToolbar = 0;
        #region Toolbar

        private readonly Dictionary<ToolbarNames, Func<bool>> m_IsSetupDone = new Dictionary<ToolbarNames, Func<bool>>();
        private void DrawToolbar()
        {
            const float spacing = 50;

            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            GUILayout.Space(spacing);

            string[] toolbarNames = Enum.GetNames(typeof(ToolbarNames));
            for (int i = 0; i < toolbarNames.Length; i++)
            {
                bool done;
                if (m_IsSetupDone.ContainsKey((ToolbarNames)i))
                {
                    done = m_IsSetupDone[(ToolbarNames)i].Invoke();
                }
                else done = true;
                DrawToolbarButton(i, toolbarNames[i], done);
            }

            GUILayout.Space(spacing);
            EditorGUILayout.EndHorizontal();
        }
        private void DrawToolbarButton(int i, string name, bool enable)
        {
            using (new EditorUtils.BoxBlock(i.Equals(m_SelectedToolbar) ? Color.black : Color.gray))
            {
                EditorGUILayout.BeginHorizontal(GUILayout.Height(22));
                if (GUILayout.Button(name, titleStyle))
                {
                    m_SelectedToolbar = i;
                }
                GUILayout.Label(enable ? m_EnableTexture : m_DisableTexture, iconStyle);
                EditorGUILayout.EndHorizontal();
            }
        }
        #endregion

        #region General Menu
        private sealed class GeneralMenu
        {
            #region Constrains

            const string
                UNITY_COLLECTIONS_CHECKS = "ENABLE_UNITY_COLLECTIONS_CHECKS",
                CORESYSTEM_DISABLE_CHECKS = "CORESYSTEM_DISABLE_CHECKS",

                CORESYSTEM_TURNBASESYSTEM = "CORESYSTEM_TURNBASESYSTEM",

                CORESYSTEM_DOTWEEN = "CORESYSTEM_DOTWEEN",
                CORESYSTEM_MOTIONMATCHING = "CORESYSTEM_MOTIONMATCHING",
                CORESYSTEM_BEHAVIORTREE = "CORESYSTEM_BEHAVIORTREE",
                CORESYSTEM_FMOD = "CORESYSTEM_FMOD";
            bool
                m_DefinedCollectionsChecks, 
                m_DefinedCoresystemChecks,

                m_DefinedTurnBasedSystem,

                m_DefinedDotween,
                m_DefinedMotionMatching,
                m_DefinedBehaviorTree,
                m_DefinedFMOD;
            List<string> m_DefinedConstrains;

            #endregion

            #region Tag Manager
            SerializedObject m_TagManagerObject;
            SerializedProperty m_TagProperty, m_LayerProperty;

            static string[] c_RequireTags = new string[] { };
            static string[] c_RequireLayers = new string[] { "Terrain" };

            List<string> m_MissingTags, m_MissingLayers;

            #endregion

            CoreSystemSettings m_CoreSystemSettings;

            #region Unity Audio

            SerializedObject m_UnityAudioManager;
            SerializedProperty
                m_UnityAudioDisableAudio,

                m_UnityAudioGlobalVolume,
                m_UnityAudioRolloffScale,
                m_UnityAudioDopplerFactor,

                m_UnityAudioRealVoiceCount,
                m_UnityAudioVirtualVoiceCount,
                m_UnityAudioDefaultSpeakerMode;

            #endregion

            public GeneralMenu()
            {
                #region Constrains

                PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, out string[] temp);
                m_DefinedConstrains = temp.ToList();

                m_DefinedCollectionsChecks = HasConstrains(UNITY_COLLECTIONS_CHECKS);
                m_DefinedCoresystemChecks = HasConstrains(CORESYSTEM_DISABLE_CHECKS);

                m_DefinedTurnBasedSystem = HasConstrains(CORESYSTEM_TURNBASESYSTEM);

                m_DefinedDotween = HasConstrains(CORESYSTEM_DOTWEEN);
                m_DefinedMotionMatching = HasConstrains(CORESYSTEM_MOTIONMATCHING);
                m_DefinedBehaviorTree = HasConstrains(CORESYSTEM_BEHAVIORTREE);
                m_DefinedFMOD = HasConstrains(CORESYSTEM_FMOD);

                #endregion

                #region Tag Manager

                UnityEngine.Object tagManagerObject = AssetDatabase.LoadMainAssetAtPath("ProjectSettings/TagManager.asset");
                m_TagManagerObject = new SerializedObject(tagManagerObject);
                m_TagProperty = m_TagManagerObject.FindProperty("tags");
                m_LayerProperty = m_TagManagerObject.FindProperty("layers");

                m_MissingTags = new List<string>(c_RequireTags);
                for (int i = 0; i < m_TagProperty.arraySize; i++)
                {
                    string value = m_TagProperty.GetArrayElementAtIndex(i).stringValue;
                    if (string.IsNullOrEmpty(value)) continue;

                    m_MissingTags.Remove(value);
                }

                m_MissingLayers = new List<string>(c_RequireLayers);
                for (int i = 0; i < m_LayerProperty.arraySize; i++)
                {
                    string value = m_LayerProperty.GetArrayElementAtIndex(i).stringValue;
                    if (string.IsNullOrEmpty(value)) continue;

                    m_MissingLayers.Remove(value);
                }

                if (m_MissingTags.Count > 0 || m_MissingLayers.Count > 0)
                {
                    m_OpenTagManager = true;
                }

                #endregion

                m_CoreSystemSettings = CoreSystemSettings.Instance;

                #region Unity Audio

                var audioManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/AudioManager.asset")[0];
                m_UnityAudioManager = new SerializedObject(audioManager);

                m_UnityAudioDisableAudio = m_UnityAudioManager.FindProperty("m_DisableAudio");

                m_UnityAudioGlobalVolume = m_UnityAudioManager.FindProperty("m_Volume");
                m_UnityAudioRolloffScale = m_UnityAudioManager.FindProperty("Rolloff Scale");
                m_UnityAudioDopplerFactor = m_UnityAudioManager.FindProperty("Doppler Factor");

                m_UnityAudioRealVoiceCount = m_UnityAudioManager.FindProperty("m_RealVoiceCount");
                m_UnityAudioVirtualVoiceCount = m_UnityAudioManager.FindProperty("m_VirtualVoiceCount");
                m_UnityAudioDefaultSpeakerMode = m_UnityAudioManager.FindProperty("Default Speaker Mode");
                
                #endregion
            }
            public void OnGUI()
            {
                using (new EditorUtils.BoxBlock(Color.black))
                {
                    DrawContrains();
                }

                using (new EditorUtils.BoxBlock(Color.black))
                {
                    DrawTagManager();
                }

                using (new EditorUtils.BoxBlock(Color.black))
                {
                    DrawSettings();
                }

                using (new EditorGUI.DisabledGroupScope(m_DefinedFMOD))
                using (new EditorUtils.BoxBlock(Color.black))
                {
                    DrawUnityAudio();
                }
            }
            public bool Predicate()
            {
                if (!TagManagerPredicate()) return false;
                return true;
            }

            #region Contrains

            private bool m_OpenContrains = false;

            private void DrawContrains()
            {
                m_OpenContrains = EditorUtils.Foldout(m_OpenContrains, "Constrains");
                if (!m_OpenContrains) return;

                EditorGUI.indentLevel++;

                EditorUtils.StringRich("Unity Constrains", 13);
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    m_DefinedCollectionsChecks
                        = EditorGUILayout.ToggleLeft("Define ENABLE_UNITY_COLLECTIONS_CHECKS", m_DefinedCollectionsChecks);

                    if (check.changed)
                    {
                        if (m_DefinedCollectionsChecks) DefineConstrains(UNITY_COLLECTIONS_CHECKS);
                        else UndefineContrains(UNITY_COLLECTIONS_CHECKS);
                    }
                }

                EditorGUILayout.Space();
                EditorUtils.Line();

                EditorUtils.StringRich("CoreSystem Constrains", 13);
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    m_DefinedCoresystemChecks =
                        EditorGUILayout.ToggleLeft("Define CORESYSTEM_DISABLE_CHECKS", m_DefinedCoresystemChecks);

                    if (check.changed)
                    {
                        if (m_DefinedCoresystemChecks) DefineConstrains(CORESYSTEM_DISABLE_CHECKS);
                        else UndefineContrains(CORESYSTEM_DISABLE_CHECKS);
                    }
                }
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    m_DefinedTurnBasedSystem =
                        EditorGUILayout.ToggleLeft("Define CORESYSTEM_TURNBASESYSTEM", m_DefinedTurnBasedSystem);

                    if (check.changed)
                    {
                        if (m_DefinedTurnBasedSystem) DefineConstrains(CORESYSTEM_TURNBASESYSTEM);
                        else UndefineContrains(CORESYSTEM_TURNBASESYSTEM);
                    }
                }

                EditorGUILayout.Space();
                EditorUtils.Line();

                EditorUtils.StringRich("Third Party Constrains", 13);

                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    m_DefinedFMOD =
                        EditorGUILayout.ToggleLeft("Define CORESYSTEM_FMOD", m_DefinedFMOD);

                    if (check.changed)
                    {
                        if (m_DefinedFMOD) DefineConstrains(CORESYSTEM_FMOD);
                        else UndefineContrains(CORESYSTEM_FMOD);
                    }
                }
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    m_DefinedDotween =
                        EditorGUILayout.ToggleLeft("Define CORESYSTEM_DOTWEEN", m_DefinedDotween);

                    if (check.changed)
                    {
                        if (m_DefinedDotween) DefineConstrains(CORESYSTEM_DOTWEEN);
                        else UndefineContrains(CORESYSTEM_DOTWEEN);
                    }
                }
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    m_DefinedMotionMatching =
                        EditorGUILayout.ToggleLeft("Define CORESYSTEM_MOTIONMATCHING", m_DefinedMotionMatching);

                    if (check.changed)
                    {
                        if (m_DefinedMotionMatching) DefineConstrains(CORESYSTEM_MOTIONMATCHING);
                        else UndefineContrains(CORESYSTEM_MOTIONMATCHING);
                    }
                }
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    m_DefinedBehaviorTree =
                        EditorGUILayout.ToggleLeft("Define CORESYSTEM_BEHAVIORTREE", m_DefinedBehaviorTree);

                    if (check.changed)
                    {
                        if (m_DefinedBehaviorTree) DefineConstrains(CORESYSTEM_BEHAVIORTREE);
                        else UndefineContrains(CORESYSTEM_BEHAVIORTREE);
                    }
                }

                EditorGUI.indentLevel--;
            }

            private bool HasConstrains(string name) => m_DefinedConstrains.Contains(name);
            private void DefineConstrains(params string[] names)
            {
                if (names == null || names.Length == 0) return;

                for (int i = 0; i < names.Length; i++)
                {
                    if (m_DefinedConstrains.Contains(names[i])) continue;

                    m_DefinedConstrains.Add(names[i]);
                }

                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, m_DefinedConstrains.ToArray());
                UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
            }
            private void UndefineContrains(params string[] names)
            {
                if (names == null || names.Length == 0) return;

                for (int i = 0; i < names.Length; i++)
                {
                    if (!m_DefinedConstrains.Contains(names[i])) continue;

                    m_DefinedConstrains.Remove(names[i]);
                }

                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, m_DefinedConstrains.ToArray());
                UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
            }

            #endregion

            #region Tag Manager

            private bool m_OpenTagManager = false;

            private bool TagManagerPredicate()
            {
                if (m_MissingTags.Count > 0 || m_MissingLayers.Count > 0) return false;
                return true;
            }
            private void DrawTagManager()
            {
                m_OpenTagManager = EditorUtils.Foldout(m_OpenTagManager, "Tag Manager");
                if (!m_OpenTagManager) return;

                EditorGUI.indentLevel++;

                EditorUtils.StringRich("Tags", 13);
                if (m_MissingTags.Count > 0)
                {
                    EditorGUILayout.HelpBox($"Number({m_MissingTags.Count}) of Tags are missing", MessageType.Error);

                    for (int i = m_MissingTags.Count - 1; i >= 0; i--)
                    {
                        EditorGUILayout.BeginHorizontal();

                        EditorGUILayout.TextField(m_MissingTags[i]);
                        if (GUILayout.Button("Add", GUILayout.Width(100)))
                        {
                            InsertTag(m_MissingTags[i]);
                            m_MissingTags.RemoveAt(i);
                        }

                        EditorGUILayout.EndHorizontal();
                    }
                }
                else EditorGUILayout.HelpBox("Nominal", MessageType.Info);

                EditorUtils.Line();

                EditorUtils.StringRich("Layers", 13);
                if (m_MissingLayers.Count > 0)
                {
                    EditorGUILayout.HelpBox($"Number({m_MissingLayers.Count}) of Layers are missing", MessageType.Error);

                    for (int i = m_MissingLayers.Count - 1; i >= 0; i--)
                    {
                        EditorGUILayout.BeginHorizontal();

                        EditorGUILayout.TextField(m_MissingLayers[i]);
                        if (GUILayout.Button("Add", GUILayout.Width(100)))
                        {
                            if (!InsertLayer(m_MissingLayers[i]))
                            {
                                CoreSystem.Logger.LogError(Channel.Editor,
                                    $"Could not add layer {m_MissingLayers[i]} because layer is full.");
                            }
                            else
                            {
                                m_MissingLayers.RemoveAt(i);
                            }
                        }

                        EditorGUILayout.EndHorizontal();
                    }
                }
                else EditorGUILayout.HelpBox("Nominal", MessageType.Info);

                EditorGUI.indentLevel--;
            }

            private void InsertTag(string tag)
            {
                for (int i = 0; i < m_TagProperty.arraySize; i++)
                {
                    string value = m_TagProperty.GetArrayElementAtIndex(i).stringValue;
                    if (string.IsNullOrEmpty(value))
                    {
                        m_TagProperty.GetArrayElementAtIndex(i).stringValue = tag;
                        m_TagManagerObject.ApplyModifiedProperties();
                        return;
                    }
                }

                m_TagProperty.InsertArrayElementAtIndex(m_TagProperty.arraySize);
                m_TagProperty.GetArrayElementAtIndex(m_TagProperty.arraySize - 1).stringValue = tag;
                m_TagManagerObject.ApplyModifiedProperties();
            }
            private bool InsertLayer(string layer)
            {
                for (int i = 0; i < m_LayerProperty.arraySize; i++)
                {
                    string value = m_LayerProperty.GetArrayElementAtIndex(i).stringValue;
                    if (string.IsNullOrEmpty(value))
                    {
                        m_LayerProperty.GetArrayElementAtIndex(i).stringValue = layer;
                        m_TagManagerObject.ApplyModifiedProperties();
                        return true;
                    }
                }

                return false;
            }

            #endregion

            #region CoreSystem Settings

            private bool m_OpenCoreSystemSettings = false;

            private void DrawSettings()
            {
                m_OpenCoreSystemSettings = EditorUtils.Foldout(m_OpenCoreSystemSettings, "Settings");
                if (!m_OpenCoreSystemSettings) return;

                EditorGUI.indentLevel++;

                using (new EditorUtils.BoxBlock(Color.white))
                {
                    EditorUtils.StringRich("Global Settings", 13);
                    EditorGUILayout.Space();

                    m_CoreSystemSettings.m_DisplayLogChannel =
                        (Channel)EditorGUILayout.EnumFlagsField("Display Log Channel", m_CoreSystemSettings.m_DisplayLogChannel);

                    m_CoreSystemSettings.m_VisualizeObjects =
                        EditorGUILayout.ToggleLeft("Visuallize All Managers", m_CoreSystemSettings.m_VisualizeObjects);

                    m_CoreSystemSettings.m_CrashAfterException =
                        EditorGUILayout.ToggleLeft("Crash After Exception", m_CoreSystemSettings.m_CrashAfterException);

                    m_CoreSystemSettings.m_HideSetupWizard =
                        EditorGUILayout.ToggleLeft("Hide Setup Wizard", m_CoreSystemSettings.m_HideSetupWizard);

                    m_CoreSystemSettings.m_EnableLua =
                        EditorGUILayout.ToggleLeft("Enable Lua", m_CoreSystemSettings.m_EnableLua);
                }
                EditorUtils.Line();

                EditorGUI.indentLevel--;
            }

            #endregion

            #region Unity Audio

            private bool 
                m_OpenUnityAudio = false, m_IsUnityAudioModified = false;

            private void DrawUnityAudio()
            {
                m_OpenUnityAudio = EditorUtils.Foldout(m_OpenUnityAudio, "Unity Audio");
                if (!m_OpenUnityAudio) return;

                EditorGUI.indentLevel++;
                using (new EditorUtils.BoxBlock(Color.white))
                {
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        EditorGUILayout.PropertyField(m_UnityAudioDisableAudio);

                        if (check.changed)
                        {
                            m_UnityAudioManager.ApplyModifiedProperties();
                            m_UnityAudioManager.Update();
                        }
                    }

                    EditorUtils.Line();

                    if (m_UnityAudioDisableAudio.boolValue)
                    {
                        EditorGUILayout.HelpBox("Unity Audio has been disabled", MessageType.Info);
                        return;
                    }

                    using (new EditorGUI.DisabledGroupScope(!m_IsUnityAudioModified))
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (m_IsUnityAudioModified)
                        {
                            EditorGUILayout.LabelField("Modified");
                        }

                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Apply", GUILayout.Width(100)))
                        {
                            m_UnityAudioManager.ApplyModifiedProperties();
                            m_UnityAudioManager.Update();

                            m_IsUnityAudioModified = false;
                        }
                    }

                    EditorGUI.BeginChangeCheck();

                    m_UnityAudioGlobalVolume.floatValue
                        = EditorGUILayout.Slider("Global Volume", m_UnityAudioGlobalVolume.floatValue, 0, 1);
                    m_UnityAudioRolloffScale.floatValue
                        = EditorGUILayout.Slider("Volume Rolloff Scale", m_UnityAudioRolloffScale.floatValue, 0, 1);
                    m_UnityAudioDopplerFactor.floatValue
                        = EditorGUILayout.Slider("Doppler Factor", m_UnityAudioDopplerFactor.floatValue, 0, 1);

                    EditorUtils.Line();

                    m_UnityAudioRealVoiceCount.intValue
                        = EditorGUILayout.IntField("Max Real Voices", m_UnityAudioRealVoiceCount.intValue);

                    m_UnityAudioVirtualVoiceCount.intValue
                        = EditorGUILayout.IntField("Max Virtual Voices", m_UnityAudioVirtualVoiceCount.intValue);

                    m_UnityAudioDefaultSpeakerMode.intValue =
                        (int)(AudioSpeakerMode)EditorGUILayout.EnumPopup("Default Speaker Mode", (AudioSpeakerMode)m_UnityAudioDefaultSpeakerMode.intValue);

                    if (EditorGUI.EndChangeCheck())
                    {
                        m_IsUnityAudioModified = true;
                    }
                }
                EditorGUI.indentLevel--;
            }

            #endregion
        }
        #endregion

        #region Scene Menu
        private sealed class SceneMenu
        {
            private SerializedObject serializedObject;

            private SerializedProperty 
                masterScene,
                startScene,
                loadingScene,
                sceneList;

            bool 
                m_OpenMasterScene = false,
                m_OpenStartScene = false,
                m_OpenCustomLoadingScene = false,
                m_OpenSceneList = false;

            Vector2
                m_Scroll = Vector2.zero;

            public SceneMenu()
            {
                serializedObject = new SerializedObject(SceneList.Instance);

                masterScene = serializedObject.FindProperty(nameof(SceneList.Instance.MasterScene));
                startScene = serializedObject.FindProperty(nameof(SceneList.Instance.StartScene));
                loadingScene = serializedObject.FindProperty(nameof(SceneList.Instance.CustomLoadingScene));
                sceneList = serializedObject.FindProperty(nameof(SceneList.Instance.Scenes));

                m_OpenMasterScene = 
                    string.IsNullOrEmpty(SceneList.Instance.MasterScene.ScenePath) ||
                    !SceneList.Instance.MasterScene.IsInBuild;
                m_OpenStartScene = 
                    string.IsNullOrEmpty(SceneList.Instance.StartScene.ScenePath) ||
                    !SceneList.Instance.StartScene.IsInBuild;

                m_OpenSceneList = 
                    sceneList.arraySize == 0 ||
                    !SceneList.Instance.Scenes[0].IsInBuild;
            }

            public void OnGUI()
            {
                m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);

                using (new EditorUtils.BoxBlock(Color.black))
                {
                    m_OpenMasterScene = EditorUtils.Foldout(m_OpenMasterScene, "Master Scene", 13);
                    bool sceneFound = !string.IsNullOrEmpty(SceneList.Instance.MasterScene.ScenePath);
                    
                    if (m_OpenMasterScene)
                    {
                        if (!sceneFound)
                        {
                            EditorGUILayout.HelpBox("!! Master Scene Not Found !!", MessageType.Error);
                        }
                        else
                        {
                            if (!SceneList.Instance.MasterScene.IsInBuild)
                            {
                                EditorGUILayout.HelpBox("Master scene must included in the build", MessageType.Error);
                            }
                            else EditorGUILayout.HelpBox("Master Scene Found", MessageType.Info);
                        }
                        EditorGUILayout.PropertyField(masterScene);

                        if (!sceneFound)
                        {
                            if (GUILayout.Button("Initialize Master Scene"))
                            {
                                Scene scene = CreateNewScene("Master");
                                SceneList.Instance.MasterScene = new SceneReference
                                {
                                    ScenePath = scene.path
                                };
                                EditorUtils.SetDirty(SceneList.Instance);
                                CloseScene(scene);

                                serializedObject.Update();
                            }
                        }
                    }
                }
                EditorUtils.Line();
                using (new EditorUtils.BoxBlock(Color.black))
                {
                    m_OpenStartScene = EditorUtils.Foldout(m_OpenStartScene, "Start Scene", 13);
                    bool sceneFound = !string.IsNullOrEmpty(SceneList.Instance.StartScene.ScenePath);
                    
                    if (m_OpenStartScene)
                    {
                        if (!sceneFound)
                        {
                            EditorGUILayout.HelpBox("!! Start Scene Not Found !!", MessageType.Error);
                        }
                        else
                        {
                            if (!SceneList.Instance.StartScene.IsInBuild)
                            {
                                EditorGUILayout.HelpBox("Start scene must included in the build", MessageType.Error);
                            }
                            else EditorGUILayout.HelpBox("Start Scene Found", MessageType.Info);
                        }
                        EditorGUILayout.PropertyField(startScene);

                        if (!sceneFound)
                        {
                            if (GUILayout.Button("Initialize Start Scene"))
                            {
                                Scene scene = CreateNewScene("Start");
                                SceneList.Instance.StartScene = new SceneReference
                                {
                                    ScenePath = scene.path
                                };
                                EditorUtils.SetDirty(SceneList.Instance);
                                CloseScene(scene);

                                serializedObject.Update();
                            }
                        }
                    }
                }
                EditorUtils.Line();
                using (new EditorUtils.BoxBlock(Color.black))
                {
                    m_OpenCustomLoadingScene = EditorUtils.Foldout(m_OpenCustomLoadingScene, "Loading Scene", 13);
                    bool sceneFound = !string.IsNullOrEmpty(SceneList.Instance.CustomLoadingScene.ScenePath);

                    if (m_OpenCustomLoadingScene)
                    {
                        if (!sceneFound)
                        {
                            EditorGUILayout.HelpBox(
                                "Loading Scene Not Found.\nWill be replaced to default loading screen.", MessageType.Info);
                        }
                        else EditorGUILayout.HelpBox("Loading Scene Found", MessageType.Info);
                        EditorGUILayout.PropertyField(loadingScene);

                        if (!sceneFound)
                        {
                            if (GUILayout.Button("Initialize Loading Scene"))
                            {
                                Scene scene = CreateNewScene("Loading");
                                SceneList.Instance.CustomLoadingScene = new SceneReference
                                {
                                    ScenePath = scene.path
                                };
                                EditorUtils.SetDirty(SceneList.Instance);
                                EditorSceneManager.SetActiveScene(scene);

                                GameObject cameraObj = new GameObject("Loading Camera");
                                Camera cam = cameraObj.AddComponent<Camera>();

                                GameObject canvasObj = new GameObject("Loading Canvas");
                                Canvas canvas = canvasObj.AddComponent<Canvas>();
                                CanvasScaler canvasScaler = canvasObj.AddComponent<CanvasScaler>();
                                canvasObj.AddComponent<GraphicRaycaster>();
                                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

                                GameObject BlackScreenObj = new GameObject("BlackScreen");
                                BlackScreenObj.transform.SetParent(canvasObj.transform);
                                CanvasGroup canvasGroup = BlackScreenObj.AddComponent<CanvasGroup>();
                                Image blackScreenImg = BlackScreenObj.AddComponent<Image>();
                                blackScreenImg.color = Color.black;
                                blackScreenImg.rectTransform.sizeDelta = new Vector2(800, 600);
                                blackScreenImg.rectTransform.anchoredPosition = Vector2.zero;

                                GameObject loadingObj = new GameObject("Loading Script");
                                CustomLoadingScene loadingScr = loadingObj.AddComponent<CustomLoadingScene>();

                                TypeHelper.TypeOf<CustomLoadingScene>.Type.GetField("m_Camera", BindingFlags.Instance | BindingFlags.NonPublic)
                                    .SetValue(loadingScr, cam);
                                TypeHelper.TypeOf<CustomLoadingScene>.Type.GetField("m_FadeGroup", BindingFlags.Instance | BindingFlags.NonPublic)
                                    .SetValue(loadingScr, canvasGroup);

                                EditorUtility.SetDirty(loadingScr);

                                EditorSceneManager.SaveScene(scene);

                                CloseScene(scene);

                                serializedObject.Update();
                            }
                        }
                    }
                }
                EditorUtils.Line();
                using (new EditorUtils.BoxBlock(Color.black))
                {
                    m_OpenSceneList = EditorUtils.Foldout(m_OpenSceneList, "Scenes", 13);

                    if (m_OpenSceneList)
                    {
                        if (sceneList.arraySize == 0)
                        {
                            EditorGUILayout.HelpBox(
                                "There\'s no scenes in the list.\n" +
                                "We need at least one scene for initialize systems", MessageType.Error);
                        }
                        else
                        {
                            if (!SceneList.Instance.Scenes[0].IsInBuild)
                            {
                                EditorGUILayout.HelpBox("Scene index 0 must included in the build", MessageType.Error);
                            }
                            else EditorGUILayout.HelpBox("Normal", MessageType.Info);
                        }

                        EditorGUILayout.PropertyField(sceneList);
                    }
                }

                EditorGUILayout.EndScrollView();
                serializedObject.ApplyModifiedProperties();
            }
            public bool Predicate()
            {
                if (string.IsNullOrEmpty(SceneList.Instance.MasterScene.ScenePath) ||
                    string.IsNullOrEmpty(SceneList.Instance.StartScene.ScenePath) ||
                    sceneList.arraySize == 0) return false;

                if (!SceneList.Instance.MasterScene.IsInBuild ||
                    !SceneList.Instance.StartScene.IsInBuild ||
                    !SceneList.Instance.Scenes[0].IsInBuild) return false;

                return true;
            }

            private Scene CreateNewScene(string name)
            {
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                scene.name = name;
                EditorSceneManager.SaveScene(scene, $"Assets/Scenes/{name}.unity");
                return scene;
            }
            private void CloseScene(Scene scene) => EditorSceneManager.CloseScene(scene, true);
        }

        #endregion

        #region Prefab Menu

        private sealed class PrefabMenu
        {
            SerializedObject serializedObject;
            SerializedProperty
                m_ObjectSettings;

            FieldInfo objectSettingsFieldInfo;
            List<PrefabList.ObjectSetting> objectSettings;

            int m_AddressableCount = 0;
            readonly List<int> m_InvalidIndices = new List<int>();

            Vector2
                m_Scroll = Vector2.zero;

            public PrefabMenu()
            {
                serializedObject = new SerializedObject(PrefabList.Instance);
                m_ObjectSettings = serializedObject.FindProperty("m_ObjectSettings");

                objectSettingsFieldInfo = TypeHelper.TypeOf<PrefabList>.Type.GetField("m_ObjectSettings",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var temp = objectSettingsFieldInfo.GetValue(PrefabList.Instance);
                
                if (temp == null)
                {
                    objectSettings = new List<PrefabList.ObjectSetting>();
                    objectSettingsFieldInfo.SetValue(PrefabList.Instance, objectSettings);

                    serializedObject.Update();
                }
                else objectSettings = (List<PrefabList.ObjectSetting>)temp;

                HashSet<UnityEngine.Object> tempSet = new HashSet<UnityEngine.Object>();
                for (int i = 0; i < objectSettings.Count; i++)
                {
                    UnityEngine.Object obj = objectSettings[i].GetEditorAsset();
                    if (obj == null)
                    {
                        m_InvalidIndices.Add(i);
                    }
                    if (tempSet.Contains(obj))
                    {
                        objectSettings[i] = new PrefabList.ObjectSetting(objectSettings[i].m_Name, null, objectSettings[i].m_IsWorldUI);
                        m_InvalidIndices.Add(i);
                    }

                    tempSet.Add(obj);
                }

                m_AddressableCount = PrefabListEditor.DefaultGroup.entries.Count;
                var groups = PrefabListEditor.PrefabListGroups;
                for (int i = 0; i < groups.Length; i++)
                {
                    m_AddressableCount += groups[i].entries.Count;
                }
            }

            public bool Predicate()
            {
                if (objectSettings.Count - m_InvalidIndices.Count != m_AddressableCount) return false;
                return true;
            }
            public void OnGUI()
            {
                if (GUILayout.Button("Rebase"))
                {
                    PrefabListEditor.Rebase();
                    serializedObject.Update();

                    m_InvalidIndices.Clear();
                    for (int i = 0; i < objectSettings.Count; i++)
                    {
                        if (objectSettings[i].GetEditorAsset() == null)
                        {
                            m_InvalidIndices.Add(i);
                        }
                    }
                }

                m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);

                using (new EditorUtils.BoxBlock(Color.black))
                {
                    if (!Predicate())
                    {
                        EditorUtils.StringRich($"Require Rebase {objectSettings.Count} - {m_InvalidIndices.Count} != {m_AddressableCount}", true);
                    }
                    else
                    {
                        EditorUtils.StringRich("Asset matched with Addressable", true);
                    }
                }

                using (new EditorUtils.BoxBlock(Color.black))
                {
                    if (m_InvalidIndices.Count > 0)
                    {
                        EditorGUILayout.HelpBox("We\'ve found invalid assets in PrefabList but normally " +
                        "it is not an issue. You can ignore this", MessageType.Info);
                        EditorUtils.StringRich("Invalid prefab found");
                        EditorGUI.indentLevel++;

                        EditorGUI.BeginDisabledGroup(true);
                        for (int i = 0; i < m_InvalidIndices.Count; i++)
                        {
                            EditorGUILayout.PropertyField(
                                m_ObjectSettings.GetArrayElementAtIndex(m_InvalidIndices[i]), 
                                new GUIContent($"Index at {m_InvalidIndices[i]}"));
                        }
                        EditorGUI.EndDisabledGroup();

                        EditorGUI.indentLevel--;
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("All prefabs nominal", MessageType.Info);
                    }
                }
                

                //
                EditorGUILayout.EndScrollView();
            }
            //
        }

        #endregion
    }
}
