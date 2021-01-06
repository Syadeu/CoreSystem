using Syadeu;
using Syadeu.FMOD;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor
{
    [CustomEditor(typeof(CoreSystem))]
    public class CoreSystemEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorUtils.StringHeader("CoreSystem");
            EditorUtils.SectorLine();


            if (Application.isPlaying)
            {
                Runtime();
                FMOD();
            }

        }

        bool m_OpenManagerList = false;
        void Runtime()
        {
            EditorUtils.StringHeader("Generals", 15);
            EditorGUI.indentLevel += 1;

            m_OpenManagerList = EditorGUILayout.Foldout(m_OpenManagerList, $"현재 생성된 파괴불가 매니저: {CoreSystem.Managers.Count}개");
            if (m_OpenManagerList)
            {
                EditorGUI.indentLevel += 1;

                for (int i = 0; i < CoreSystem.Managers.Count; i++)
                {
                    EditorGUILayout.LabelField($"> {CoreSystem.Managers[i].GetType().Name}");
                }

                EditorGUI.indentLevel -= 1;
            }

            EditorGUI.indentLevel -= 1;
            EditorUtils.SectorLine();

            EditorUtils.StringHeader("Jobs", 15);
            EditorGUI.indentLevel += 1;

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.IntField($"생성된 백그라운드 워커", CoreSystem.Instance.GetBackgroundJobWorkerCount());
            EditorGUILayout.IntField($"가동중인 백그라운드 워커", CoreSystem.Instance.GetCurrentRunningBackgroundWorkerCount());
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("등록된 잡 갯수");
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.IntField("Background", CoreSystem.Instance.GetBackgroundJobCount());
            EditorGUILayout.IntField("Foreground", CoreSystem.Instance.GetForegroundJobCount());
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel -= 1;
        }

        void FMOD()
        {
            EditorUtils.StringHeader("FMOD", 15);
            EditorGUI.indentLevel += 1;

            if (FMODSystem.Initialized == false)
            {
                EditorGUILayout.LabelField("현재 표시할 정보가 없음");
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(true);

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel -= 1;
        }
    }
}
