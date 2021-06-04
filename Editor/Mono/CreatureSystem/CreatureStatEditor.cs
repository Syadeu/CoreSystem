using Syadeu.Mono.Creature;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor
{
    [CustomEditor(typeof(CreatureStat))]
    public sealed class CreatureStatEditor : EditorEntity
    {
        private static readonly GUIContent m_StatReferenceLable = new GUIContent("Stat Reference: ");

        private CreatureStat m_Scr;
        private SerializedProperty m_StatReference;

        private bool m_ShowOriginalContents = false;

        private void OnEnable()
        {
            m_Scr = target as CreatureStat;

            m_StatReference = serializedObject.FindProperty("m_StatReference");

        }
        public override void OnInspectorGUI()
        {
            EditorUtils.StringHeader("Creature Stat");
            EditorUtils.SectorLine();

            EditorGUILayout.PropertyField(m_StatReference, m_StatReferenceLable);
            if (m_StatReference.objectReferenceValue == null)
            {
                if (GUILayout.Button("Create"))
                {

                }
            }
            else EditorGUILayout.Space();

            EditorGUILayout.Space();
            serializedObject.ApplyModifiedProperties();
            m_ShowOriginalContents = EditorUtils.Foldout(m_ShowOriginalContents, "Original Contents");
            if (m_ShowOriginalContents) base.OnInspectorGUI();
        }
    }
}
