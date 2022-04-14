using Syadeu;
using Syadeu.Collections;
using Syadeu.Internal;
using Syadeu.Mono;
using Syadeu.Presentation;
using Syadeu.Presentation.Map;
using SyadeuEditor.Tree;
using SyadeuEditor.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Presentation.Map
{
    public sealed class MapSystemWindow : EditorWindowEntity<MapSystemWindow>
    {
        const string c_EditorOnly = "EditorOnly";

        protected override string DisplayName => "Map System";

        private MapDataLoader m_MapDataLoader;

        protected override void OnEnable()
        {
            m_MapDataLoader = new MapDataLoader();
            EditorApplication.playModeStateChanged -= EditorApplication_playModeStateChanged;
            EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;

            base.OnEnable();
        }
        private void EditorApplication_playModeStateChanged(PlayModeStateChange obj)
        {
            if (obj == PlayModeStateChange.ExitingEditMode)
            {
                if (m_MapDataLoader != null)
                {
                    m_MapDataLoader.Dispose();
                    m_MapDataLoader = null;
                }
                m_MapDataLoader = new MapDataLoader();
            }
        }

        protected override void OnDisable()
        {
            Tools.hidden = false;

            if (m_MapDataLoader != null)
            {
                m_MapDataLoader.Dispose();
                m_MapDataLoader = null;
            }

            base.OnDisable();
        }
        private void OnDestroy()
        {
            if (m_MapDataLoader != null)
            {
                m_MapDataLoader.Dispose();
                m_MapDataLoader = null;
            }
        }
        private void OnValidate()
        {
            if (m_MapDataLoader != null)
            {
                m_MapDataLoader.Dispose();
                m_MapDataLoader = null;
            }
        }

        private void OnGUI()
        {
            using (new EditorUtilities.BoxBlock(Color.black))
            {
                EditorUtilities.StringHeader("Map System", 20, true);
            }
            GUILayout.Space(4);
            CoreGUI.Line();

            //EditorGUI.BeginChangeCheck();
            //m_EnableEdit = EditorGUILayout.ToggleLeft("Enable Edit", m_EnableEdit);
            //if (EditorGUI.EndChangeCheck())
            //{
            //    if (!m_EnableEdit) Tools.hidden = false;
            //}
            //if (GUILayout.Button("Show Tools")) Tools.hidden = false;
            //EditorGUILayout.Space();

            //MapDataGUI();
            #region Scene data selector
            using (new EditorUtilities.BoxBlock(Color.gray))
            {
                using (new EditorGUI.DisabledGroupScope(m_SceneDataTarget == null))
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Save"))
                    {
                        EntityDataList.Instance.SaveData(m_SceneDataTarget);
                    }
                    if (GUILayout.Button("Close"))
                    {
                        ResetSceneData();
                    }
                }
                ReferenceDrawer.DrawReferenceSelector("Scene data: ", (hash) =>
                {
                    if (hash.Equals(Hash.Empty))
                    {
                        ResetSceneData();
                        return;
                    }

                    m_SceneData = new Reference<SceneDataEntity>(hash);

                    if (m_SceneData.IsValid())
                    {
                        m_SceneDataTarget = m_SceneData.GetObject();

                        //m_GridMap = new GridMapExtension(m_SceneDataTarget.GetAttribute<GridMapAttribute>());
                        SceneView.lastActiveSceneView.Repaint();
                    }
                }, m_SceneData, TypeHelper.TypeOf<SceneDataEntity>.Type);
            }
            #endregion

            CoreGUI.Line();

            m_MapDataLoader.OnGUI();

            if (Event.current.isKey)
            {
                if (Event.current.control && Event.current.keyCode == KeyCode.S)
                {
                    EntityDataList.Instance.SaveData();

                    CoreSystem.Logger.Log(Channel.Editor,
                        $"Map data Saved");
                }
            }
        }
        private void OnSelectionChange()
        {
            if (Selection.activeGameObject != null)
            {
                m_MapDataLoader.SelectObjects(Selection.gameObjects);
            }
            else if (m_MapDataLoader.SelectedGameObjects != null)
            {
                m_MapDataLoader.SelectObjects(null);
            }
        }
        protected override void OnSceneGUI(SceneView obj)
        {
            m_MapDataLoader.OnSceneGUI();
        }

        #region Common
        private Transform m_PreviewFolder;
        const string c_EditInPlayingWarning = "Cannot edit data while playing";
        private void ResetSceneData()
        {
            m_SceneData = new Reference<SceneDataEntity>(Hash.Empty);
            m_SceneDataTarget = null;

            SceneView.lastActiveSceneView.Repaint();
        }

        private void ResetPreviewFolder()
        {
            if (m_PreviewFolder != null) DestroyImmediate(m_PreviewFolder.gameObject);
            m_PreviewFolder = new GameObject("Preview").transform;
            m_PreviewFolder.gameObject.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
            m_PreviewFolder.gameObject.tag = c_EditorOnly;

            //m_PreviewObjects.Clear();
        }

        #endregion

        private Reference<SceneDataEntity> m_SceneData;
        private SceneDataEntity m_SceneDataTarget;
    }
}
