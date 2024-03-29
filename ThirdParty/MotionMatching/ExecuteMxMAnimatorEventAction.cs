﻿using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Actions;
using System.ComponentModel;
using Syadeu.Collections;
using Syadeu.Presentation.Entities;
using Newtonsoft.Json;

#if CORESYSTEM_MOTIONMATCHING
namespace Syadeu.Presentation.MotionMatching
{
    [DisplayName("TriggerAction: Execute MxMAnimator Event")]
    public sealed class ExecuteMxMAnimatorEventAction : TriggerAction
    {
        [JsonProperty(Order = 0, PropertyName = "EventID")]
        private Reference<MxMEventDefinitionData> m_EventData = Reference<MxMEventDefinitionData>.Empty;

        protected override void OnExecute(Entity<IObject> entity)
        {
            AnimatorAttribute animator = entity.GetAttribute<AnimatorAttribute>();
            var anim = animator.AnimatorComponent.GetComponent<MxM.MxMAnimator>();

            var temp = m_EventData.GetObject();

            if (temp.EventDefinition == null)
            {
                temp.Initialize();
            }
            
            anim.BeginEvent(temp.EventDefinition);
        }
    }
}
#endif
