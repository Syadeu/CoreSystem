using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

using Syadeu.Mono;
using Syadeu.Extentions.EditorUtils;
using DG.DOTweenEditor.UI;

namespace Syadeu
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
            EditorUtils.StringHeader("\t����", 15);
            EditorGUILayout.Space();

            if (GUILayout.Button("������ ����Ʈ ����"))
            {
                Selection.activeObject = PrefabList.Instance;
            }

            EditorGUILayout.BeginHorizontal();
            SyadeuSettings.Instance.m_PMErrorAutoFix = EditorGUILayout.ToggleLeft("�ڵ� ���� �ذ�", SyadeuSettings.Instance.m_PMErrorAutoFix);
            EditorGUILayout.HelpBox("Ȱ��ȭ��, ��Ÿ�� ���� ���� ��ü�� �ı��Ǿ��ų� �ϴ� ���� ���ܻ��� ��ġ�� �ڵ����� �ذ��մϴ�.", MessageType.Info);
            EditorGUILayout.EndHorizontal();
        }
        bool[] openInstances = new bool[0];
        bool[] sortInstances = new bool[0];
        void Runtime()
        {
            var objList = ins.GetRecycleObjectList();
            if (openInstances.Length != objList.Count) openInstances = new bool[objList.Count];
            if (sortInstances.Length != objList.Count) sortInstances = new bool[objList.Count];

            EditorUtils.StringHeader("\t��Ÿ��", 15);

            if (GUILayout.Button("�̻�� ��ü ������"))
            {
                int count = PrefabManager.ReleaseTerminatedObjects();
                $"CoreSystem.RecycleObject :: {count}���� ������� �ʴ� ���� ��ü�� �� ������ ���ŵ˴ϴ�".ToLog();
            }

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.IntField("���� ���� ��ü ���� ����: ", objList.Count);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space();

            for (int i = 0; i < objList.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorUtils.StringHeader($"> {objList[i].Value.name}", 15);
                openInstances[i] = EditorGUILayout.ToggleLeft(openInstances[i] ? "����" : "����", openInstances[i]);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"\t���� �ν��Ͻ� ����: {ins.GetInstanceCount(objList[i].Key)}");

                string maxInstanceCount;
                if (PrefabList.Instance.m_ObjectSettings[objList[i].Key].MaxInstanceCount < 0)
                {
                    maxInstanceCount = "����";
                }
                else
                {
                    maxInstanceCount = PrefabList.Instance.m_ObjectSettings[objList[i].Key].MaxInstanceCount.ToString();
                }
                EditorGUILayout.LabelField($"�ִ� �ν��Ͻ� ����: {maxInstanceCount}");
                EditorGUILayout.EndHorizontal();

                if (openInstances[i])
                {
                    var instances = ins.GetInstances(objList[i].Key);

                    EditorGUILayout.BeginHorizontal();
                    sortInstances[i] = EditorGUILayout.Toggle("\t��� ���� �ν��Ͻ���", sortInstances[i]);
                    int sum = 0;
                    for (int a = 0; a < instances.Count; a++)
                    {
                        if (instances[a].Activated) sum += 1;
                    }
                    EditorGUILayout.LabelField($"\tȰ��ȭ�� �ν��Ͻ�: {sum}");
                    EditorGUILayout.EndHorizontal();
                    
                    for (int a = 0; a < instances.Count; a++)
                    {
                        if (sortInstances[i] && !instances[a].Activated) continue;

                        if (GUILayout.Button($"> {a}.\t{instances[a].DisplayName}: {instances[a].Activated}", "TextField"))
                        {
                            EditorGUIUtility.PingObject(instances[a]);
                        }
                    }
                }

                EditorUtils.SectorLine();
                //end
            }
            
        }
    }
}
