#if CORESYSTEM_FMOD
using Syadeu.FMOD;
#elif CORESYSTEM_UNITYAUDIO
#endif

using Syadeu;
using Syadeu.Internal;
using Syadeu.Mono;
using Syadeu.Presentation;
using SyadeuEditor.Presentation;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SyadeuEditor
{
    public sealed class CoreSystemSetupWizard : EditorWindow
    {
        static CoreSystemSetupWizard()
        {
            EditorApplication.delayCall += Startup;
        }
        static void Startup()
        {
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

            m_SceneMenu = new SceneMenu();
            m_PrefabMenu = new PrefabMenu();
            AddSetup(ToolbarNames.Scene, m_SceneMenu.Predicate);
            AddSetup(ToolbarNames.Prefab, m_PrefabMenu.Predicate);
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

                for (int i = 0; i < objectSettings.Count; i++)
                {
                    if (objectSettings[i].GetEditorAsset() == null)
                    {
                        m_InvalidIndices.Add(i);
                    }
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
                    if (objectSettings.Count - m_InvalidIndices.Count != m_AddressableCount)
                    {
                        EditorUtils.StringRich("Require Rebase", true);
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
