// Copyright 2021 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Proxy;
using System;
using System.Collections;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Syadeu.Presentation.Actions
{
    [DisplayName("TriggerAction: Play Unity Audio")]
    public sealed class PlayUnityAudioAction : TriggerAction
    {
        [Flags]
        public enum PlayOptions
        {
            OneShot         =   0x001,
            Loop            =   0x010,

            UpdatePosition  =   0x100
        }

        [JsonProperty(Order = 0, PropertyName = "AudioClip")]
        private PrefabReference<AudioClip> m_AudioClip = PrefabReference<AudioClip>.None;
        [JsonProperty(Order = 1, PropertyName = "PlayOptions")]
        private PlayOptions m_PlayOptions = PlayOptions.OneShot;

        [Space, Header("General")]
        [JsonProperty(Order = 2, PropertyName = "Volume"), Range(0, 1)]
        private float m_Volume = 1;
        [JsonProperty(Order = 3, PropertyName = "Pitch"), Range(0, 1)]
        private float m_Pitch = 1;
        [JsonProperty(Order = 4, PropertyName = "StereoPan"), Range(-1, 1)]
        private float m_StereoPan = 0;

        [JsonIgnore] private CoroutineSystem m_CoroutineSystem = null;

        protected override void OnCreated()
        {
            m_CoroutineSystem = PresentationSystem<DefaultPresentationGroup, CoroutineSystem>.System;
        }
        protected override void OnDestroy()
        {
            m_CoroutineSystem = null;
        }

        protected override void OnExecute(Entity<IObject> entity)
        {
#if DEBUG_MODE
            if (!(entity.Target is IEntity))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"TriggerAction({nameof(PlayUnityAudioAction)}) is only can attached to IEntity. {entity.RawName} is not allowed.");

                return;
            }
#endif
            PlayJob job = new PlayJob
            {
                m_GameObject = FixedGameObject.CreateInstance(),
                m_AudioClip = m_AudioClip,
                m_Transform = (ProxyTransform)entity.ToEntity<IEntity>().transform,
                m_PlayOptions = m_PlayOptions,
                m_Loop = (m_PlayOptions & PlayOptions.UpdatePosition) == PlayOptions.UpdatePosition ? UpdateLoop.AfterTransform : UpdateLoop.Default,

                m_Volume = m_Volume
            };

            m_CoroutineSystem.StartCoroutine(job);
        }

        private struct PlayJob : ICoroutineJob
        {
            public FixedGameObject m_GameObject;
            public PrefabReference<AudioClip> m_AudioClip;
            public ProxyTransform m_Transform;
            public PlayOptions m_PlayOptions;
            public UpdateLoop m_Loop;

            public float m_Volume;

            public UpdateLoop Loop => m_Loop;

            public void Dispose()
            {
                m_GameObject.Dispose();
            }
            public IEnumerator Execute()
            {
                if (m_AudioClip.IsNone()) yield break;

                GameObject obj = m_GameObject.Target;
                AudioSource audioSource = obj.AddComponent<AudioSource>();
                obj.transform.position = m_Transform.position;
                
                if (m_AudioClip.Asset == null)
                {
                    AsyncOperationHandle handler = m_AudioClip.LoadAssetAsync();
                    yield return handler;
                }

                audioSource.clip = m_AudioClip.Asset;
                audioSource.loop = (m_PlayOptions & PlayOptions.Loop) == PlayOptions.Loop ? true : false;
                audioSource.volume = m_Volume;

                audioSource.Play();

                if ((m_PlayOptions & PlayOptions.UpdatePosition) == PlayOptions.UpdatePosition)
                {
                    Transform tr = obj.transform;
                    while (audioSource.isPlaying)
                    {
                        tr.position = m_Transform.position;

                        yield return null;
                    }
                }
                else
                {
                    AudioAwaiter awaiter = new AudioAwaiter(audioSource);
                    yield return awaiter;

                    awaiter.Dispose();
                }

                UnityEngine.Object.Destroy(m_GameObject.Target.GetComponent<AudioSource>());
            }
        }
        private sealed class AudioAwaiter : ICustomYieldAwaiter, IDisposable
        {
            private AudioSource m_AudioSource;

            public bool KeepWait => m_AudioSource.isPlaying;

            public AudioAwaiter(AudioSource audioSource)
            {
                m_AudioSource = audioSource;
            }

            public void Dispose()
            {
                m_AudioSource = null;
            }
        }
    }
}
