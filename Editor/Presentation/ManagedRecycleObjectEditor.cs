using Syadeu.Mono;
using UnityEngine;
using UnityEditor;
using Syadeu.Presentation.Proxy;
using Syadeu.Presentation;
using SyadeuEditor.Utilities;

namespace SyadeuEditor.Presentation
{
    [CustomEditor(typeof(ManagedRecycleObject))]
    public sealed class ManagedRecycleObjectEditor : EditorEntity<ManagedRecycleObject>
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
        public override void OnInspectorGUI()
        {
            EditorUtilities.StringHeader("Recycle Object");
            EditorUtilities.SectorLine();
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(onCreation, new GUIContent("인스턴스 생성 시 한번만 호출할 함수"));
            EditorGUILayout.PropertyField(onInitializion, new GUIContent("재사용을 위해 호출되었을 때"));
            EditorGUILayout.PropertyField(onTermination, new GUIContent("재사용 풀로 되돌아갈 때"));

            serializedObject.ApplyModifiedProperties();
            //base.OnInspectorGUI();

            if (!Application.isPlaying) return;

            EditorUtilities.Line();

            if (!Target.entity.IsValid())
            {
                EditorUtilities.StringRich("Invalid Entity", 13, true);
                return;
            }

            var drawer = ObjectBaseDrawer.GetDrawer((ObjectBase)Target.entity.Target);
            drawer.OnGUI();
        }

        private void OnSceneGUI()
        {
            if (!Application.isPlaying) return;
            else if (!Target.entity.IsValid()) return;

            Vector2 guiPos = HandleUtility.WorldToGUIPoint(Target.transform.position);
            Handles.BeginGUI();

            Rect rect = new Rect(guiPos, new Vector2(180, 60));

            using (new GUI.GroupScope(rect, Target.entity.Name, EditorStyleUtilities.Box))
            {
                EditorGUILayout.LabelField("test");
            }

            Handles.EndGUI();
        }
    }
}
