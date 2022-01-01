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
    public sealed class SetRequiredTagMxMAnimatorAction : TriggerAction
    {
        [JsonProperty(Order = 0, PropertyName = "Tags")]
        private MxM.ETags m_Tags = MxM.ETags.None;

        protected override void OnExecute(Entity<IObject> entity)
        {
            AnimatorAttribute animator = entity.GetAttribute<AnimatorAttribute>();
            var anim = animator.AnimatorComponent.GetComponent<MxM.MxMAnimator>();

            //if ((anim.RequiredTags & m_Tags) == m_Tags)
            //{
            //    anim.RemoveRequiredTags(m_Tags);
            //}
            //else
            anim.ClearRequiredTags();
            {
                anim.AddRequiredTags(m_Tags);
            }
        }
    }
}
#endif
