// Copyright 2021 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Components;
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
        [UnityEngine.SerializeField, JsonProperty(Order = 0, PropertyName = "HorizontalKey")] private string m_HorizontalKey = string.Empty;
        [UnityEngine.SerializeField, JsonProperty(Order = 1, PropertyName = "VerticalKey")] private string m_VerticalKey = string.Empty;
        [UnityEngine.SerializeField, JsonProperty(Order = 2, PropertyName = "SpeedKey")] private string m_SpeedKey = string.Empty;

        [Space]
        [UnityEngine.SerializeField, JsonProperty(Order = 3, PropertyName = "AnimationSpeed")] private float m_AnimationSpeed = 2;

        [JsonIgnore] private int m_Horizontal;
        [JsonIgnore] private int m_Vertical;
        [JsonIgnore] private int m_Speed;

        protected override void OnCreated()
        {
#if DEBUG
            if (string.IsNullOrEmpty(m_HorizontalKey))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"TriggerAction({nameof(NavAgentDirToAnimatorAction)}, name of {Name}) has empty {nameof(m_HorizontalKey)} string. This is not allowed.");
            }
            if (string.IsNullOrEmpty(m_VerticalKey))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"TriggerAction({nameof(NavAgentDirToAnimatorAction)}, name of {Name}) has empty {nameof(m_VerticalKey)} string. This is not allowed.");
            }
            if (string.IsNullOrEmpty(m_SpeedKey))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"TriggerAction({nameof(NavAgentDirToAnimatorAction)}, name of {Name}) has empty {nameof(m_SpeedKey)} string. This is not allowed.");
            }
#endif

            m_Horizontal = Animator.StringToHash(m_HorizontalKey);
            m_Vertical = Animator.StringToHash(m_VerticalKey);
            m_Speed = Animator.StringToHash(m_SpeedKey);
        }
        protected override void OnExecute(Entity<IObject> entity)
        {
            //NavAgentAttribute navAgent = entity.GetAttribute<NavAgentAttribute>();
            if (!entity.HasComponent<NavAgentComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"{nameof(NavAgentAttribute)} not found at {entity.Name}");
                return;
            }
            NavAgentComponent navAgent = entity.GetComponentReadOnly<NavAgentComponent>();
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
