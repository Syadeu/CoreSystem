using Newtonsoft.Json;
using Syadeu.Internal;
using Syadeu.Presentation.Entities;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    [ReflectionDescription("Unity Animator 전용입니다")]
    public sealed class AnimationTriggerKeyAction : ActionBase<AnimationTriggerKeyAction>
    {
        [JsonProperty(Order = 0, PropertyName = "TriggerKey")] private string m_TriggerKey = string.Empty;

        [JsonIgnore] private int m_KeyHash;
        
        protected override void OnInitialize()
        {
            m_KeyHash = Animator.StringToHash(m_TriggerKey);
        }
        protected override void OnExecute(EntityData<IEntityData> entity)
        {
            if (!(entity.Target is EntityBase entitybase))
            {
                CoreSystem.Logger.LogError(Channel.Presentation,
                    "Target is not a EntityBase");
                return;
            }
            
            if (entitybase.transform is IUnityTransform unityTr)
            {
                SetTrigger(unityTr.provider);
            }
            else if (entitybase.transform is ProxyTransform proxyTr)
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
}
