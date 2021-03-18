using UnityEditor;

using SyadeuEditor;
using Syadeu.ECS;
using UnityEngine.AI;
using System.Collections.Generic;
using UnityEngine;
using Syadeu.Mono;
using Syadeu;

namespace SyadeuEditor
{
    [CustomEditor(typeof(GridManager))]
    public class GridManagerEditor : EditorEntity
    {
        private GridManager m_Scr;
        private ECSPathMeshBaker m_NavBaker;

        private static Bounds m_Bounds;
        private static int m_GridIdx;

        private SerializedProperty m_GridSize;

        private bool m_ShowOriginalContents = false;

        private void OnEnable()
        {
            m_Scr = target as GridManager;
            m_NavBaker = m_Scr.GetComponent<ECSPathMeshBaker>();

            m_GridSize = serializedObject.FindProperty("m_GridSize");

            m_GridIdx = GridManager.CreateGrid(in m_Bounds, 2.5f);
        }

        private void OnDestroy()
        {
            GridManager.m_EditorGrids = new GridManager.Grid[0];
        }

        public override void OnInspectorGUI()
        {
            EditorUtils.StringHeader("Grid Manager");
            EditorUtils.SectorLine();

            if (GUILayout.Button("Reload Grid"))
            {
                GridManager.UpdateGrid(in m_GridIdx, in m_Bounds, m_GridSize.floatValue);
                //$"{grid.Cells.Length} :: {GridManager.m_EditorGrids[0].Cells.Length} :: created".ToLog();
                SceneView.lastActiveSceneView.Repaint();
            }

            EditorGUILayout.Space();
            m_Bounds = EditorGUILayout.BoundsField("Bounds: ", m_Bounds);
            EditorGUI.BeginDisabledGroup(m_NavBaker == null);
            if (GUILayout.Button("Match with ECSBaker"))
            {
                Vector3 adjust = new Vector3(m_GridSize.floatValue * .5f, 0, 0);
                m_Bounds = new Bounds(m_Scr.transform.position + adjust, m_NavBaker.m_Size);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();
            m_ShowOriginalContents = EditorUtils.Foldout(m_ShowOriginalContents, "Original Contents");
            if (m_ShowOriginalContents) base.OnInspectorGUI();
        }

        Color red = new Color(1, 0, 0, .2f);
        Color blue = new Color(0, 0, 1, .2f);
        Color green = new Color(0, 1, 1, .2f);

        private void OnSceneGUI()
        {
            ref GridManager.Grid grid = ref GridManager.m_EditorGrids[m_GridIdx];
            for (int i = 0; i < grid.Cells.Length; i++)
            {
                GLDrawBounds(in grid.Cells[i].Bounds, i % 2 == 0 ? green : blue);
            }
        }
    }
}
