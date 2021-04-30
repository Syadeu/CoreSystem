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

        private bool m_ShowOriginalContents = false;

        private void OnEnable()
        {
            m_Scr = target as SimpleFollower;

            m_Target = serializedObject.FindProperty("m_Target");
            m_Offset = serializedObject.FindProperty("m_Offset");
            m_Speed = serializedObject.FindProperty("m_Speed");
        }
        public override void OnInspectorGUI()
        {
            EditorUtils.StringHeader("Simple Follower");
            EditorUtils.SectorLine();

            EditorGUILayout.PropertyField(m_Target, new GUIContent("Target Transform: "));
            EditorGUILayout.PropertyField(m_Offset, new GUIContent("Offset: "));
            EditorGUILayout.PropertyField(m_Speed, new GUIContent("Follow Speed: "));

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            m_ShowOriginalContents = EditorUtils.Foldout(m_ShowOriginalContents, "Original Contents");
            if (m_ShowOriginalContents) base.OnInspectorGUI();
        }
    }
}
