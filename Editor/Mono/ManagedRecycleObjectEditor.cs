using Syadeu.Mono;
using UnityEngine;
using UnityEditor;

namespace SyadeuEditor
{
    [CustomEditor(typeof(ManagedRecycleObject))]
    public sealed class ManagedRecycleObjectEditor : Editor
    {
        private ManagedRecycleObject m_Scr;

        private SerializedProperty onCreated;
        private SerializedProperty onInitialize;
        private SerializedProperty onTerminate;

        private void OnEnable()
        {
            m_Scr = target as ManagedRecycleObject;

            onCreated = serializedObject.FindProperty("onCreated");
            onInitialize = serializedObject.FindProperty("onInitialize");
            onTerminate = serializedObject.FindProperty("onTerminate");
        }
        public override void OnInspectorGUI()
        {
            EditorUtils.StringHeader("Recycle Object");
            EditorUtils.SectorLine();
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(onCreated, new GUIContent("인스턴스 생성 시 한번만 호출할 함수"));
            EditorGUILayout.PropertyField(onInitialize, new GUIContent("재사용을 위해 호출되었을 때"));
            EditorGUILayout.PropertyField(onTerminate, new GUIContent("재사용 풀로 되돌아갈 때"));

            serializedObject.ApplyModifiedProperties();
            //base.OnInspectorGUI();
        }
    }
}
