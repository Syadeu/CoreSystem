using Syadeu;
using Syadeu.Mono;
using System.Collections.Generic;
using UnityEditor;

namespace SyadeuEditor
{
    [CustomEditor(typeof(PrefabList))]
    public class PrefabListEditor : Editor
    {
        private bool m_ShowOriginalContents = false;
        private static string[] m_PrefabNames = null;

        public void OnEnable()
        {
            CheckConditions();
        }
        public override void OnInspectorGUI()
        {
            EditorUtils.StringHeader("Prefab List");
            EditorUtils.SectorLine();

            EditorGUI.BeginChangeCheck();

            var objSettings = serializedObject.FindProperty("m_ObjectSettings");
            EditorGUILayout.PropertyField(objSettings);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.Space();
            m_ShowOriginalContents = EditorUtils.Foldout(m_ShowOriginalContents, "Original Contents");
            if (m_ShowOriginalContents) base.OnInspectorGUI();
        }
        private static void CheckConditions()
        {
            if (m_PrefabNames == null || m_PrefabNames.Length != PrefabList.Instance.ObjectSettings.Count)
            {
                IReadOnlyList<PrefabList.ObjectSetting> list = PrefabList.Instance.ObjectSettings;
                m_PrefabNames = new string[list.Count];
                for (int i = 0; i < m_PrefabNames.Length; i++)
                {
                    m_PrefabNames[i] = string.IsNullOrEmpty(list[i].m_Name) ? list[i].Prefab.name : list[i].m_Name;
                }
            }
        }

        public static int DrawPrefabSelector(int i)
        {
            CheckConditions();
            i = EditorGUILayout.Popup("Prefab: ", i, m_PrefabNames);

            return i;
        }
    }
}
