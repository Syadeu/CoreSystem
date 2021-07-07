using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Syadeu.Mono;
using UnityEngine.AI;
using Syadeu.Database;
using Syadeu.Internal;
using System;
using Syadeu.Mono.Creature;

namespace SyadeuEditor
{
    [CustomEditor(typeof(CreatureBrain))]
    public sealed class CreatureBrainEditor : EditorEntity<CreatureBrain>
    {
        private SerializedProperty m_CreatureName;
        private SerializedProperty m_CreatureDescription;
        private SerializedProperty m_InitializeOnStart;
        private SerializedProperty m_OnCreated;
        private SerializedProperty m_OnInitialize;
        private SerializedProperty m_OnTerminate;

        private ValuePairContainer Components;

        private bool m_ShowDelegates = false;
        private bool m_ShowOriginalContents = false;

        [Serializable]
        private class CreatureValue : ValuePair<CreatureEntity>
        {
            public override object Clone()
            {
                throw new System.NotImplementedException();
            }
        }

        private void OnEnable()
        {
            serializedObject.FindProperty("m_NavMeshAgent").objectReferenceValue = Asset.GetComponent<NavMeshAgent>();
            m_CreatureName = serializedObject.FindProperty("m_CreatureName");
            m_CreatureDescription = serializedObject.FindProperty("m_CreatureDescription");
            m_InitializeOnStart = serializedObject.FindProperty("m_InitializeOnStart");
            m_OnCreated = serializedObject.FindProperty("m_OnCreated");
            m_OnInitialize = serializedObject.FindProperty("m_OnInitialize");
            m_OnTerminate = serializedObject.FindProperty("m_OnTerminate");

            serializedObject.ApplyModifiedProperties();

            Components = new ValuePairContainer();
            CreatureEntity[] componentList = Asset.GetComponentsInChildren<CreatureEntity>();
            for (int i = 0; i < componentList.Length; i++)
            {
                Components.Add(new CreatureValue()
                {
                    Name = componentList[i].GetType().Name,
                    m_Value = componentList[i],
                });
            }
        }
        public override void OnInspectorGUI()
        {
            EditorUtils.StringHeader("Creature Brain");
            EditorUtils.SectorLine();

            if (Application.isPlaying)
            {
                EditorUtils.StringRich(Asset.Initialized ? "Initialized" : "Not Initialized", true);
            }
            else PrefabListEditor.DrawPrefabAdder(Asset.gameObject);
            EditorUtils.Line();

            EditorGUI.BeginDisabledGroup(Application.isPlaying);

            EditorGUILayout.BeginVertical(EditorUtils.Box);
            {
                m_CreatureName.stringValue = EditorGUILayout.TextField("Creature Name: ", m_CreatureName.stringValue);
                EditorGUILayout.LabelField("Description");
                m_CreatureDescription.stringValue = EditorGUILayout.TextArea(m_CreatureDescription.stringValue, GUILayout.MinHeight(50));
            }
            EditorGUILayout.EndVertical();

            EditorUtils.Line();

            EditorGUILayout.BeginVertical(EditorUtils.Box);
            if (m_InitializeOnStart.boolValue && FindObjectOfType<CreatureManager>() == null)
            {
                EditorGUILayout.HelpBox(
                    "We couldn\'t found CreatureManager but you trying to initialize on start.\n" +
                    "This creature will never initialize.", MessageType.Error);
            }
            EditorGUILayout.PropertyField(m_InitializeOnStart);
            //Asset.m_EnableCameraCull = EditorGUILayout.Toggle("", Asset.m_EnableCameraCull);
            EditorGUILayout.EndVertical();

            EditorUtils.Line();

            EditorGUILayout.BeginVertical(EditorUtils.Box);
            EditorGUI.indentLevel += 1;
            m_ShowDelegates = EditorUtils.Foldout(m_ShowDelegates, "Delegates");
            if (m_ShowDelegates)
            {
                EditorGUILayout.PropertyField(m_OnCreated);
                EditorGUILayout.PropertyField(m_OnInitialize);
                EditorGUILayout.PropertyField(m_OnTerminate);
            }
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.EndVertical();

            EditorUtils.Line();

            Components.DrawValueContainer("Components", ValuePairEditor.DrawMenu.None, null);

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();
            serializedObject.ApplyModifiedProperties();
            m_ShowOriginalContents = EditorUtils.Foldout(m_ShowOriginalContents, "Original Contents");
            if (m_ShowOriginalContents) base.OnInspectorGUI();
        }
    }
}
