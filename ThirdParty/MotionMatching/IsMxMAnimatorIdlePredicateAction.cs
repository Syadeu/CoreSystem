using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Actions;
using System.ComponentModel;
using Syadeu.Collections;
using Syadeu.Presentation.Entities;
using Newtonsoft.Json;

#if CORESYSTEM_MOTIONMATCHING
namespace Syadeu.Presentation.MotionMatching
{
    [DisplayName("PredicateAction: Is MxMAnimator Idle ?")]
    public sealed class IsMxMAnimatorIdlePredicateAction : TriggerPredicateAction
    {
        [JsonProperty(Order = 0, PropertyName = "DesireValue")]
        private bool m_DesireValue = false;

        protected override bool OnExecute(EntityData<IEntityData> entity)
        {
            AnimatorAttribute animator = entity.GetAttribute<AnimatorAttribute>();
            if (animator.AnimatorComponent == null) return true;

            var anim = animator.AnimatorComponent.GetComponent<MxM.MxMAnimator>();
            
            return anim.IsIdle == m_DesireValue;
        }
    }
}
#endif
