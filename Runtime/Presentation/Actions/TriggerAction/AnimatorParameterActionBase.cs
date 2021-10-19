using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    public abstract class AnimatorParameterActionBase : TriggerAction
    {
        [JsonProperty(Order = -1, PropertyName = "TriggerKey")] protected string m_TriggerKey = string.Empty;

        [JsonIgnore] private int m_KeyHash;
        [JsonIgnore] protected int KeyHash => m_KeyHash;

        protected override void OnCreated()
        {
            m_KeyHash = Animator.StringToHash(m_TriggerKey);
        }
        protected bool IsExecutable(EntityData<IEntityData> entity, out AnimatorAttribute attribute)
        {
            attribute = entity.GetAttribute<AnimatorAttribute>();
            if (attribute == null)
            {
                CoreSystem.Logger.LogError(Channel.Presentation,
                    $"Target does not have {nameof(AnimatorAttribute)}");
                return false;
            }

            return true;
        }
    }
}
