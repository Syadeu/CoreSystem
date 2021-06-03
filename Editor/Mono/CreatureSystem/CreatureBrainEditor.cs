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
        private SerializedProperty m_OnCreated;
        private SerializedProperty m_OnInitialize;
        private SerializedProperty m_OnTerminate;

        private bool m_ShowOriginalContents = false;

        private void OnEnable()
        {
            m_Scr = target as CreatureBrain;

            serializedObject.FindProperty("m_NavMeshAgent").objectReferenceValue = m_Scr.GetComponent<NavMeshAgent>();
            m_OnCreated = serializedObject.FindProperty("m_OnCreated");
            m_OnInitialize = serializedObject.FindProperty("m_OnInitialize");
            m_OnTerminate = serializedObject.FindProperty("m_OnTerminate");

            serializedObject.ApplyModifiedProperties();
        }
        public override void OnInspectorGUI()
        {
            EditorUtils.StringHeader("Creature Brain");
            EditorUtils.SectorLine();

            EditorGUILayout.PropertyField(m_OnCreated);
            EditorGUILayout.PropertyField(m_OnInitialize);
            EditorGUILayout.PropertyField(m_OnTerminate);

            EditorGUILayout.Space();
            m_ShowOriginalContents = EditorUtils.Foldout(m_ShowOriginalContents, "Original Contents");
            if (m_ShowOriginalContents) base.OnInspectorGUI();
        }
    }
}
