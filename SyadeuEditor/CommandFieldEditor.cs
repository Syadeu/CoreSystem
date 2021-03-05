
using Syadeu.Mono.Console;

using UnityEngine;
using UnityEditor;

namespace SyadeuEditor
{
    [CustomEditor(typeof(CommandField))]
    public class CommandFieldEditor : Editor
    {
        private CommandField m_Field;

        SerializedProperty m_Args;

        private bool m_ShowOriginalContents = false;

        private void OnEnable()
        {
            m_Field = target as CommandField;

            m_Args = serializedObject.FindProperty("m_Args");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();
            EditorUtils.StringHeader("Command Field", StringColor.grey, true);
            EditorUtils.SectorLine();

            EditorGUI.BeginChangeCheck();
            Arguments();
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(target);
            }

            EditorGUILayout.Space();
            m_ShowOriginalContents = EditorUtils.Foldout(m_ShowOriginalContents, "Original Contents");
            if (m_ShowOriginalContents) base.OnInspectorGUI();
        }

        private void Arguments()
        {
            m_Field.m_Field = EditorGUILayout.TextField("Field Command: ", m_Field.m_Field);
            EditorGUILayout.Space();

            CommandDefinitionEditor.ShowTypeHelpBox(m_Field.m_Type);
            m_Field.m_Type = (CommandInputType)EditorGUILayout.EnumFlagsField("Input Type: ", m_Field.m_Type);

            EditorUtils.SectorLine();
            EditorGUILayout.PropertyField(m_Args, new GUIContent("Command Arguments"));
            //EditorGUILayout.HelpBox("이 변수로 실행할 수 있는 변수들입니다", MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
