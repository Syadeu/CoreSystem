using Syadeu.Mono;

using UnityEngine;
using UnityEditor;
using Syadeu;

namespace SyadeuEditor
{
    [CustomEditor(typeof(PrefabManager))]
    public class PrefabManagerEditor : Editor
    {
        PrefabManager ins;

        private void OnEnable()
        {
            ins = target as PrefabManager;
        }

        public override void OnInspectorGUI()
        {
            EditorUtils.StringHeader("Prefab Manager");
            EditorUtils.SectorLine();

            EditorGUI.BeginChangeCheck();
            Editor();
            if (EditorGUI.EndChangeCheck() && !Application.isPlaying)
            {
                EditorUtility.SetDirty(SyadeuSettings.Instance);
            }

            if (Application.isPlaying)
            {
                EditorUtils.SectorLine();
                Runtime();
            }
            
            //base.OnInspectorGUI();
        }

        void Editor()
        {
            EditorUtils.StringHeader("\t에디터", 15);
            EditorGUILayout.Space();

            if (GUILayout.Button("프리팹 리스트"))
            {
                Selection.activeObject = PrefabList.Instance;
            }

            EditorGUILayout.BeginHorizontal();
            SyadeuSettings.Instance.m_PMErrorAutoFix = EditorGUILayout.ToggleLeft("자동 에러 해결", SyadeuSettings.Instance.m_PMErrorAutoFix);
            EditorGUILayout.HelpBox("활성화시, 런타임 도중 재사용 객체가 파괴되었거나 하는 등의 예외사항 조치를 자동으로 해결합니다.", MessageType.Info);
            EditorGUILayout.EndHorizontal();
        }
        bool[] openInstances = new bool[0];
        bool[] sortInstances = new bool[0];
        void Runtime()
        {
            var objList = ins.GetRecycleObjectList();
            if (openInstances.Length != objList.Count) openInstances = new bool[objList.Count];
            if (sortInstances.Length != objList.Count) sortInstances = new bool[objList.Count];

            EditorUtils.StringHeader("\t런타임", 15);

            if (GUILayout.Button("미사용 객체 릴리즈"))
            {
                int count = PrefabManager.ReleaseTerminatedObjects();
                $"CoreSystem.RecycleObject :: {count}개의 사용하지 않는 재사용 객체가 곧 영구히 제거됩니다".ToLog();
            }

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.IntField("현재 재사용 객체 종류 갯수: ", objList.Count);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space();

            for (int i = 0; i < objList.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorUtils.StringHeader($"> {objList[i].Value.name}", 15);
                openInstances[i] = EditorGUILayout.ToggleLeft(openInstances[i] ? "접기" : "열기", openInstances[i]);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"\t현재 인스턴스 갯수: {ins.GetInstanceCount(objList[i].Key)}");

                string maxInstanceCount;
                if (PrefabList.Instance.m_ObjectSettings[objList[i].Key].MaxInstanceCount < 0)
                {
                    maxInstanceCount = "무한";
                }
                else
                {
                    maxInstanceCount = PrefabList.Instance.m_ObjectSettings[objList[i].Key].MaxInstanceCount.ToString();
                }
                EditorGUILayout.LabelField($"최대 인스턴스 갯수: {maxInstanceCount}");
                EditorGUILayout.EndHorizontal();

                if (openInstances[i])
                {
                    var instances = ins.GetInstances(objList[i].Key);

                    EditorGUILayout.BeginHorizontal();
                    sortInstances[i] = EditorGUILayout.Toggle("\t사용 중인 인스턴스만", sortInstances[i]);
                    int sum = 0;
                    for (int a = 0; a < instances.Count; a++)
                    {
                        if (instances[a].Activated) sum += 1;
                    }
                    EditorGUILayout.LabelField($"\t활성화된 인스턴스: {sum}");
                    EditorGUILayout.EndHorizontal();

                    EditorGUI.indentLevel += 1;
                    for (int a = 0; a < instances.Count; a++)
                    {
                        if (sortInstances[i] && !instances[a].Activated) continue;

                        string rightText = null;
                        if (instances[a].GetComponent<RenderController>() != null)
                        {
                            RenderController render = instances[a].GetComponent<RenderController>();
                            rightText = $": {RenderControllerEditor.DrawStatus(render)}";
                            EditorGUILayout.BeginHorizontal();
                        }
                        if (EditorUtils.Button($"> {a}.\t{instances[a].DisplayName}: {instances[a].Activated}", "TextField", 1))
                        {
                            EditorGUIUtility.PingObject(instances[a]);
                        }
                        if (!string.IsNullOrEmpty(rightText))
                        {
                            EditorUtils.StringRich(rightText);
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    EditorGUI.indentLevel -= 1;
                }

                EditorUtils.SectorLine();
                //end
            }
            
        }
    }
}
