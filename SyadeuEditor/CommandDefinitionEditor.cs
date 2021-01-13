using System.Collections;
using System.Collections.Generic;

using Syadeu.Mono.Console;

using UnityEngine;
using UnityEditor;
using Syadeu.Mono;

namespace SyadeuEditor
{
    [CustomEditor(typeof(CommandDefinition))]
    public class CommandDefinitionEditor : Editor
    {
        private CommandDefinition m_Def;

        SerializedProperty m_Args;

        private bool m_ShowOriginalContents = false;

        private void OnEnable()
        {
            m_Def = target as CommandDefinition;

            m_Args = serializedObject.FindProperty("m_Args");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();
            EditorUtils.StringHeader("Command Definition", StringColor.black, true);
            EditorGUILayout.Space();
            if (IsListed())
            {
                EditorUtils.StringRich("등록된 커맨드", StringColor.teal, true);
                if (EditorUtils.Button("해제"))
                {
                    SyadeuSettings.Instance.m_CommandDefinitions.Remove(m_Def);
                    EditorUtility.SetDirty(SyadeuSettings.Instance);
                }
            }
            else
            {
                EditorUtils.StringRich("해제된 커맨드", StringColor.maroon, true);
                if (EditorUtils.Button("등록"))
                {
                    SyadeuSettings.Instance.m_CommandDefinitions.Add(m_Def);
                    EditorUtility.SetDirty(SyadeuSettings.Instance);
                }
            }
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

        private bool IsListed()
        {
            for (int i = 0; i < SyadeuSettings.Instance.m_CommandDefinitions.Count; i++)
            {
                if (SyadeuSettings.Instance.m_CommandDefinitions[i] == null)
                {
                    SyadeuSettings.Instance.m_CommandDefinitions.RemoveAt(i);
                    i--;
                    EditorUtils.SetDirty(SyadeuSettings.Instance);
                    continue;
                }

                if (SyadeuSettings.Instance.m_CommandDefinitions[i] == m_Def) return true;
            }
            return false;
        }

        private void Arguments()
        {
            m_Def.m_Initializer = EditorGUILayout.TextField("시작 명령어: ", m_Def.m_Initializer);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox("추후 추가되는 기능입니다. 현재는 아무런 기능을 하지 않습니다.", MessageType.Info);
            m_Def.m_Type = (CommandInputType)EditorGUILayout.EnumFlagsField("인풋 타입: ", m_Def.m_Type);

            EditorUtils.SectorLine();
            EditorGUILayout.PropertyField(m_Args, new GUIContent("명령 변수"));
            EditorGUILayout.HelpBox("이 명령어로 실행할 수 있는 변수들입니다", MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
