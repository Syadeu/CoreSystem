using UnityEditor;

using SyadeuEditor;
using Syadeu.ECS;
using UnityEngine.AI;
using System.Collections.Generic;
using UnityEngine;
using Syadeu.Mono;
using Syadeu;
using Unity.Mathematics;

namespace SyadeuEditor
{
    [CustomEditor(typeof(GridManager))]
    public class GridManagerEditor : EditorEntity
    {
        private GridManager m_Scr;
        private ECSPathMeshBaker m_NavBaker;

        private static Bounds m_Bounds;
        private static int m_GridIdx;

        private bool m_EnableNavMesh;
        private SerializedProperty m_CellSize;

        private bool m_ShowPreviewPanel = true;
        private bool m_ShowOriginalContents = false;

        private void OnEnable()
        {
            m_Scr = target as GridManager;
            m_NavBaker = m_Scr.GetComponent<ECSPathMeshBaker>();

            m_CellSize = serializedObject.FindProperty("m_CellSize");

            m_GridIdx = GridManager.CreateGrid(in m_Bounds, 2.5f, true);
        }

        //private void OnDestroy()
        //{
        //    GridManager.m_EditorGrids = new GridManager.Grid[0];
        //}

        public override void OnInspectorGUI()
        {
            EditorUtils.StringHeader("Grid Manager");
            EditorUtils.SectorLine();

            EditorGUILayout.Space();

            m_ShowPreviewPanel = EditorUtils.Foldout(m_ShowPreviewPanel, "Preview", 13);
            if (m_ShowPreviewPanel) GridPreview();

            EditorGUILayout.Space();
            m_ShowOriginalContents = EditorUtils.Foldout(m_ShowOriginalContents, "Original Contents");
            if (m_ShowOriginalContents) base.OnInspectorGUI();
        }

        private void GridPreview()
        {
            if (GUILayout.Button("Reload Grid"))
            {
                GridManager.UpdateGrid(in m_GridIdx, in m_Bounds, m_CellSize.floatValue, m_EnableNavMesh);
                SceneView.lastActiveSceneView.Repaint();
            }
            if (GUILayout.Button("To Bytes (Test)"))
            {
                ref GridManager.Grid grid = ref GridManager.GetGrid(in m_GridIdx);
                grid.SetCustomData(new float3(123, 123, 123));

                GridManager.BinaryWrapper wrapper = grid.Convert();

                GridManager.Grid newGrid = wrapper.ToGrid();

                $"{grid.Length} :: {newGrid.Length}, {newGrid.GetCustomData<float3>()}".ToLog();
            }

            EditorGUILayout.Space();
            m_EnableNavMesh = EditorGUILayout.ToggleLeft("Enable NavMesh", m_EnableNavMesh);

            EditorGUILayout.Space();
            m_Bounds = EditorGUILayout.BoundsField("Bounds: ", m_Bounds);
            EditorGUI.BeginDisabledGroup(m_NavBaker == null);
            if (GUILayout.Button("Match with ECSBaker"))
            {
                SerializedObject meshBaker = new SerializedObject(m_NavBaker);

                Vector3 adjust = new Vector3(m_CellSize.floatValue * .5f, 0, 0);
                m_Bounds = new Bounds(m_Scr.transform.position + adjust, meshBaker.FindProperty("m_Size").vector3Value);
            }
            EditorGUI.EndDisabledGroup();
        }

        Color red = new Color(1, 0, 0, .2f);
        Color blue = new Color(0, 0, 1, .2f);
        Color green = new Color(0, 1, 1, .2f);

        private void OnSceneGUI()
        {
            ref GridManager.Grid grid = ref GridManager.s_EditorGrids[m_GridIdx];
            
            for (int i = 0; i < grid.Length; i++)
            {
                ref var cell = ref grid.GetCell(i);

                if (cell.BlockedByNavMesh)
                {
                    GLDrawBounds(in cell.Bounds, red);
                }
                else GLDrawBounds(in cell.Bounds, i % 2 == 0 ? green : blue);

                Handles.Label(cell.Bounds.center, $"{cell.Location.x},{cell.Location.y}");
            }
        }
    }
}
