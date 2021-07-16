using Newtonsoft.Json;
using Syadeu.Mono;
using Syadeu.Presentation;
using System;
using UnityEngine;

namespace Syadeu.Database.CreatureData.Attributes
{
    public sealed class StatAttribute : CreatureAttribute
    {
        [Space]
        [JsonProperty(Order = 1, PropertyName = "Stats")] public ValuePairContainer m_Stats;
    }
    public sealed class StatProcessor : AttributeProcessor<StatAttribute>
    {
        protected override void OnCreated(StatAttribute attribute, DataGameObject dataObj)
        {
            CreatureBrain monoObj = (CreatureBrain)dataObj.transform.ProxyObject;

            CreatureStat stat = monoObj.Stat;
            if (stat == null)
            {
                stat = monoObj.gameObject.AddComponent<CreatureStat>();
                monoObj.InitializeCreatureEntity(stat);
            }

            stat.Values = attribute.m_Stats;
        }

        protected override void OnDestory(StatAttribute attribute, DataGameObject dataObj)
        {
            "destory".ToLog();
        }
    }
    
}
