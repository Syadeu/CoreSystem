using Newtonsoft.Json;
using Syadeu.Presentation.Entities;
using UnityEngine;

namespace Syadeu.Presentation.Attributes
{
    public sealed class NavAgentDirToAnimatorAction : ActionBase<NavAgentDirToAnimatorAction>
    {
        [JsonProperty(Order = 0, PropertyName = "HorizontalKey")] private string m_HorizontalKey = string.Empty;
        [JsonProperty(Order = 1, PropertyName = "VerticalKey")] private string m_VerticalKey = string.Empty;
        [JsonProperty(Order = 2, PropertyName = "SpeedKey")] private string m_SpeedKey = string.Empty;

        [JsonIgnore] private int m_Horizontal;
        [JsonIgnore] private int m_Vertical;
        [JsonIgnore] private int m_Speed;

        protected override void OnInitialize()
        {
            m_Horizontal = Animator.StringToHash(m_HorizontalKey);
            m_Vertical = Animator.StringToHash(m_VerticalKey);
            m_Speed = Animator.StringToHash(m_SpeedKey);
        }
        protected override void OnExecute(EntityData<IEntityData> entity)
        {
            NavAgentAttribute navAgent = entity.GetAttribute<NavAgentAttribute>();
            if (navAgent == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"{nameof(NavAgentAttribute)} not found at {entity.Name}");
                return;
            }

            AnimatorAttribute animator = entity.GetAttribute<AnimatorAttribute>();
            if (animator == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"{nameof(AnimatorAttribute)} not found at {entity.Name}");
                return;
            }

            Vector3 dir = navAgent.Direction;
            animator.SetFloat(m_Horizontal, dir.x);
            animator.SetFloat(m_Vertical, dir.y);

            if (navAgent.IsMoving)
            {
                animator.SetFloat(m_Speed, dir.sqrMagnitude > 0 ? 1 : 0);
            }
            else animator.SetFloat(m_Speed, 0);
        }
    }
}
