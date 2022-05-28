#if CORESYSTEM_FMOD
using Syadeu.FMOD;
#elif CORESYSTEM_UNITYAUDIO
#endif

using Syadeu;
using Syadeu.Collections.Editor;
using Syadeu.Mono;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor
{
    internal sealed class GeneralMenu : SetupWizardMenuItem
    {
        public override string Name => "General";
        public override int Order => -9999;

        private Vector2 m_Scroll = Vector2.zero;

        #region Constraints

        const string
            UNITY_COLLECTIONS_CHECKS = "ENABLE_UNITY_COLLECTIONS_CHECKS",
            CORESYSTEM_DISABLE_CHECKS = "CORESYSTEM_DISABLE_CHECKS",

            CORESYSTEM_TURNBASESYSTEM = "CORESYSTEM_TURNBASESYSTEM",

            CORESYSTEM_DOTWEEN = "CORESYSTEM_DOTWEEN",
            CORESYSTEM_MOTIONMATCHING = "CORESYSTEM_MOTIONMATCHING",
            CORESYSTEM_BEHAVIORTREE = "CORESYSTEM_BEHAVIORTREE",
            CORESYSTEM_SHAPES = "CORESYSTEM_SHAPES",
            CORESYSTEM_FMOD = "CORESYSTEM_FMOD";
        bool
            m_DefinedCollectionsChecks,
            m_DefinedCoresystemChecks,

            m_DefinedTurnBasedSystem,

            m_DefinedDotween,
            m_DefinedMotionMatching,
            m_DefinedBehaviorTree,
            m_DefinedShapes,
            m_DefinedFMOD;
        List<string> m_DefinedConstraints;

        #endregion

        #region Tag Manager
        SerializedObject m_TagManagerObject;
        SerializedProperty m_TagProperty, m_LayerProperty;

        static string[] c_RequireTags = new string[] { };
        static string[] c_RequireLayers = new string[] { "Terrain", "FloorProjection", "Entity" };

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
            m_DefinedConstraints = temp.ToList();

            m_DefinedCollectionsChecks = HasConstrains(UNITY_COLLECTIONS_CHECKS);
            m_DefinedCoresystemChecks = HasConstrains(CORESYSTEM_DISABLE_CHECKS);

            m_DefinedTurnBasedSystem = HasConstrains(CORESYSTEM_TURNBASESYSTEM);

            m_DefinedDotween = HasConstrains(CORESYSTEM_DOTWEEN);
            m_DefinedMotionMatching = HasConstrains(CORESYSTEM_MOTIONMATCHING);
            m_DefinedBehaviorTree = HasConstrains(CORESYSTEM_BEHAVIORTREE);
            m_DefinedShapes = HasConstrains(CORESYSTEM_SHAPES);
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
        public override void OnGUI()
        {
            using (var scroll = new EditorGUILayout.ScrollViewScope(m_Scroll))
            {
                using (new CoreGUI.BoxBlock(Color.black))
                {
                    DrawContraints();
                }

                using (new CoreGUI.BoxBlock(Color.black))
                {
                    DrawTagManager();
                }

                using (new CoreGUI.BoxBlock(Color.black))
                {
                    DrawSettings();
                }

                using (new EditorGUI.DisabledGroupScope(m_DefinedFMOD))
                using (new CoreGUI.BoxBlock(Color.black))
                {
                    DrawUnityAudio();
                }

                m_Scroll = scroll.scrollPosition;
            }
        }
        public override bool Predicate()
        {
            if (!TagManagerPredicate()) return false;
            return true;
        }

        #region Contrains

        private bool m_OpenContraints = false;

        private void DrawContraints()
        {
            m_OpenContraints = CoreGUI.Foldout(m_OpenContraints, "Constraints");
            if (!m_OpenContraints) return;

            EditorGUI.indentLevel++;

            CoreGUI.Label("Unity Constraints", 13);

            DrawConstraint(ref m_DefinedCollectionsChecks, UNITY_COLLECTIONS_CHECKS);

            EditorGUILayout.Space();
            CoreGUI.Line();

            CoreGUI.Label("CoreSystem Constraints", 13);

            DrawConstraint(ref m_DefinedCoresystemChecks, CORESYSTEM_DISABLE_CHECKS);
            DrawConstraint(ref m_DefinedTurnBasedSystem, CORESYSTEM_TURNBASESYSTEM);

            EditorGUILayout.Space();
            CoreGUI.Line();

            CoreGUI.Label("Third Party Constraints", 13);

            DrawConstraint(ref m_DefinedFMOD, CORESYSTEM_FMOD);
            DrawConstraint(ref m_DefinedDotween, CORESYSTEM_DOTWEEN);
            DrawConstraint(ref m_DefinedMotionMatching, CORESYSTEM_MOTIONMATCHING);

            bool enableShapes;
#if SHAPES_HDRP
                enableShapes = true;
#else
            enableShapes = false;
#endif
            using (new EditorGUI.DisabledGroupScope(enableShapes))
            {
                DrawConstraint(ref m_DefinedShapes, CORESYSTEM_SHAPES);
            }

            DrawConstraint(ref m_DefinedBehaviorTree, CORESYSTEM_BEHAVIORTREE);

            EditorGUI.indentLevel--;
        }

        private void DrawConstraint(ref bool defined, in string constString)
        {
            const string c_Label = "Define {0}";
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                defined =
                    EditorGUILayout.ToggleLeft(string.Format(c_Label, constString), defined);

                if (check.changed)
                {
                    if (defined) DefineConstraints(constString);
                    else UndefineContraints(constString);
                }
            }
        }
        private bool HasConstrains(string name) => m_DefinedConstraints.Contains(name);
        private void DefineConstraints(params string[] names)
        {
            if (names == null || names.Length == 0) return;

            for (int i = 0; i < names.Length; i++)
            {
                if (m_DefinedConstraints.Contains(names[i])) continue;

                m_DefinedConstraints.Add(names[i]);
            }

            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, m_DefinedConstraints.ToArray());
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
        }
        private void UndefineContraints(params string[] names)
        {
            if (names == null || names.Length == 0) return;

            for (int i = 0; i < names.Length; i++)
            {
                if (!m_DefinedConstraints.Contains(names[i])) continue;

                m_DefinedConstraints.Remove(names[i]);
            }

            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, m_DefinedConstraints.ToArray());
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
            m_OpenTagManager = CoreGUI.Foldout(m_OpenTagManager, "Tag Manager");
            if (!m_OpenTagManager) return;

            EditorGUI.indentLevel++;

            CoreGUI.Label("Tags", 13);
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

            CoreGUI.Line();

            CoreGUI.Label("Layers", 13);
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
                            CoreSystem.Logger.LogError(LogChannel.Editor,
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
            m_OpenCoreSystemSettings = CoreGUI.Foldout(m_OpenCoreSystemSettings, "Settings");
            if (!m_OpenCoreSystemSettings) return;

            EditorGUI.indentLevel++;

            using (new CoreGUI.BoxBlock(Color.white))
            {
                CoreGUI.Label("Global Settings", 13);
                EditorGUILayout.Space();

                m_CoreSystemSettings.m_DisplayLogChannel =
                    (LogChannel)EditorGUILayout.EnumFlagsField("Display Log Channel", m_CoreSystemSettings.m_DisplayLogChannel);

                m_CoreSystemSettings.m_VisualizeObjects =
                    EditorGUILayout.ToggleLeft("Visuallize All Managers", m_CoreSystemSettings.m_VisualizeObjects);

                m_CoreSystemSettings.m_CrashAfterException =
                    EditorGUILayout.ToggleLeft("Crash After Exception", m_CoreSystemSettings.m_CrashAfterException);

                m_CoreSystemSettings.m_HideSetupWizard =
                    EditorGUILayout.ToggleLeft("Hide Setup Wizard", m_CoreSystemSettings.m_HideSetupWizard);

                m_CoreSystemSettings.m_EnableLua =
                    EditorGUILayout.ToggleLeft("Enable Lua", m_CoreSystemSettings.m_EnableLua);
            }
            CoreGUI.Line();

            EditorGUI.indentLevel--;
        }

        #endregion

        #region Unity Audio

        private bool
            m_OpenUnityAudio = false, m_IsUnityAudioModified = false;

        private void DrawUnityAudio()
        {
            m_OpenUnityAudio = CoreGUI.Foldout(m_OpenUnityAudio, "Unity Audio");
            if (!m_OpenUnityAudio) return;

            EditorGUI.indentLevel++;
            using (new CoreGUI.BoxBlock(Color.white))
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

                CoreGUI.Line();

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

                CoreGUI.Line();

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
}
