#if CORESYSTEM_FMOD
using Syadeu.FMOD;
#elif CORESYSTEM_UNITYAUDIO
#endif

using Syadeu.Collections;
using Syadeu.Collections.Editor;
using Syadeu.Mono;
using Syadeu.Presentation;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SyadeuEditor
{
    internal sealed class SceneMenu : SetupWizardMenuItem
    {
        public override string Name => "Scene";
        public override int Order => -9998;

        private SerializedObject serializedObject;

        private SerializedProperty
            masterScene,
            startScene,
            loadingScene,
            sceneList,
            cameraPrefab;

        //private FieldInfo m_CameraPrefabFieldInfo;

        //PrefabReferenceDrawer m_CameraPrefabDrawer;

        bool
            m_OpenMasterScene = false,
            m_OpenStartScene = false,
            m_OpenCustomLoadingScene = false,
            m_OpenSceneList = false;

        Vector2
            m_Scroll = Vector2.zero;

        public SceneMenu()
        {
            serializedObject = new SerializedObject(SceneSettings.Instance);

            masterScene = serializedObject.FindProperty(nameof(SceneSettings.Instance.MasterScene));
            startScene = serializedObject.FindProperty(nameof(SceneSettings.Instance.StartScene));
            loadingScene = serializedObject.FindProperty(nameof(SceneSettings.Instance.CustomLoadingScene));
            sceneList = serializedObject.FindProperty(nameof(SceneSettings.Instance.Scenes));
            cameraPrefab = serializedObject.FindProperty("m_CameraPrefab");

            //m_CameraPrefabField = serializedObject.FindProperty("m_CameraPrefab");
            //m_CameraPrefabFieldInfo = TypeHelper.TypeOf<SceneSettings>.GetFieldInfo("m_CameraPrefab");
            //m_CameraPrefabDrawer = (PrefabReferenceDrawer)ObjectDrawerBase.ToDrawer(SceneSettings.Instance,
            //    m_CameraPrefabFieldInfo, false);

            m_OpenMasterScene =
                string.IsNullOrEmpty(SceneSettings.Instance.MasterScene.ScenePath) ||
                !SceneSettings.Instance.MasterScene.IsInBuild;
            m_OpenStartScene =
                string.IsNullOrEmpty(SceneSettings.Instance.StartScene.ScenePath) ||
                !SceneSettings.Instance.StartScene.IsInBuild;

            m_OpenSceneList =
                sceneList.arraySize == 0 ||
                !SceneSettings.Instance.Scenes[0].IsInBuild;
        }

        public override void OnGUI()
        {
            using (var scroll = new EditorGUILayout.ScrollViewScope(m_Scroll))
            {
                m_Scroll = scroll.scrollPosition;

                #region Scenes Selector

                using (new CoreGUI.BoxBlock(Color.black))
                {
                    m_OpenMasterScene = CoreGUI.Foldout(m_OpenMasterScene, "Master Scene", 13);
                    bool sceneFound = !string.IsNullOrEmpty(SceneSettings.Instance.MasterScene.ScenePath);

                    if (m_OpenMasterScene)
                    {
                        if (!sceneFound)
                        {
                            EditorGUILayout.HelpBox("!! Master Scene Not Found !!", MessageType.Error);
                        }
                        else
                        {
                            if (!SceneSettings.Instance.MasterScene.IsInBuild)
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
                                SceneSettings.Instance.MasterScene = new SceneReference
                                {
                                    ScenePath = scene.path
                                };
                                EditorUtilities.SetDirty(SceneSettings.Instance);
                                CloseScene(scene);

                                serializedObject.Update();
                            }
                        }
                    }
                }
                CoreGUI.Line();
                using (new CoreGUI.BoxBlock(Color.black))
                {
                    m_OpenStartScene = CoreGUI.Foldout(m_OpenStartScene, "Start Scene", 13);
                    bool sceneFound = !string.IsNullOrEmpty(SceneSettings.Instance.StartScene.ScenePath);

                    if (m_OpenStartScene)
                    {
                        if (!sceneFound)
                        {
                            EditorGUILayout.HelpBox("!! Start Scene Not Found !!", MessageType.Error);
                        }
                        else
                        {
                            if (!SceneSettings.Instance.StartScene.IsInBuild)
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
                                SceneSettings.Instance.StartScene = new SceneReference
                                {
                                    ScenePath = scene.path
                                };
                                EditorUtilities.SetDirty(SceneSettings.Instance);
                                CloseScene(scene);

                                serializedObject.Update();
                            }
                        }
                    }
                }
                CoreGUI.Line();
                using (new CoreGUI.BoxBlock(Color.black))
                {
                    m_OpenCustomLoadingScene = CoreGUI.Foldout(m_OpenCustomLoadingScene, "Loading Scene", 13);
                    bool sceneFound = !string.IsNullOrEmpty(SceneSettings.Instance.CustomLoadingScene.ScenePath);

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
                                SceneSettings.Instance.CustomLoadingScene = new SceneReference
                                {
                                    ScenePath = scene.path
                                };
                                EditorUtilities.SetDirty(SceneSettings.Instance);
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
                CoreGUI.Line();
                using (new CoreGUI.BoxBlock(Color.black))
                {
                    m_OpenSceneList = CoreGUI.Foldout(m_OpenSceneList, "Scenes", 13);

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
                            if (!SceneSettings.Instance.Scenes[0].IsInBuild)
                            {
                                EditorGUILayout.HelpBox("Scene index 0 must included in the build", MessageType.Error);
                            }
                            else EditorGUILayout.HelpBox("Normal", MessageType.Info);
                        }

                        EditorGUILayout.PropertyField(sceneList);
                    }
                }

                #endregion

                using (var change = new EditorGUI.ChangeCheckScope())
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Camera Prefab");
                    //m_CameraPrefabDrawer.OnGUI();
                    EditorGUILayout.PropertyField(cameraPrefab);

                    if (change.changed)
                    {
                        //m_CameraPrefabFieldInfo.SetValue(SceneSettings.Instance,
                        //    )

                        EditorUtility.SetDirty(SceneSettings.Instance);
                        //AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(SceneSettings.Instance));
                        //"asd".ToLog();
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
        public override bool Predicate()
        {
            if (string.IsNullOrEmpty(SceneSettings.Instance.MasterScene.ScenePath) ||
                string.IsNullOrEmpty(SceneSettings.Instance.StartScene.ScenePath) ||
                sceneList.arraySize == 0) return false;

            if (!SceneSettings.Instance.MasterScene.IsInBuild ||
                !SceneSettings.Instance.StartScene.IsInBuild ||
                !SceneSettings.Instance.Scenes[0].IsInBuild) return false;

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
}
