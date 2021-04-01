using UnityEditor;

using SyadeuEditor;
using Syadeu.ECS;
using UnityEngine.AI;
using System.Collections.Generic;
using UnityEngine;
using Syadeu.Mono;
using Syadeu;
using Unity.Mathematics;
using Syadeu.Database;

namespace SyadeuEditor
{
    [CustomEditor(typeof(GridManager))]
    public class GridManagerEditor : EditorEntity
    {
        private GridManager m_Scr;
        private ECSPathMeshBaker m_NavBaker;

        private static float m_CellSize = 2.5f;
        private static Bounds m_Bounds;
        private static int m_GridIdx = -1;

        private bool m_EnableNavMesh;
        private bool m_EnableCellIdx;

        private bool m_ShowPreviewPanel = true;
        private bool m_ShowOriginalContents = false;

        private void OnEnable()
        {
            m_Scr = target as GridManager;
            m_NavBaker = m_Scr.GetComponent<ECSPathMeshBaker>();
        }
        private void OnDisable()
        {
            if (Application.isPlaying)
            {
                GridManager.ClearEditorGrids();
                m_GridIdx = -1;
            }
        }

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

        [System.Serializable]
        public struct TestDataStruct : ITag
        {
            public UserTagFlag UserTag { get; set; }
            public CustomTagFlag CustomTag { get; set; }

            public int Idx { get; set; }
            public float3 TestFloat3 { get; set; }

            public TestDataStruct(object other)
            {
                UserTag = UserTagFlag.UserTag3;
                CustomTag = CustomTagFlag.CustomTag10;

                Idx = 152;
                TestFloat3 = new float3(123, 123, 123);
            }
        }
        private void GridPreview()
        {
            if (Application.isPlaying)
            {
                EditorUtils.StringRich("실행 중에는 불가합니다", true);
                return;
            }

            if (GUILayout.Button("Clear All Grids"))
            {
                GridManager.ClearEditorGrids();
                m_GridIdx = -1;
                SceneView.lastActiveSceneView.Repaint();
            }
            if (GUILayout.Button(m_GridIdx < 0 ? "Create Grid" : "Reload Grid"))
            {
                if (m_GridIdx < 0)
                {
                    m_GridIdx = GridManager.CreateGrid(in m_Bounds, 2.5f, true);
                }

                GridManager.UpdateGrid(in m_GridIdx, in m_Bounds, m_CellSize, m_EnableNavMesh, true, m_EnableCellIdx);
                SceneView.lastActiveSceneView.Repaint();
            }
            EditorUtils.SectorLine();

            if (GUILayout.Button("To Bytes (Test)"))
            {
                ref GridManager.Grid grid = ref GridManager.GetGrid(in m_GridIdx);
                ref GridManager.GridCell cell = ref grid.GetCell(1);
                grid.SetCustomData(new TestDataStruct(null));
                cell.SetCustomData(new TestDataStruct(null));

                GridManager.BinaryWrapper wrapper = grid.ConvertToWrapper();
                byte[] bytes = wrapper.ToBytesWithStream();

                //GridManager.Grid newGrid = wrapper.ToGrid();
                TestDataStruct data;
                GridManager.Grid newGrid = GridManager.BinaryWrapper.ToWrapper(bytes).ToGrid();
                var newCell = newGrid.GetCell(1);

                //System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
                //stopwatch.Start();
                //newGrid.For((int i, ref GridManager.GridCell data) =>
                //{

                //});
                //stopwatch.Stop();
                //$"1. {stopwatch.ElapsedTicks}".ToLog();
                //stopwatch.Reset();
                //stopwatch.Start();
                //newGrid.For((int i, GridManager.GridCell data) =>
                //{

                //});
                //stopwatch.Stop();
                //$"2. {stopwatch.ElapsedTicks}".ToLog();

                newGrid.GetCustomData(out TestDataStruct cusData);
                newCell.GetCustomData(out TestDataStruct cusCellData);
                $"{grid.Length} :: {newGrid.Length}, {cusData.TestFloat3} :: {cusData.UserTag}".ToLog();
                $"CELL____ ::{cusCellData.TestFloat3} :: {cusCellData.UserTag}".ToLog();
            }
            if (GUILayout.Button("Connect (19.23),(20.23) (Test)"))
            {
                ref GridManager.Grid grid = ref GridManager.GetGrid(m_GridIdx);
                ref GridManager.GridCell cell = ref grid.GetCell(20, 23);

                $"{cell.Location} == 20,23".ToLog();
            }

            EditorGUILayout.Space();
            m_EnableNavMesh = EditorGUILayout.ToggleLeft("Enable NavMesh", m_EnableNavMesh);
            m_EnableCellIdx = EditorGUILayout.ToggleLeft("Enable CellIdx", m_EnableCellIdx);

            EditorGUILayout.Space();
            m_CellSize = EditorGUILayout.FloatField("CellSize: ", m_CellSize);
            m_Bounds = EditorGUILayout.BoundsField("Bounds: ", m_Bounds);
            EditorGUI.BeginDisabledGroup(m_NavBaker == null);
            if (GUILayout.Button("Match with ECSBaker"))
            {
                SerializedObject meshBaker = new SerializedObject(m_NavBaker);

                Vector3 adjust = new Vector3(m_CellSize * .5f, 0, 0);
                m_Bounds = new Bounds(m_Scr.transform.position + adjust, meshBaker.FindProperty("m_Size").vector3Value);
            }
            EditorGUI.EndDisabledGroup();
        }

        Color red = new Color(1, 0, 0, .2f);
        Color blue = new Color(0, 0, 1, .2f);
        Color green = new Color(0, 1, 1, .2f);

        private void OnSceneGUI()
        {
            if (Application.isPlaying) return;
            //ref GridManager.Grid grid = ref GridManager.s_EditorGrids[m_GridIdx];
            
            //for (int i = 0; i < grid.Length; i++)
            //{
            //    ref var cell = ref grid.GetCell(i);

            //    Handles.Label(cell.Bounds.center, $"{cell.Location.x},{cell.Location.y}");
            //}
        }
    }
}
