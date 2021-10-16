﻿using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Actions;
using System.ComponentModel;
using Syadeu.Collections;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Map;
using UnityEngine;

#if CORESYSTEM_MOTIONMATCHING
namespace Syadeu.Presentation.MotionMatching
{
    [DisplayName("TriggerAction: Match MxMAnimator to NavAgent")]
    public sealed class NavAgentDirToMxMAnimatorAction : TriggerAction
    {
        protected override void OnExecute(EntityData<IEntityData> entity)
        {
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

            if (animator.AnimatorComponent == null) return;

            var trajectory = animator.AnimatorComponent.GetComponent<MxM.MxMTrajectoryGenerator>();
            trajectory.InputVector = navAgent.Direction;
        }
    }
}
#endif
