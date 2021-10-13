﻿using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Internal;
using Syadeu.Presentation.Entities;
using System;
using System.ComponentModel;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    [DisplayName("TriggerAction: Execute Animation")]
    [ReflectionDescription(
        "애니메이션 클립에 달린 TriggerAction이 타겟으로 삼을 액션입니다.")]
    public sealed class AnimationTriggerAction : TriggerAction
    {
        [Header("General")]
        [JsonProperty(Order = 0, PropertyName = "TriggerName")] public string m_TriggerName;

        [Space, Header("TriggerActions")]
        [JsonProperty(Order = 1, PropertyName = "OnExecute")]
        private Reference<TriggerAction>[] m_OnExecute = Array.Empty<Reference<TriggerAction>>();

        protected override void OnExecute(EntityData<IEntityData> entity)
        {
            for (int i = 0; i < m_OnExecute.Length; i++)
            {
                m_OnExecute[i].Execute(entity);
            }
        }
    }
}
