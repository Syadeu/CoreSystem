using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

using Syadeu.Mono;
using Syadeu.Extentions.EditorUtils;

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

            if (Application.isPlaying)
            {
                Runtime();
            }
            else
            {
                Editor();
            }
            
            base.OnInspectorGUI();
        }

        void Editor()
        {

        }
        void Runtime()
        {
            var objList = ins.GetRecycleObjectList();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.IntField("현재 재사용 객체 종류 갯수: ", objList.Count);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space();

            for (int i = 0; i < objList.Count; i++)
            {
                EditorUtils.StringHeader(objList[i].Value.name, 15);
                EditorGUI.indentLevel += 1;

                var instances = ins.GetInstances(objList[i].Key);
                for (int a = 0; a < instances.Count; a++)
                {
                    EditorGUILayout.IntField($"{a}.\tIngameIndex: ", instances[a].IngameIndex);
                    EditorGUILayout.LabelField($"\tActivated: {instances[a].Activated}");
                }

                EditorGUI.indentLevel -= 1;
            }
            
        }
    }
}
