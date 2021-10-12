using Newtonsoft.Json;
using Syadeu.Internal;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Proxy;
using System.ComponentModel;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    [DisplayName("TriggerAction: Set Animator Trigger")]
    [ReflectionDescription("Unity Animator 전용입니다")]
    public sealed class AnimationTriggerKeyAction : AnimatorParameterActionBase
    {
        protected override void OnExecute(EntityData<IEntityData> entity)
        {
            if (!IsExecutable(entity, out AnimatorAttribute animator))
            {
                return;
            }

            animator.SetTrigger(KeyHash);
        }
    }
}
