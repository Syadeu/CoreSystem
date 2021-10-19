using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Presentation.Events;
using System.Collections;
using System.ComponentModel;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    [DisplayName("InstanceAction: Set Time Scale")]
    public sealed class SetTimeScaleAction : InstanceAction, IEventSequence
    {
        [JsonProperty(Order = 0, PropertyName = "TargetScale")]
        private float m_TargetScale = 1;

        public enum Update
        {
            Instant,
            Lerp
        }
        [JsonProperty(Order = 1, PropertyName = "UpdateType")]
        private Update m_UpdateType = Update.Instant;

        [Tooltip("UpdateType 이 Lerp 일 경우에만 적용됩니다.")]
        [JsonProperty(Order = 2, PropertyName = "TargetUpdateTime")]
        private float m_TargetUpdateTime = 0.1f;

        [Space, Header("Sequence")]
        [JsonProperty(Order = 2, PropertyName = "AfterDelay")]
        private float m_AfterDelay = 0;

        [JsonIgnore] private bool m_KeepWait = false;

        [JsonIgnore] public bool KeepWait => m_KeepWait;
        [JsonIgnore] public float AfterDelay => m_AfterDelay;

        protected override void OnExecute()
        {
            if (m_UpdateType == Update.Instant)
            {
                Time.timeScale = m_TargetScale;
                return;
            }

            m_KeepWait = true;
            UpdateJob job = new UpdateJob()
            {
                m_Caller = new Instance<SetTimeScaleAction>(Idx),
                m_TargetTimeScale = m_TargetScale,
                m_TargetUpdateTime = m_TargetUpdateTime
            };

            PresentationSystem<DefaultPresentationGroup, CoroutineSystem>.System.PostCoroutineJob(job);
        }

        private struct UpdateJob : ICoroutineJob
        {
            public Instance<SetTimeScaleAction> m_Caller;
            public float m_TargetUpdateTime, m_TargetTimeScale;

            public UpdateLoop Loop => UpdateLoop.Default;

            public void Dispose()
            {
                Time.timeScale = m_TargetTimeScale;
                m_Caller.GetObject().m_KeepWait = false;
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
