using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Syadeu.Mono;
using UnityEngine.AI;

namespace SyadeuEditor
{
    [CustomEditor(typeof(CreatureBrain))]
    public sealed class CreatureBrainEditor : EditorEntity
    {
        private CreatureBrain m_Scr;
        private SerializedProperty m_CreatureName;
        private SerializedProperty m_CreatureDescription;
        private SerializedProperty m_OnCreated;
        private SerializedProperty m_OnInitialize;
        private SerializedProperty m_OnTerminate;

        private bool m_ShowOriginalContents = false;

        private void OnEnable()
        {
            m_Scr = target as CreatureBrain;

            serializedObject.FindProperty("m_NavMeshAgent").objectReferenceValue = m_Scr.GetComponent<NavMeshAgent>();
            m_CreatureName = serializedObject.FindProperty("m_CreatureName");
            m_CreatureDescription = serializedObject.FindProperty("m_CreatureDescription");
            m_OnCreated = serializedObject.FindProperty("m_OnCreated");
            m_OnInitialize = serializedObject.FindProperty("m_OnInitialize");
            m_OnTerminate = serializedObject.FindProperty("m_OnTerminate");

            serializedObject.ApplyModifiedProperties();
        }
        public override void OnInspectorGUI()
        {
            EditorUtils.StringHeader("Creature Brain");
            EditorUtils.SectorLine();

            m_CreatureName.stringValue = EditorGUILayout.TextField("Creature Name: ", m_CreatureName.stringValue);
            EditorGUILayout.LabelField("Description");
            m_CreatureDescription.stringValue = EditorGUILayout.TextArea(m_CreatureDescription.stringValue, GUILayout.MinHeight(50));
            EditorUtils.SectorLine();

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_OnCreated);
            EditorGUILayout.PropertyField(m_OnInitialize);
            EditorGUILayout.PropertyField(m_OnTerminate);

            EditorGUILayout.Space();
            serializedObject.ApplyModifiedProperties();
            m_ShowOriginalContents = EditorUtils.Foldout(m_ShowOriginalContents, "Original Contents");
            if (m_ShowOriginalContents) base.OnInspectorGUI();
        }
    }
}
