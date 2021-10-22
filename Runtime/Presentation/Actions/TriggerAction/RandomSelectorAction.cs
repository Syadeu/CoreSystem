using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Internal;
using Syadeu.Presentation.Entities;
using System;
using System.ComponentModel;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    [DisplayName("TriggerAction: Random Select Trigger")]
    [ReflectionDescription(
        "TriggerActions 에 등록된 액션 중, 랜덤하게 무작위 선택되어 하나만 실행합니다. " +
        "만약 RandomPossibiltiy 가 100 (persent) 보다 낮으면 해당 확률로 실행 여부를 결정합니다.")]
    public sealed class RandomSelectorAction : TriggerAction
    {
        [JsonProperty(Order = 0, PropertyName = "TriggerActions")]
        private Reference<TriggerAction>[] m_TriggerActions = Array.Empty<Reference<TriggerAction>>();
        [Tooltip(
            "만약 100 보다 낮으면 해당 값의 확률로 선택된 TriggerAction 의 실행 여부를 결정합니다.\n" +
            "값은 0 ~ 100 까지 입니다.")]
        [JsonProperty(Order = 1, PropertyName = "RandomPossbbility")]
        private int m_RandomPossiblity = 100;

        protected override void OnExecute(EntityData<IEntityData> entity)
        {
            if (m_TriggerActions.Length == 0)
            {
                CoreSystem.Logger.LogError(Channel.Action,
                    $"Trigger Action({Name}) doesn\'t have any TriggerAction element to execute.");
                return;
            }

            int idx = UnityEngine.Random.Range(0, m_TriggerActions.Length);
            if (m_RandomPossiblity < 100)
            {
                bool trigger = m_RandomPossiblity <= UnityEngine.Random.Range(0, 100);
                if (!trigger) return;
            }

            m_TriggerActions[idx].Execute(entity);
        }
    }
}
