using UnityEditor;

using SyadeuEditor;
using Syadeu.ECS;
using UnityEngine.AI;
using System.Collections.Generic;
using UnityEngine;
using Syadeu.Mono;

namespace SyadeuEditor
{
    [CustomEditor(typeof(GridManager))]
    public class GridManagerEditor : EditorEntity
    {
        private GridManager m_Scr;
        private ECSPathMeshBaker m_NavBaker;

        private Bounds m_Bounds;
        private GridManager.Grid grid;

        private SerializedProperty m_GridSize;

        private bool m_ShowOriginalContents = false;

        private void OnEnable()
        {
            m_Scr = target as GridManager;
            m_NavBaker = m_Scr.GetComponent<ECSPathMeshBaker>();

            m_GridSize = serializedObject.FindProperty("m_GridSize");

            grid = GridManager.CreateGrid(m_Bounds, 2.5f);
        }

        private void OnDestroy()
        {

        }

        public override void OnInspectorGUI()
        {
            EditorUtils.StringHeader("Grid Manager");
            EditorUtils.SectorLine();

            if (GUILayout.Button("Reload Grid"))
            {
                grid = GridManager.CreateGrid(m_Bounds, 2.5f);
                SceneView.lastActiveSceneView.Repaint();
            }
            EditorGUI.BeginDisabledGroup(m_NavBaker == null);
            if (GUILayout.Button("Match with ECSBaker"))
            {
                Vector3 adjust = new Vector3(m_GridSize.floatValue * .5f, 0, 0);
                m_Bounds = new Bounds(m_Scr.transform.position + adjust, m_NavBaker.m_Size);
                serializedObject.ApplyModifiedProperties();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();
            m_ShowOriginalContents = EditorUtils.Foldout(m_ShowOriginalContents, "Original Contents");
            if (m_ShowOriginalContents) base.OnInspectorGUI();
        }

        Color red = new Color(1, 0, 0, 1f);
        Color blue = new Color(0, 0, 1, 1f);
        Color green = new Color(0, 1, 1);

        private void OnSceneGUI()
        {
            for (int i = 0; i < grid.Cells?.Length; i++)
            {
                GLDrawBounds(grid.Cells[i].Bounds, i % 2 == 0 ? green : blue);
                //if (i > 1) break;
            }
        }
    }
}
