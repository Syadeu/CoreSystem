using Newtonsoft.Json;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.Entities
{
    public sealed class EntityAnimationTriggerAction : ActionBase<EntityAnimationTriggerAction>
    {
        [JsonProperty(Order = 0, PropertyName = "TriggerKey")] private string m_TriggerKey = string.Empty;

        [JsonIgnore] private int m_KeyHash;
        
        protected override void OnInitialize()
        {
            m_KeyHash = Animator.StringToHash(m_TriggerKey);
        }
        protected override void OnExecute()
        {
            if (!(Parent.Target is EntityBase entity))
            {
                CoreSystem.Logger.LogError(Channel.Presentation,
                    "Target is not a EntityBase");
                return;
            }
            
            if (entity.transform is IUnityTransform unityTr)
            {
                SetTrigger(unityTr.provider);
            }
            else if (entity.transform is ProxyTransform proxyTr)
            {
                if (proxyTr.hasProxy && !proxyTr.hasProxyQueued)
                {
                    SetTrigger(proxyTr.proxy);
                }
            }
            else
            {
                CoreSystem.Logger.LogError(Channel.Presentation,
                    "Unhandled type");
            }
        }

        private void SetTrigger(Component component)
        {
            component.GetComponentInChildren<Animator>().SetTrigger(m_KeyHash);
        }
    }
    public sealed class EntityMoveToVectorAction : ActionBase<EntityMoveToVectorAction>
    {
        public enum UpdateType
        {
            Instant =   0,
            Lerp    =   1
        }

        [JsonProperty(Order = 0, PropertyName = "Target")] private float3 m_Target;
        [JsonProperty(Order = 1, PropertyName = "UpdateType")] private UpdateType m_UpdateType;
        [JsonProperty(Order = 2, PropertyName = "Speed")] private float m_Speed = 5;

        protected override void OnExecute()
        {
            if (!(Parent.Target is EntityBase entity))
            {
                CoreSystem.Logger.LogError(Channel.Presentation,
                    "Target is not a EntityBase");
                return;
            }

            switch (m_UpdateType)
            {
                case UpdateType.Lerp:
                    CoreSystem.StartUnityUpdate(this, Lerp(entity.transform, m_Target, m_Speed));
                    break;
                default:
                    entity.transform.position = m_Target;
                    break;
            }
        }

        private static IEnumerator Lerp(ITransform tr, float3 pos, float speed)
        {
            while (sqr(tr.position - pos) > .1f)
            {
                tr.position = math.lerp(tr.position, pos, Time.deltaTime * speed);
                yield return null;
            }

            tr.position = pos;
        }

        private static float sqr(float3 translation)
        {
            return math.mul(translation, translation);
        }
    }
}
