using Newtonsoft.Json;
using Syadeu.Internal;
using Syadeu.Presentation.Entities;
using System.ComponentModel;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    [DisplayName("PredicateAction: Random True")]
    [ReflectionDescription("지정한 확률로 True 를 반환합니다.")]
    public sealed class RandomTruePredicateAction : TriggerPredicateAction
    {
        [JsonProperty(Order = 0, PropertyName = "Persentage")]
        [Tooltip("확률은 0 ~ 100 까지입니다.")]
        private float m_Persentage = 50;

        protected override bool OnExecute(EntityData<IEntityData> entity)
        {
            if (m_Persentage <= 100) return true;
            else if (m_Persentage <= 0) return false;

            return Random.Range(0, 100) < m_Persentage;
        }
    }
}
