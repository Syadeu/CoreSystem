using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Actions
{
    [System.ComponentModel.DisplayName("TriggerAction: Set Animator Integer")]
    public sealed class AnimatorSetIntegerAction : AnimatorParameterActionBase
    {
        [JsonProperty(Order = 0, PropertyName = "Value")] private int m_Value;

        protected override void OnExecute(EntityData<IEntityData> entity)
        {
            if (!IsExecutable(entity, out AnimatorAttribute animator))
            {
                return;
            }

            animator.SetInteger(KeyHash, m_Value);
        }
    }
}
