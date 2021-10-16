using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Actions;
using System.ComponentModel;
using Syadeu.Collections;
using Syadeu.Presentation.Entities;
using Newtonsoft.Json;
using Syadeu.Internal;

#if CORESYSTEM_MOTIONMATCHING
namespace Syadeu.Presentation.MotionMatching
{
    [DisplayName("TriggerAction: Set RequiredTag MxMAnimator")]
    [ReflectionDescription("없으면 추가하고 있으면 제거합니다.")]
    public sealed class SetRequiredTagMxMAnimatorAction : TriggerAction
    {
        [JsonProperty(Order = 0, PropertyName = "Tags")]
        private MxM.ETags m_Tags = MxM.ETags.None;

        protected override void OnExecute(EntityData<IEntityData> entity)
        {
            AnimatorAttribute animator = entity.GetAttribute<AnimatorAttribute>();
            var anim = animator.AnimatorComponent.GetComponent<MxM.MxMAnimator>();

            if ((anim.RequiredTags & m_Tags) == m_Tags)
            {
                anim.RemoveRequiredTags(m_Tags);
            }
            else
            {
                anim.AddRequiredTags(m_Tags);
            }
        }
    }
}
#endif
