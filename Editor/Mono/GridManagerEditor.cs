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

        private SerializedProperty m_Bounds;

        private GridManager.Grid grid;

        private bool m_ShowOriginalContents = false;

        private void OnEnable()
        {
            m_Scr = target as GridManager;
            m_NavBaker = m_Scr.GetComponent<ECSPathMeshBaker>();

            m_Bounds = serializedObject.FindProperty("m_Bounds");


            grid = GridManager.CreateGrid(m_Bounds.boundsValue, 2.5f);
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
                grid = GridManager.CreateGrid(m_Bounds.boundsValue, 2.5f);
                SceneView.lastActiveSceneView.Repaint();
            }
            EditorGUI.BeginDisabledGroup(m_NavBaker == null);
            if (GUILayout.Button("Match with ECSBaker"))
            {
                m_Bounds.boundsValue = new Bounds(m_Scr.transform.position, m_NavBaker.m_Size);
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
            for (int i = 0; i < grid.Cells.Length; i++)
            {
                GLDrawBounds(grid.Cells[i].Bounds, i % 2 == 0 ? green : blue);
                //if (i > 1) break;
            }
        }
    }
}
