using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Syadeu.Mono;

#if CORESYSTEM_UNITYAUDIO

using Syadeu.Mono.Audio;

namespace SyadeuEditor
{
    [CustomEditor(typeof(UnityAudioSource))]
    public sealed class UnityAudioSourceEditor : EditorEntity<UnityAudioSource>
    {
        private AudioSource m_AudioSource;
        private SerializedProperty m_SimpleFollower;
        private SerializedProperty m_SelectedPlayType;

        private bool m_ShowOriginalContents = false;

        private static string[] m_PlayTypeString = new string[] { "OneShot", "Tracked" };

        private void OnEnable()
        {
            m_AudioSource = Asset.GetComponentInChildren<AudioSource>();
            if (m_AudioSource == null)
            {
                GameObject folder = new GameObject("AudioSource");
                folder.transform.SetParent(Asset.transform);
                folder.transform.localPosition = Vector3.zero;

                m_AudioSource = folder.AddComponent<AudioSource>();
            }

            serializedObject.FindProperty("m_AudioSource").objectReferenceValue = m_AudioSource;
            m_SimpleFollower = serializedObject.FindProperty("m_SimpleFollower");
            m_SelectedPlayType = serializedObject.FindProperty("m_PlayType");

            serializedObject.ApplyModifiedProperties();
        }

        public override void OnInspectorGUI()
        {
            EditorUtils.StringHeader("Unity Audio Source");
            EditorUtils.SectorLine();
            EditorGUILayout.Space();

            if (UnityAudioManager.EditorListener == null)
            {
                EditorGUILayout.HelpBox("!! Audio Listener Not Found !!", MessageType.Error);
                return;
            }

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.FloatField("Distance From Listener: ", UnityAudioManager.DistanceFromListener(Asset.transform));
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space();

            m_AudioSource.playOnAwake = EditorGUILayout.Toggle("Play On Awake: ", m_AudioSource.playOnAwake);

            EditorGUI.BeginChangeCheck();
            m_SelectedPlayType.intValue = EditorGUILayout.Popup("Play Type: ", m_SelectedPlayType.intValue, m_PlayTypeString);
            if (EditorGUI.EndChangeCheck())
            {
                SimpleFollower follower = Asset.GetComponent<SimpleFollower>();
                if (m_SelectedPlayType.intValue == 0)
                {
                    if (follower != null) DestroyImmediate(follower);
                }
                else if (m_SelectedPlayType.intValue == 1)
                {
                    if (follower == null) follower = Asset.gameObject.AddComponent<SimpleFollower>();
                    m_SimpleFollower.objectReferenceValue = follower;
                }
            }
            if (m_SelectedPlayType.intValue == 1)
            {
                SimpleFollower follower = (SimpleFollower)m_SimpleFollower.objectReferenceValue;
                follower.SetTarget(
                    (Transform)EditorGUILayout.ObjectField("Track Target: ", follower.GetTarget(), typeof(Transform), true)
                    );
            }

            EditorGUILayout.Space();

            Asset.AudioClip = (AudioClip)EditorGUILayout.ObjectField("Audio Clip: ", Asset.AudioClip, typeof(AudioClip), false);
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Only can play the sound at runtime", MessageType.Info);
            }
            EditorGUI.BeginDisabledGroup(!Application.isPlaying);
            if (GUILayout.Button("Play"))
            {
                Asset.Play();
            }
            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();
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