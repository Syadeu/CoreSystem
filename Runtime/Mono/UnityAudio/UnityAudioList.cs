
using UnityEngine;
using UnityEngine.Audio;

#if CORESYSTEM_UNITYAUDIO
namespace Syadeu.Mono.Audio
{
    [CustomStaticSetting("Audio")]
    public sealed class UnityAudioList : StaticSettingEntity<UnityAudioList>
    {
        [System.Serializable]
        public class Content
        {
            [SerializeField] private string m_Name;
            public AudioClip m_AudioClip;

            [Space]
            public Settings m_Settings;

            public void Play(Vector3 position) => UnityAudioManager.Play(this, position);
            public void Play(Transform tr) => UnityAudioManager.Play(this, tr);
        }
        [System.Serializable]
        public class Settings
        {
            public bool m_IsLoop = false;
            public bool m_Is3D = true;

            [Space]
            public UnityAudioSource m_BaseSetting = null;
            public AudioMixerGroup m_AudioGroup = null;

            [Space]
            [SerializeField] [Range(0, 1)] private float m_VolumeMin = 1;
            [SerializeField] [Range(0, 1)] private float m_VolumeMax = 1;

            [Space]
            [SerializeField] [Range(-3, 3)] private float m_PitchMin = 1;
            [SerializeField] [Range(-3, 3)] private float m_PitchMax = 1;

            public float Volume
            {
                get
                {
                    if (m_VolumeMin == m_VolumeMax) return m_VolumeMin;
                    return Random.Range(m_VolumeMin, m_VolumeMax);
                }
            }
            public float Pitch
            {
                get
                {
                    if (m_PitchMin == m_PitchMax) return m_PitchMin;
                    return Random.Range(m_PitchMin, m_PitchMax);
                }
            }
        }

        [SerializeField] private string m_AudioMixerPath = "Resources/Audio/Mixers";
        [SerializeField] private string m_AudioClipPath = "Resources/Audio/Clips";

        [Space]
        [SerializeField] private Content[] m_Contents = new Content[0];

        public Content[] Contents => m_Contents;
    }
}
#endif