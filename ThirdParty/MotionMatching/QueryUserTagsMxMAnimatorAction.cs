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
    [DisplayName("PredicateAction: QueryUserTags MxMAnimator")]
    [Description("현재 해당 유저 태그가 있는 애니메이션을 재생 중이면 True 를 반환합니다.")]
    public sealed class QueryUserTagsMxMAnimatorAction : TriggerPredicateAction
    {
        [JsonProperty(Order = 0, PropertyName = "Tags")]
        private MxM.EUserTags m_Tags = MxM.EUserTags.None;

        protected override bool OnExecute(EntityData<IEntityData> entity)
        {
            AnimatorAttribute animator = entity.GetAttribute<AnimatorAttribute>();
            var anim = animator.AnimatorComponent.GetComponent<MxM.MxMAnimator>();

            return anim.QueryUserTags(m_Tags);
        }
    }
}
#endif
