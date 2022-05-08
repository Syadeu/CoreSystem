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
using System.Collections;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    [DisplayName("System/Set Time Scale")]
    [Guid("B77B77A7-5B87-4672-B648-9F32CFCBC477")]
    internal sealed class SetTimeScaleConstAction : ConstAction<int>
    {
        [JsonProperty(Order = 0, PropertyName = "TargetScale")]
        private float m_TargetScale = 1;

        public enum Update
        {
            Instant,
            Lerp
        }
        [UnityEngine.SerializeField, JsonProperty(Order = 1, PropertyName = "UpdateType")]
        private Update m_UpdateType = Update.Instant;

        [Tooltip("UpdateType 이 Lerp 일 경우에만 적용됩니다.")]
        [UnityEngine.SerializeField, JsonProperty(Order = 2, PropertyName = "TargetUpdateTime")]
        private float m_TargetUpdateTime = 0.1f;

        [JsonIgnore] private bool m_KeepWait = false;

        protected override int Execute()
        {
            if (m_UpdateType == Update.Instant)
            {
                Time.timeScale = m_TargetScale;
                return 0;
            }

            m_KeepWait = true;
            UpdateJob job = new UpdateJob()
            {
                m_TargetTimeScale = m_TargetScale,
                m_TargetUpdateTime = m_TargetUpdateTime
            };

            PresentationSystem<DefaultPresentationGroup, CoroutineSystem>.System.StartCoroutine(job);

            return 0;
        }

        private struct UpdateJob : ICoroutineJob
        {
            public float m_TargetUpdateTime, m_TargetTimeScale;

            public UpdateLoop Loop => UpdateLoop.Default;

            public void Dispose()
            {
                Time.timeScale = m_TargetTimeScale;
            }
            public IEnumerator Execute()
            {
                float startTime = Time.realtimeSinceStartup;
                float progress = 0;
                while (progress < m_TargetTimeScale)
                {
                    float delta = Time.realtimeSinceStartup - startTime;

                    progress += delta;
                    if (progress > m_TargetUpdateTime) break;

                    float target = progress / m_TargetUpdateTime;

                    Time.timeScale = math.lerp(Time.timeScale, m_TargetTimeScale, target);

                    yield return null;
                }
            }
        }
    }
}
