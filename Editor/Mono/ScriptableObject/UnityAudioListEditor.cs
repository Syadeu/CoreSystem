using Syadeu.Mono.Audio;
using UnityEditor;

namespace SyadeuEditor
{
    [CustomEditor(typeof(UnityAudioList))]
    public sealed class UnityAudioListEditor : Editor
    {
        private UnityAudioList m_Scr;
        private SerializedProperty m_AudioMixerPath;
        private SerializedProperty m_AudioClipPath;

        private bool m_ShowOriginalContents = false;

        private void OnEnable()
        {
            m_Scr = target as UnityAudioList;
            m_AudioMixerPath = serializedObject.FindProperty("m_AudioMixerPath");
            m_AudioClipPath = serializedObject.FindProperty("m_AudioClipPath");
        }
        public override void OnInspectorGUI()
        {
            EditorUtils.StringHeader("Unity Audio List");
            EditorUtils.SectorLine();

            m_ShowOriginalContents = EditorUtils.Foldout(m_ShowOriginalContents, "Original Contents");
            if (m_ShowOriginalContents) base.OnInspectorGUI();
        }
    }
}
