using Syadeu.Database;

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if CORESYSTEM_UNITYAUDIO
namespace Syadeu.Mono.Audio
{
    public sealed class UnityAudioManager : StaticManager<UnityAudioManager>
    {
        [SerializeField] private int m_AudioPrefabIdx = 0;

        private readonly List<UnityAudioSource> m_UnityAudios = new List<UnityAudioSource>();
        private AudioListener m_AudioListener = null;

        public static int CurrentPlaying => Instance.m_UnityAudios.Count;

        public static AudioListener AudioListener
        {
            get
            {
                if (Instance.m_AudioListener == null) Instance.m_AudioListener = FindObjectOfType<AudioListener>();
                return Instance.m_AudioListener;
            }
        }

        public override void OnStart()
        {
            StartCoroutine(Updater());
        }
        private IEnumerator Updater()
        {
            while (true)
            {
                for (int i = m_UnityAudios.Count - 1; i >= 0; i--)
                {
                    if (!m_UnityAudios[i].IsPlaying)
                    {
                        m_UnityAudios[i].Terminate();
                        m_UnityAudios.RemoveAt(i);

                        continue;
                    }

                    if (i % 250 == 0) yield return null;
                }

                yield return null;
            }
        }

        public static float DistanceFromListener(Transform tr)
            => (AudioListener.transform.position - tr.position).magnitude;

        #region Play
        private static UnityAudioSource GetAudioSource()
            => (UnityAudioSource)PrefabManager.GetRecycleObject(Instance.m_AudioPrefabIdx);

        internal static void Play(UnityAudioList.Content content, Vector3 position)
        {
            UnityAudioSource audio = GetAudioSource();
            audio.Initialize(content);
            audio.SetPosition(position);

            Instance.m_UnityAudios.Add(audio);
            audio.Play();
        }
        internal static void Play(UnityAudioList.Content content, Transform tr)
        {
            UnityAudioSource audio = GetAudioSource();
            audio.Initialize(content);
            audio.SetPosition(tr);

            Instance.m_UnityAudios.Add(audio);
            audio.Play();
        }

        #endregion

    }

    [RequireComponent(typeof(AudioSource))]
    public sealed class UnityAudioSource : RecycleableMonobehaviour, IInitialize<UnityAudioList.Content>, IValidation
    {
        [SerializeField] private AudioSource m_AudioSource;
        [SerializeField] private SimpleFollower m_SimpleFollower;

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
                return this;
            }
            return this;
        }
        public UnityAudioSource Play(int audioListIdx)
        {
            Initialize(UnityAudioList.Instance.Contents[audioListIdx]);

            if (!InternalPlay())
            {
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

            public UnityAudioSource m_BaseSetting = null;

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

        [SerializeField] private Content[] m_Contents = new Content[0];

        public Content[] Contents => m_Contents;
    }
}
#endif