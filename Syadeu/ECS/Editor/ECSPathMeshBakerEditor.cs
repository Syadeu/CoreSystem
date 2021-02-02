using UnityEditor;
using Syadeu.ECS;

namespace SyadeuEditor.ECS
{
    [CustomEditor(typeof(ECSPathMeshBaker))]
    public class ECSPathMeshBakerEditor : Editor
    {
        ECSPathMeshBaker baker;

        private bool m_ShowOriginalContents = false;

        private void OnEnable()
        {
            baker = target as ECSPathMeshBaker;
        }

        public override void OnInspectorGUI()
        {
            EditorUtils.StringHeader("ECS Mesh Baker");
            EditorUtils.SectorLine();

            m_ShowOriginalContents = EditorUtils.Foldout(m_ShowOriginalContents, "Original Contents");
            if (m_ShowOriginalContents) base.OnInspectorGUI();
        }
    }
}
