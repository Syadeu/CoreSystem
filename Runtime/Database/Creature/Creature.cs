using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Database.Converters;
using Syadeu.Internal;
using Syadeu.Presentation;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Database.CreatureData
{
    [Serializable]
    public sealed class Creature
    {
        [JsonProperty(Order = 0, PropertyName = "Name")] public string m_Name = "New Creature Entity";
        [JsonProperty(Order = 1, PropertyName = "Hash")] public Hash m_Hash;
        [JsonProperty(Order = 2, PropertyName = "PrefabIdx")] public int m_PrefabIdx;
        [JsonProperty(Order = 3, PropertyName = "Attributes")] public List<Hash> m_Attributes;
        [JsonProperty(Order = 4, PropertyName = "Values")] public ValuePairContainer m_Values = new ValuePairContainer();
    }
    
    public sealed class CreatureStatAttribute : CreatureAttribute
    {
        [JsonProperty(Order = 3, PropertyName = "Stats")] public ValuePairContainer m_Stats;
    }

    public sealed class CreatureOnSpawnAttribute : CreatureAttribute
    {
        //[JsonProperty(Order = 3, PropertyName = "OnSpawnLuaFunc")] public LuaScript m_OnSpawnLuaFunc;
    }
    public sealed class CreatureOnSpawnProcessor : CreatureAttributeProcessor<CreatureOnSpawnAttribute>
    {
        protected override void OnCreated(CreatureOnSpawnAttribute attribute, Creature creature, DataGameObject dataObj)
        {
            "in".ToLog();
        }
    }
}
