using UnityEditor;
using Syadeu.ECS;
using UnityEngine;

namespace SyadeuEditor.ECS
{
    [CustomEditor(typeof(ECSPathSurface))]
    public class ECSPathSurfaceEditor : EditorEntity
    {
        private ECSPathSurface m_Scr;
        private bool m_HasBaker;

        private SerializedProperty objProperty;

        private bool m_ShowOriginalContents = false;

        private void OnEnable()
        {
            m_Scr = target as ECSPathSurface;

            m_HasBaker = FindObjectOfType<ECSPathMeshBaker>() != null;

            if (!Application.isPlaying)
            {
                objProperty = serializedObject.FindProperty("obj");

                MeshFilter meshFilter = m_Scr.GetComponent<MeshFilter>();
                Terrain terrain = m_Scr.GetComponent<Terrain>();

                if (meshFilter != null)
                {
                    objProperty.objectReferenceValue = meshFilter;
                }
                else if (terrain != null)
                {
                    objProperty.objectReferenceValue = terrain;
                }

                serializedObject.ApplyModifiedProperties();
            }
        }

        public override void OnInspectorGUI()
        {
            EditorUtils.StringHeader("ECS Surface");
            EditorUtils.SectorLine();

            string searchBakerTxt;
            if (m_HasBaker) searchBakerTxt = "";
            else searchBakerTxt = "";

            EditorGUILayout.Space();
            m_ShowOriginalContents = EditorUtils.Foldout(m_ShowOriginalContents, "Original Contents");
            if (m_ShowOriginalContents) base.OnInspectorGUI();
        }
    }
}
