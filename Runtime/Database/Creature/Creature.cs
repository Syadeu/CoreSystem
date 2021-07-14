using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Database.Converters;
using Syadeu.Database.Lua;
using Syadeu.Internal;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Database.CreatureData
{
    [Serializable]
    public sealed class Creature
    {
        [JsonProperty(Order = 0, PropertyName = "Name")] public string m_Name;
        [JsonProperty(Order = 1, PropertyName = "Hash")] public Hash m_Hash;
        [JsonProperty(Order = 2, PropertyName = "PrefabIdx")] public int m_PrefabIdx;
        [JsonProperty(Order = 3, PropertyName = "Attributes")] public List<Hash> m_Attributes;
        [JsonProperty(Order = 4, PropertyName = "Values")] public ValuePairContainer m_Values = new ValuePairContainer();

        [JsonProperty(Order = 5, PropertyName = "OnSpawn")] public LuaScriptContainer m_OnSpawn;
    }

    //[Serializable] [JsonConverter(typeof(CreatureAttributeJsonConverter))]
    //public abstract class CreatureAttributeEntity
    //{
    //    [JsonProperty(Order = 0, PropertyName = "Name")] public string m_Name;
    //    [JsonProperty(Order = 1, PropertyName = "Hash")] public Hash m_Hash;
    //    [JsonProperty(Order = 2, PropertyName = "Values")] public ValuePairContainer m_Values = new ValuePairContainer();
    //}
    public interface ICreatureAttribute
    {
        string Name { get; }
        Hash Hash { get; }
        ValuePairContainer Values { get; }
    }

    [Serializable]
    public class CreatureStatAttribute : ICreatureAttribute
    {
        [JsonProperty(Order = 0)] public string Name { get; set; }
        [JsonProperty(Order = 1)] public Hash Hash { get; set; }
        [JsonProperty(Order = 2)] public ValuePairContainer Values { get; set; }

        [JsonProperty(Order = 3, PropertyName = "Stats")] public ValuePairContainer m_Stats;
    }
    [Serializable]
    public class CreatureOnSpawnAttribute : ICreatureAttribute
    {
        [JsonProperty(Order = 0)] public string Name { get; set; }
        [JsonProperty(Order = 1)] public Hash Hash { get; set; }
        [JsonProperty(Order = 2)] public ValuePairContainer Values { get; set; }

        [JsonProperty(Order = 3, PropertyName = "OnSpawnLuaFunc")] public LuaScript m_OnSpawnLuaFunc;
    }
}
