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

        private bool m_ShowOriginalContents = false;

        private void OnEnable()
        {
            m_Def = target as CommandDefinition;
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
                if (SyadeuSettings.Instance.m_CommandDefinitions[i] == m_Def) return true;
            }
            return false;
        }

        private void Arguments()
        {
            m_Def.m_Initializer = EditorGUILayout.TextField("시작 명령어: ", m_Def.m_Initializer);
            EditorGUILayout.Space();
        }
    }
}
