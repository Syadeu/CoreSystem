using UnityEditor;
using Syadeu.Mono.Creature;

namespace SyadeuEditor
{
    [CustomEditor(typeof(CreatureSettings))]
    public sealed class CreatureSettingsEditor : Editor
    {
        private CreatureSettings m_Scr;

        private bool m_ShowOriginalContents = false;

        private void OnEnable()
        {
            m_Scr = target as CreatureSettings;
        }
        public override void OnInspectorGUI()
        {
            EditorUtils.StringHeader("Creature Settings");
            EditorUtils.SectorLine();

            EditorGUILayout.Space();
            m_ShowOriginalContents = EditorUtils.Foldout(m_ShowOriginalContents, "Original Contents");
            if (m_ShowOriginalContents) base.OnInspectorGUI();
        }
    }
}
