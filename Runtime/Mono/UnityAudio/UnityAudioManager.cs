
using Syadeu.Database;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if CORESYSTEM_UNITYAUDIO
namespace Syadeu.Mono.Audio
{
    public sealed class UnityAudioManager : StaticManager<UnityAudioManager>
    {
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
#if UNITY_EDITOR
        private static AudioListener m_EditorListener = null;
        public static AudioListener EditorListener
        {
            get
            {
                if (m_EditorListener == null) m_EditorListener = FindObjectOfType<AudioListener>();
                return m_EditorListener;
            }
        }
#endif

        public override void OnInitialize()
        {
            PoolContainer<UnityAudioSource>.Initialize(() =>
            {
                GameObject obj = new GameObject("AudioObject");
                obj.AddComponent<AudioSource>().playOnAwake = false;

                UnityAudioSource audioSource = obj.AddComponent<UnityAudioSource>();
                return audioSource;
            }, 10);
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
                        PoolContainer<UnityAudioSource>.Enqueue(m_UnityAudios[i]);
                        m_UnityAudios.RemoveAt(i);

                        continue;
                    }

                    if (i % 250 == 0) yield return null;
                }

                yield return null;
            }
        }

        public static float DistanceFromListener(Transform tr)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return (EditorListener.transform.position - tr.position).magnitude;
            }
            else
#endif
            {
                return (AudioListener.transform.position - tr.position).magnitude;
            }
        }

        #region Play

        internal static void Play(UnityAudioList.Content content, Vector3 position)
        {
            UnityAudioSource audio = PoolContainer<UnityAudioSource>.Dequeue();
            audio.Initialize(content);
            audio.SetPosition(position);

            Instance.m_UnityAudios.Add(audio);
            audio.Play();
        }
        internal static void Play(UnityAudioList.Content content, Transform tr)
        {
            UnityAudioSource audio = PoolContainer<UnityAudioSource>.Dequeue();
            audio.Initialize(content);
            audio.SetPosition(tr);

            Instance.m_UnityAudios.Add(audio);
            audio.Play();
        }

        #endregion

    }
}
#endif