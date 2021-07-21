using Syadeu.Database;
using Syadeu.Mono;
using UnityEditor;
using UnityEngine;

#if CORESYSTEM_GOOGLE
using Google.Apis.Sheets.v4.Data;
#endif

#if UNITY_ADDRESSABLES
#endif

namespace SyadeuEditor
{
    [System.Obsolete("", true)]
    [CustomEditor(typeof(CreatureStat))]
    public sealed class CreatureStatEditor : EditorEntity<CreatureStat>
    {
        ValuePairContainer m_ExclusiveValues;
        ValuePairContainer m_ActualValues;

        private void OnEnable()
        {
            OnValidate();
        }
        private void OnValidate()
        {
            m_ExclusiveValues = GetFieldValue<ValuePairContainer>("m_ExclusiveValues");
            if (m_ExclusiveValues == null)
            {
                SetFieldValue("m_ReflectionValues", m_ExclusiveValues);
                EditorUtility.SetDirty(Asset);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            m_ActualValues = GetFieldValue<ValuePairContainer>("m_Values");
            if (m_ActualValues == null)
            {
                SetFieldValue("m_Values", m_ActualValues);
                EditorUtility.SetDirty(Asset);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            m_ExclusiveValues.DrawValueContainer("Exclusive Values");
            EditorGUI.EndDisabledGroup();
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(Asset);
            }

            EditorGUI.BeginDisabledGroup(!Application.isPlaying);
            m_ActualValues.DrawValueContainer("Actual Values", ValuePairEditor.DrawMenu.None, null);
            EditorGUI.EndDisabledGroup();

            //EditorGUILayout.Space();
            //base.OnInspectorGUI();
        }
    }
}
