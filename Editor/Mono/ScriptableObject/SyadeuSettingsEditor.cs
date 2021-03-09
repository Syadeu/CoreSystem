using Syadeu.Mono;

using UnityEditor;

namespace SyadeuEditor
{
    [CustomEditor(typeof(SyadeuSettings))]
    public class SyadeuSettingsEditor : Editor
    {
        bool m_EnableHelpbox = false;

        bool m_GlobalOption = true;

        bool m_ShowOriginalContents = false;

        public override void OnInspectorGUI()
        {
            EditorUtils.StringHeader("CoreSystem Setting");
            EditorUtils.SectorLine();

            m_EnableHelpbox = EditorGUILayout.ToggleLeft("도움말 표시", m_EnableHelpbox);
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();

            m_GlobalOption = EditorUtils.Foldout(m_GlobalOption, "Global Option", 15);
            if (m_GlobalOption) GlobalSettings();
            EditorUtils.SectorLine();

            if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(SyadeuSettings.Instance);

            EditorGUILayout.Space();
            m_ShowOriginalContents = EditorUtils.Foldout(m_ShowOriginalContents, "Original Contents");
            if (m_ShowOriginalContents) base.OnInspectorGUI();
        }

        private void GlobalSettings()
        {
            if (m_EnableHelpbox)
            {
                EditorGUILayout.HelpBox(
                    "활성화시, StaticManager, MonoManager 의 HideInHierarchy 설정을 무시하고, " +
                    "전부 강제로 Hierarchy에 표시시킵니다.", MessageType.Info);
            }

            SyadeuSettings.Instance.m_VisualizeObjects =
                EditorGUILayout.ToggleLeft("전부 Hierarchy에 표시", SyadeuSettings.Instance.m_VisualizeObjects);

            if (m_EnableHelpbox)
            {
                EditorGUILayout.HelpBox(
                    "게임내 Exception이 raise 된 후, 게임을 강제로 크래쉬 시킵니다. " +
                    "에디터에서는 작동하지 않고, 빌드에서만 작동합니다.", MessageType.Info);
            }

            SyadeuSettings.Instance.m_CrashAfterException =
                EditorGUILayout.ToggleLeft("에러 발생 후 강제 크래쉬", SyadeuSettings.Instance.m_CrashAfterException);
        }
    }
}
