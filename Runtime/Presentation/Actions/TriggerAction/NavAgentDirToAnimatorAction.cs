using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Map;
using System.ComponentModel;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    [DisplayName("TriggerAction: Match Animator to NavAgent")]
    public sealed class NavAgentDirToAnimatorAction : TriggerAction
    {
        [JsonProperty(Order = 0, PropertyName = "HorizontalKey")] private string m_HorizontalKey = string.Empty;
        [JsonProperty(Order = 1, PropertyName = "VerticalKey")] private string m_VerticalKey = string.Empty;
        [JsonProperty(Order = 2, PropertyName = "SpeedKey")] private string m_SpeedKey = string.Empty;

        [Space]
        [JsonProperty(Order = 3, PropertyName = "AnimationSpeed")] private float m_AnimationSpeed = 2;

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
            //NavAgentAttribute navAgent = entity.GetAttribute<NavAgentAttribute>();
            if (!entity.HasComponent<NavAgentComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"{nameof(NavAgentAttribute)} not found at {entity.Name}");
                return;
            }
            NavAgentComponent navAgent = entity.GetComponent<NavAgentComponent>();
            AnimatorAttribute animator = entity.GetAttribute<AnimatorAttribute>();
            if (animator == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"{nameof(AnimatorAttribute)} not found at {entity.Name}");
                return;
            }

            Vector3 dir = navAgent.Direction;
            //Quaternion rot = animator.Animator.transform.rotation;
            //rot.SetLookRotation(dir);
            //animator.Animator.transform.rotation = rot;

            float
                //speed = navAgent.Speed,
                speed = 2,
                prevSpeed = animator.GetFloat(m_Speed),
                prevHorizontal = animator.GetFloat(m_Horizontal)
                //prevVertical = animator.GetFloat(m_Vertical)
                ;

            animator.SetFloat(m_Horizontal, math.lerp(prevHorizontal, speed, Time.deltaTime * m_AnimationSpeed));


            if (navAgent.IsMoving)
            {
                animator.SetFloat(m_Speed, math.lerp(prevSpeed, speed, Time.deltaTime * m_AnimationSpeed));
            }
            else
            {
                animator.SetFloat(m_Speed, 0);
            }
        }
    }
}
