using UnityEditor;
using Syadeu.ECS;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using Syadeu.Extentions.EditorUtils;
using Syadeu;

namespace SyadeuEditor.ECS
{
    [CustomEditor(typeof(ECSPathMeshBaker))]
    public class ECSPathMeshBakerEditor : EditorEntity
    {
        private ECSPathMeshBaker m_Scr;
        private static Color s_AreaColor = new Color(0, 1, 0, .1f);

        private bool m_PreviewNavMesh;
        private NavMeshData m_NavMesh;
        private NavMeshDataInstance m_NavMeshData;
        private NavMeshBuildSettings m_NavMeshBuildSettings;
        private List<NavMeshBuildSource> m_NavMeshSources;

        private bool m_ShowOriginalContents = false;

        private void OnEnable()
        {
            m_Scr = target as ECSPathMeshBaker;

            DisableNavMeshPreview();
            m_PreviewNavMesh = false;
        }

        public override void OnInspectorGUI()
        {
            EditorUtils.StringHeader("ECS Mesh Baker");
            EditorUtils.SectorLine();

            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            if (GUILayout.Button(m_PreviewNavMesh ? "Disable Preview" : "Enable Preview"))
            {
                if (m_PreviewNavMesh) DisableNavMeshPreview();
                else EnableNavMeshPreview();

                m_PreviewNavMesh = !m_PreviewNavMesh;
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginChangeCheck();
            m_Scr.m_Size = EditorGUILayout.Vector3Field("Size: ", m_Scr.m_Size);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(target);
            }

            if (Application.isPlaying && m_PreviewNavMesh)
            {
                DisableNavMeshPreview();
                m_PreviewNavMesh = false;
            }

            m_ShowOriginalContents = EditorUtils.Foldout(m_ShowOriginalContents, "Original Contents");
            if (m_ShowOriginalContents) base.OnInspectorGUI();
        }

        private void OnSceneGUI()
        {
            GLDrawCube(m_Scr.transform.position, m_Scr.m_Size);
            GLDrawWireBounds(m_Scr.transform.position, m_Scr.m_Size);
        }

        private void EnableNavMeshPreview()
        {
            static Vector3 Quantize(Vector3 v, Vector3 quant)
            {
                float x = quant.x * Mathf.Floor(v.x / quant.x);
                float y = quant.y * Mathf.Floor(v.y / quant.y);
                float z = quant.z * Mathf.Floor(v.z / quant.z);
                return new Vector3(x, y, z);
            }
            Bounds QuantizedBounds()
            {
                return new Bounds(Quantize(m_Scr.transform.position, 0.1f * m_Scr.m_Size), m_Scr.m_Size);
            }

            m_NavMesh = new NavMeshData();
            m_NavMeshData = NavMesh.AddNavMeshData(m_NavMesh);
            m_NavMeshBuildSettings = NavMesh.GetSettingsByID(0);
            m_NavMeshSources = new List<NavMeshBuildSource>();

            var obstacles = FindObjectsOfType<ECSPathObstacleComponent>();
            for (int i = 0; i < obstacles.Length; i++)
            {
                NavMeshBuildSource source;
                if (obstacles[i].GetComponent<MeshFilter>() != null)
                {
                    var mesh = obstacles[i].GetComponent<MeshFilter>();
                    source = new NavMeshBuildSource()
                    {
                        shape = NavMeshBuildSourceShape.Mesh,
                        sourceObject = mesh.sharedMesh,
                        transform = mesh.transform.localToWorldMatrix,
                        area = 0
                    };
                }
                else if (obstacles[i].GetComponent<Terrain>() != null)
                {
                    var terrain = obstacles[i].GetComponent<Terrain>();
                    source = new NavMeshBuildSource()
                    {
                        shape = NavMeshBuildSourceShape.Terrain,
                        sourceObject = terrain.terrainData,
                        transform = Matrix4x4.TRS(terrain.transform.position, Quaternion.identity, Vector3.one),
                        area = 0
                    };
                }
                else throw new CoreSystemException(CoreSystemExceptionFlag.ECS, "NavMesh Obstacle 지정은 MeshFilter 혹은 Terrain만 가능합니다");

                m_NavMeshSources.Add(source);
            }

            NavMeshBuilder.UpdateNavMeshData(m_NavMesh, m_NavMeshBuildSettings, m_NavMeshSources, QuantizedBounds());
        }
        private void DisableNavMeshPreview()
        {
            if (!m_PreviewNavMesh) return;

            m_NavMeshData.Remove();
            m_NavMeshSources = null;

            SceneView.lastActiveSceneView.Repaint();
        }
    }
}
