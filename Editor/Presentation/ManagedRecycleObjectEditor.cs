using Syadeu.Mono;
using UnityEngine;
using UnityEditor;
using Syadeu.Presentation.Proxy;

namespace SyadeuEditor.Presentation
{
    [CustomEditor(typeof(ManagedRecycleObject))]
    public sealed class ManagedRecycleObjectEditor : Editor
    {
        private ManagedRecycleObject m_Scr;

        private SerializedProperty onCreation;
        private SerializedProperty onInitializion;
        private SerializedProperty onTermination;

        private void OnEnable()
        {
            m_Scr = target as ManagedRecycleObject;

            onCreation = serializedObject.FindProperty("onCreation");
            onInitializion = serializedObject.FindProperty("onInitializion");
            onTermination = serializedObject.FindProperty("onTermination");
        }
        public override void OnInspectorGUI()
        {
            EditorUtils.StringHeader("Recycle Object");
            EditorUtils.SectorLine();
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(onCreation, new GUIContent("인스턴스 생성 시 한번만 호출할 함수"));
            EditorGUILayout.PropertyField(onInitializion, new GUIContent("재사용을 위해 호출되었을 때"));
            EditorGUILayout.PropertyField(onTermination, new GUIContent("재사용 풀로 되돌아갈 때"));

            serializedObject.ApplyModifiedProperties();
            //base.OnInspectorGUI();
        }
    }
}
