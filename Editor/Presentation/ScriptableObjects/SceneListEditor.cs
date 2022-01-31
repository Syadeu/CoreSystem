using Syadeu.Mono;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Syadeu.Presentation;
using Syadeu.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace SyadeuEditor
{
    [CustomEditor(typeof(SceneSettings))]
    public sealed class SceneListEditor : EditorEntity<SceneSettings>
    {
        SceneReference m_CurrentScene;

        BinaryGrid grid;
        float cellSize = 2.5f;
        Vector3Int center;
        Vector3Int size;

        private void OnEnable()
        {
            Scene scene = SceneManager.GetActiveScene();

            for (int i = 0; i < Target.Scenes.Count; i++)
            {
                if (Target.Scenes[i].ScenePath.Equals(scene.path))
                {
                    m_CurrentScene = Target.Scenes[i];
                    break;
                }
            }

            //if (m_CurrentScene == null || m_CurrentScene.m_SceneGridData == null ||
            //    m_CurrentScene.m_SceneGridData.Length == 0) return;

            //grid = ManagedGrid.FromBinary(m_CurrentScene.m_SceneGridData);
            //cellSize = grid.cellSize;
            //center = Vector3Int.FloorToInt(grid.center);
            //size = Vector3Int.FloorToInt(grid.size);

            //GridManager.ImportGrids(grid);
            //GridManager.GetGrid(grid.Idx).EnableDrawGL = true;
        }
        private void OnDisable()
        {
            //GridManager.ClearEditorGrids();
        }
        public override void OnInspectorGUI()
        {
            if (m_CurrentScene == null)
            {
                EditorGUILayout.LabelField("This scene is not in the scenelist");

                //return;
            }
            //EditorUtils.StringHeader("Grid");
            //center = EditorGUILayout.Vector3IntField("Center: ", center);
            //size = EditorGUILayout.Vector3IntField("Size: ", size);
            //cellSize = EditorGUILayout.FloatField("Cell Size: ", cellSize);

            //if (GUILayout.Button("make"))
            //{
            //    grid = new ManagedGrid(new int3(center.x, center.y, center.z),
            //    new int3(size.x, size.y, size.z), cellSize);
            //}
            //if (GUILayout.Button("Save"))
            //{
            //    m_CurrentScene.m_SceneGridData = grid.ToBinary();
            //    EditorUtils.SetDirty(target);
            //}
            //if (GUILayout.Button("test"))
            //{
            //    grid.GetCell(0).SetValue(Hash.NewHash());
            //}
            //if (GUILayout.Button("remove"))
            //{
            //    grid = null;
            //    EditorUtils.SetDirty(target);
            //}

            //if (grid != null)
            //{
            //    ManagedCell cell = grid.GetCell(0);
            //    EditorGUILayout.LabelField(cell.GetValue()?.ToString());
            //}

            EditorGUILayout.Space();
            base.OnInspectorGUI();
        }
    }
}
