using UnityEditor;

using Syadeu.Mono;
using UnityEngine;

namespace Syadeu
{
    [CustomEditor(typeof(PrefabList))]
    public class PrefabListEditor : Editor
    {
        bool m_ShowOriginalContents = false;

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
            m_ShowOriginalContents = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowOriginalContents, "Original Contents");
            if (m_ShowOriginalContents) base.OnInspectorGUI();
        }
    }
}
