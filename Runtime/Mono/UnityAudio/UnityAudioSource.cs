using Syadeu.Database;

using UnityEngine;

#if CORESYSTEM_UNITYAUDIO
namespace Syadeu.Mono.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class UnityAudioSource : RecycleableMonobehaviour, IInitialize<UnityAudioList.Content>, IValidation
    {
        [SerializeField] private AudioSource m_AudioSource;
        [SerializeField] private SimpleFollower m_SimpleFollower;
        [SerializeField] private int m_PlayType = 0;

        private bool m_Initialized = false;
        private UnityAudioList.Content m_Content = null;

        public override string DisplayName => AudioClip == null ? base.DisplayName : AudioClip.name;

        private AudioSource AudioSource
        {
            get
            {
                if (m_AudioSource == null) m_AudioSource = GetComponent<AudioSource>();
                return m_AudioSource;
            }
        }

        public AudioClip AudioClip { get => AudioSource.clip; set => AudioSource.clip = value; }
        public float Volume { get => AudioSource.volume; set => AudioSource.volume = value; }
        public float Pitch { get => AudioSource.pitch; set => AudioSource.pitch = value; }
        public bool IsPlaying => AudioSource.isPlaying;

        public void Initialize(UnityAudioList.Content t)
        {
            m_Content = t;

            if (!m_Content.m_Settings.m_Is3D) AudioSource.spatialBlend = 0;
            AudioSource.loop = m_Content.m_Settings.m_IsLoop;

            AudioClip = m_Content.m_AudioClip;
            Volume = m_Content.m_Settings.Volume;
            Pitch = m_Content.m_Settings.Pitch;

            m_Initialized = true;
        }
        protected override void OnTerminate()
        {
            m_Content = null;
            m_Initialized = false;
        }
        public bool IsValid() => !m_Initialized;

        #region Functions
        public UnityAudioSource SetPosition(Transform tr)
        {
            if (transform.parent != null) transform.SetParent(null);

            if (m_SimpleFollower == null)
            {
                m_SimpleFollower = GetComponent<SimpleFollower>();
                if (m_SimpleFollower == null) m_SimpleFollower = gameObject.AddComponent<SimpleFollower>();
            }
            else m_SimpleFollower.enabled = true;

            m_SimpleFollower.SetTarget(tr);
            m_PlayType = 1;
            return this;
        }
        public UnityAudioSource SetPosition(Vector3 worldPosition)
        {
            if (transform.parent != null) transform.SetParent(null);

            transform.position = worldPosition;
            return this;
        }
        #endregion

        public UnityAudioSource Play()
        {
            if (!InternalPlay())
            {
                $"AudioSource({name}) is not initialized".ToLogError();
                return this;
            }
            return this;
        }
        public UnityAudioSource Play(int audioListIdx)
        {
            Initialize(UnityAudioList.Instance.Contents[audioListIdx]);

            if (!InternalPlay())
            {
                $"AudioSource({name}) is not initialized".ToLogError();
                return this;
            }
            return this;
        }

        private bool InternalPlay()
        {
            if (!IsValid())
            {
                return false;
            }
            if (AudioClip == null) return false;

            AudioSource.Play();

            return true;
        }
    }
}
#endif