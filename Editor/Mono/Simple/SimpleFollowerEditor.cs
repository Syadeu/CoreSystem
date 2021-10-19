using Syadeu.Mono;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEditor;
using UnityEngine;

namespace SyadeuEditor
{
    [CustomEditor(typeof(SimpleFollower))]
    public sealed class SimpleFollowerEditor : EditorEntity
    {
        private SimpleFollower m_Scr = null;

        private SerializedProperty m_Target;
        private SerializedProperty m_Offset;
        private SerializedProperty m_Speed;
        private SerializedProperty m_UpdateType;
        private SerializedProperty m_UpdateAt;

        private int m_SelectedUpdateType = 0;
        private int m_SelectedUpdateAt = 0;
        private static readonly string[] m_UpdateTypeString = new string[] { "Instant", "Lerp" };
        private static readonly string[] m_UpdateAtString = new string[] { "Update", "LateUpdate", "FixedUpdate" };

        private bool m_ShowOriginalContents = false;

        private void OnEnable()
        {
            m_Scr = target as SimpleFollower;

            m_Target = serializedObject.FindProperty("m_Target");
            m_Offset = serializedObject.FindProperty("m_Offset");
            m_Speed = serializedObject.FindProperty("m_Speed");
            m_UpdateType = serializedObject.FindProperty("m_UpdateType");
            m_UpdateAt = serializedObject.FindProperty("m_UpdateAt");

            m_SelectedUpdateType = m_UpdateType.intValue;
            m_SelectedUpdateAt = m_UpdateAt.intValue;
        }
        public override void OnInspectorGUI()
        {
            EditorUtilities.StringHeader("Simple Follower");
            EditorUtilities.SectorLine();

            EditorGUILayout.PropertyField(m_Target, new GUIContent("Target Transform: "));
            EditorGUI.BeginChangeCheck();
            m_SelectedUpdateType = EditorGUILayout.Popup("Update Type: ", m_SelectedUpdateType, m_UpdateTypeString);
            m_SelectedUpdateAt = EditorGUILayout.Popup("Update At: ", m_SelectedUpdateAt, m_UpdateAtString);
            if (EditorGUI.EndChangeCheck())
            {
                m_UpdateType.intValue = m_SelectedUpdateType;
                m_UpdateAt.intValue = m_SelectedUpdateAt;
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_Offset, new GUIContent("Offset: "));
            if (m_SelectedUpdateType == 1) EditorGUILayout.PropertyField(m_Speed, new GUIContent("Follow Speed: "));

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            m_ShowOriginalContents = EditorUtilities.Foldout(m_ShowOriginalContents, "Original Contents");
            if (m_ShowOriginalContents) base.OnInspectorGUI();
        }
    }
}
