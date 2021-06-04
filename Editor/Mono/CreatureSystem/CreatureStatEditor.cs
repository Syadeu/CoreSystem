using UnityEditor;
using Syadeu.Mono;
using Syadeu.Mono.Creature;

namespace SyadeuEditor
{
    [CustomEditor(typeof(CreatureStat))]
    public sealed class CreatureStatEditor : EditorEntity
    {
        private CreatureStat m_Scr;

        private bool m_ShowOriginalContents = false;

        private void OnEnable()
        {
            m_Scr = target as CreatureStat;
            serializedObject.FindProperty("m_Brain").objectReferenceValue = m_Scr.GetComponent<CreatureBrain>();

            serializedObject.ApplyModifiedProperties();
        }
        public override void OnInspectorGUI()
        {
            EditorUtils.StringHeader("Creature Stat");
            EditorUtils.SectorLine();

            EditorGUILayout.Space();
            m_ShowOriginalContents = EditorUtils.Foldout(m_ShowOriginalContents, "Original Contents");
            if (m_ShowOriginalContents) base.OnInspectorGUI();
        }
    }
}
