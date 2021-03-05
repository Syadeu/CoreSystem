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
            EditorUtils.StringHeader("Command Definition", StringColor.grey, true);
            EditorGUILayout.Space();
            if (IsListed())
            {
                EditorUtils.StringRich("Added", StringColor.teal, true);
                if (EditorUtils.Button("Remove"))
                {
                    SyadeuSettings.Instance.m_CommandDefinitions.Remove(m_Def);
                    EditorUtility.SetDirty(SyadeuSettings.Instance);
                }
            }
            else
            {
                EditorUtils.StringRich("Not Added", StringColor.maroon, true);
                if (EditorUtils.Button("Add"))
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
            m_Def.m_Initializer = EditorGUILayout.TextField("Initializer: ", m_Def.m_Initializer);
            EditorGUILayout.Space();

            //EditorGUILayout.HelpBox("ÃßÈÄ Ãß°¡µÇ´Â ±â´ÉÀÔ´Ï´Ù. ÇöÀç´Â ¾Æ¹«·± ±â´ÉÀ» ÇÏÁö ¾Ê½À´Ï´Ù.", MessageType.Info);
            ShowTypeHelpBox(m_Def.m_Settings);
            m_Def.m_Settings = (CommandSetting)EditorGUILayout.EnumFlagsField("Input Type: ", m_Def.m_Settings);

            EditorUtils.SectorLine();
            EditorGUILayout.PropertyField(m_Args, new GUIContent("Command Arguments"));
            //EditorGUILayout.HelpBox("ÀÌ ¸í·É¾î·Î ½ÇÇàÇÒ ¼ö ÀÖ´Â º¯¼öµéÀÔ´Ï´Ù", MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }

        internal static void ShowTypeHelpBox(CommandSetting inputType)
        {
            if (inputType == 0)
            {
                EditorGUILayout.HelpBox("타입이 선택되지 않았습니다. 아무런 추가 기능을 수행하지않습니다.", MessageType.Info);
                return;
            }

            string typeHelpTxt = null;
            if (inputType.HasFlag(CommandSetting.ShowIfRequiresTrue))
            {
                EditorUtils.AutoString(ref typeHelpTxt,
                    "ShowIfRequiresTrue: Requires 프로퍼티의 리턴이 예상값과 일치할 경우에만 자동완성 및 콘솔창에 표시됩니다.");
            }
            EditorGUILayout.HelpBox(typeHelpTxt, MessageType.Info);
        }
    }
}
