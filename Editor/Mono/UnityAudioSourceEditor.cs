using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

#if CORESYSTEM_UNITYAUDIO

using Syadeu.Mono.Audio;

namespace SyadeuEditor
{
    [CustomEditor(typeof(UnityAudioSource))]
    public sealed class UnityAudioSourceEditor : EditorEntity
    {
        private UnityAudioSource m_Scr;
        private bool m_ShowOriginalContents = false;

        private void OnEnable()
        {
            m_Scr = target as UnityAudioSource;

            AudioSource audio = m_Scr.GetComponent<AudioSource>();
            serializedObject.FindProperty("m_AudioSource").objectReferenceValue = audio;

            serializedObject.ApplyModifiedProperties();
        }

        public override void OnInspectorGUI()
        {
            EditorUtils.StringHeader("Unity Audio Source");
            EditorUtils.SectorLine();
            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.FloatField("Distance From Listener: ", Application.isPlaying ? UnityAudioManager.DistanceFromListener(m_Scr.transform) : 0);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Only can play the sound at runtime", MessageType.Info);
            }
            m_Scr.AudioClip = (AudioClip)EditorGUILayout.ObjectField("Audio Clip: ", m_Scr.AudioClip, typeof(AudioClip), false);
            EditorGUI.BeginDisabledGroup(!Application.isPlaying);
            if (GUILayout.Button("Play"))
            {
                m_Scr.Play();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();
            m_ShowOriginalContents = EditorUtils.Foldout(m_ShowOriginalContents, "Original Contents");
            if (m_ShowOriginalContents) base.OnInspectorGUI();
        }
    }

    //[CustomPropertyDrawer(typeof(UnityAudioList.Settings))]
    //public sealed class UnityAudioListSettingsDrawer : PropertyDrawer
    //{
    //    //public override VisualElement CreatePropertyGUI(SerializedProperty property)
    //    //{
    //    //    //return base.CreatePropertyGUI(property);

    //    //    VisualElement container = new VisualElement();

    //    //    var isLoop = new PropertyField(property.FindPropertyRelative("m_IsLoop"));
    //    //    var is3D = new PropertyField(property.FindPropertyRelative("m_Is3D"));

    //    //    container.Add(isLoop);
    //    //    container.Add(is3D);

    //    //    return container;
    //    //}
    //    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    //    {
    //        //base.OnGUI(position, property, label);

    //        EditorGUI.BeginProperty(position, label, property);

    //        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

    //        EditorGUI.EndProperty();
    //    }
    //}
}
#endif