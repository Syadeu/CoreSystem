using Newtonsoft.Json;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Actions
{
    [System.ComponentModel.DisplayName("TriggerAction: Set Animator Float")]
    public sealed class AnimatorSetFloatAction : AnimatorParameterActionBase
    {
        [JsonProperty(Order = 0, PropertyName = "Value")] private float m_Value;

        protected override void OnExecute(EntityData<IEntityData> entity)
        {
            if (!IsExecutable(entity, out AnimatorAttribute animator))
            {
                return;
            }

            animator.SetFloat(KeyHash, m_Value);
        }
    }
}
