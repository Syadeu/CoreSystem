using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Syadeu.ECS;

namespace SyadeuEditor.ECS
{
    [CustomEditor(typeof(ECSPathObstacleComponent))]
    public class ECSPathObstacleEditor : Editor
    {
        ECSPathObstacleComponent obstacle;

        private bool isMeshFilter = false;
        private bool isTerrain = false;
        private SerializedProperty objProperty;

        private bool m_ShowOriginalContents = false;

        private void OnEnable()
        {
            obstacle = target as ECSPathObstacleComponent;

            if (!Application.isPlaying)
            {
                objProperty = serializedObject.FindProperty("obj");

                MeshFilter meshFilter = obstacle.GetComponent<MeshFilter>();
                Terrain terrain = obstacle.GetComponent<Terrain>();

                if (meshFilter != null)
                {
                    objProperty.objectReferenceValue = meshFilter;
                    isMeshFilter = true;
                }
                else if (terrain != null)
                {
                    objProperty.objectReferenceValue = terrain;
                    isTerrain = true;
                }

                serializedObject.ApplyModifiedProperties();
            }
            else
            {
                isMeshFilter = obstacle.GetComponent<MeshFilter>() != null;
                isTerrain = obstacle.GetComponent<Terrain>() != null;
            }
        }

        public override void OnInspectorGUI()
        {
            EditorUtils.StringHeader("ECS Obstacle");
            EditorUtils.SectorLine();

            if (!isMeshFilter && !isTerrain)
            {
                EditorUtils.StringRich("MeshFilter 혹은 Terrain을 추가해주세요", StringColor.maroon, true);
                return;
            }
            string current = isMeshFilter ? "MeshFilter" : "Terrain";
            EditorUtils.StringRich($"현재: {current}", StringColor.grey, true);


            m_ShowOriginalContents = EditorUtils.Foldout(m_ShowOriginalContents, "Original Contents");
            if (m_ShowOriginalContents) base.OnInspectorGUI();
        }
    }
}
