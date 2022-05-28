using Syadeu.Mono;
using UnityEngine;
using UnityEditor;
using Syadeu.Presentation.Proxy;
using Syadeu.Presentation;
using SyadeuEditor.Utilities;
using Syadeu.Collections.Editor;
using Syadeu.Collections.PropertyDrawers;

namespace SyadeuEditor.Presentation
{
    [CustomEditor(typeof(ManagedRecycleObject))]
    public sealed class ManagedRecycleObjectEditor : InspectorEditor<ManagedRecycleObject>
    {
        private SerializedProperty onCreation;
        private SerializedProperty onInitializion;
        private SerializedProperty onTermination;

        private void OnEnable()
        {
            onCreation = serializedObject.FindProperty("onCreation");
            onInitializion = serializedObject.FindProperty("onInitializion");
            onTermination = serializedObject.FindProperty("onTermination");
        }
        protected override void OnInspectorGUIContents()
        {
            CoreGUI.Label("Recycle Object", 20);
            CoreGUI.SectorLine();
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(onCreation, new GUIContent("인스턴스 생성 시 한번만 호출할 함수"));
            EditorGUILayout.PropertyField(onInitializion, new GUIContent("재사용을 위해 호출되었을 때"));
            EditorGUILayout.PropertyField(onTermination, new GUIContent("재사용 풀로 되돌아갈 때"));

            serializedObject.ApplyModifiedProperties();
            //base.OnInspectorGUI();

            if (!Application.isPlaying) return;

            CoreGUI.Line();

            if (!target.entity.IsValid())
            {
                CoreGUI.Label("Invalid Entity", 13, TextAnchor.MiddleCenter);
                return;
            }

            var property = SerializedObject<ObjectBase>.GetSharedObject((ObjectBase)target.entity.Target);
            EditorGUILayout.PropertyField(property);
            //var drawer = ObjectBaseDrawer.GetDrawer((ObjectBase)Target.entity.Target);
            //drawer.OnGUI();
        }

        private void OnSceneGUI()
        {
            if (!Application.isPlaying) return;
            else if (!target.entity.IsValid()) return;

            Vector2 guiPos = HandleUtility.WorldToGUIPoint(target.transform.position);
            Handles.BeginGUI();

            Rect rect = new Rect(guiPos, new Vector2(180, 60));

            using (new GUI.GroupScope(rect, target.entity.Name, EditorStyleUtilities.Box))
            {
                EditorGUILayout.LabelField("test");
            }

            Handles.EndGUI();
        }
    }
}
