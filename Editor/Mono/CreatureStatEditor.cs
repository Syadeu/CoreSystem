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
    [CustomEditor(typeof(CreatureStat))]
    public sealed class CreatureStatEditor : EditorEntity<CreatureStat>
    {
        ValuePairContainer m_ReflectionValues;
        ValuePairContainer m_ActualValues;

        private void OnEnable()
        {
            OnValidate();
        }
        private void OnValidate()
        {
            m_ReflectionValues = GetValue<ValuePairContainer>("m_ReflectionValues");
            if (m_ReflectionValues == null)
            {
                SetValue("m_ReflectionValues", m_ReflectionValues);
                EditorUtility.SetDirty(Asset);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            m_ActualValues = GetValue<ValuePairContainer>("m_Values");
            if (m_ActualValues == null)
            {
                SetValue("m_Values", m_ActualValues);
                EditorUtility.SetDirty(Asset);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            m_ReflectionValues.DrawValueContainer("Reflection Values", ValuePairEditor.DrawMenu.String, null);
            EditorGUI.EndDisabledGroup();
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(Asset);
            }

            EditorGUI.BeginDisabledGroup(true);
            m_ActualValues.DrawValueContainer("Actual Values", ValuePairEditor.DrawMenu.None, null);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();
            base.OnInspectorGUI();
        }
    }
}
