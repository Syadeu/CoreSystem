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
                EditorUtils.StringRich("��ϵ� Ŀ�ǵ�", StringColor.teal, true);
                if (EditorUtils.Button("����"))
                {
                    SyadeuSettings.Instance.m_CommandDefinitions.Remove(m_Def);
                    EditorUtility.SetDirty(SyadeuSettings.Instance);
                }
            }
            else
            {
                EditorUtils.StringRich("������ Ŀ�ǵ�", StringColor.maroon, true);
                if (EditorUtils.Button("���"))
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
            m_Def.m_Initializer = EditorGUILayout.TextField("���� ��ɾ�: ", m_Def.m_Initializer);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox("���� �߰��Ǵ� ����Դϴ�. ����� �ƹ��� ����� ���� �ʽ��ϴ�.", MessageType.Info);
            m_Def.m_Type = (CommandInputType)EditorGUILayout.EnumFlagsField("��ǲ Ÿ��: ", m_Def.m_Type);

            EditorUtils.SectorLine();
            EditorGUILayout.PropertyField(m_Args, new GUIContent("��� ����"));
            EditorGUILayout.HelpBox("�� ��ɾ�� ������ �� �ִ� �������Դϴ�", MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
