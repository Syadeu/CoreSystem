using Syadeu.Mono;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Syadeu.Presentation;
using Unity.Mathematics;
using UnityEngine;

namespace SyadeuEditor
{
    [CustomEditor(typeof(SceneList))]
    public sealed class SceneListEditor : EditorEntity<SceneList>
    {
        SceneReference m_CurrentScene;

        int gridIdx = -1;
        float cellSize = 2.5f;
        Vector3 center;
        Vector3 size;

        private void OnEnable()
        {
            Scene scene = SceneManager.GetActiveScene();

            for (int i = 0; i < Asset.Scenes.Count; i++)
            {
                if (Asset.Scenes[i].ScenePath.Equals(scene.path))
                {
                    m_CurrentScene = Asset.Scenes[i];
                    break;
                }
            }

            if (m_CurrentScene == null || m_CurrentScene.m_SceneData.Length == 0) return;

            var wrapper = GridManager.BinaryWrapper.ToWrapper(m_CurrentScene.m_SceneData);
            var grid = wrapper.ToGrid();

            gridIdx = grid.Idx;
            cellSize = grid.CellSize;
            center = grid.GetBounds().center;
            size = grid.GetBounds().size;

            GridManager.ImportGrids(grid);
            GridManager.GetGrid(grid.Idx).EnableDrawGL = true;
        }
        private void OnDisable()
        {
            GridManager.ClearEditorGrids();
        }
        public override void OnInspectorGUI()
        {
            if (m_CurrentScene == null)
            {
                EditorGUILayout.LabelField("This scene is not in the scenelist");

                return;
            }
            EditorUtils.StringHeader("Grid");
            center = EditorGUILayout.Vector3Field("Center: ", center);
            size = EditorGUILayout.Vector3Field("Size: ", size);
            cellSize = EditorGUILayout.FloatField("Cell Size: ", cellSize);

            if (GUILayout.Button("make"))
            {
                gridIdx = GridManager.CreateGrid(new Bounds(center, size), cellSize, false);
                GridManager.GetGrid(gridIdx).EnableDrawGL = true;
            }
            if (GUILayout.Button("Save"))
            {
                m_CurrentScene.m_SceneData = GridManager.GetGrid(gridIdx).ConvertToWrapper().ToBinary();
                EditorUtils.SetDirty(target);
            }
            if (GUILayout.Button("remove"))
            {
                if (m_CurrentScene != null) m_CurrentScene.m_SceneData = null;
                GridManager.ClearEditorGrids();
                EditorUtils.SetDirty(target);
            }

            EditorGUILayout.Space();
            base.OnInspectorGUI();
        }
    }
}
