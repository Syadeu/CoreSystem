using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Database.Converters;
using Syadeu.Database.Lua;
using Syadeu.Internal;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
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
        [DataMember] [JsonProperty(Order = -3)] string Name { get; }
        [DataMember] [JsonProperty(Order = -2)] Hash Hash { get; set; }
        [DataMember] [JsonProperty(Order = -1)] ValuePairContainer Values { get; }
    }

    [DataContract]
    public class CreatureStatAttribute : ICreatureAttribute
    {
        public string Name { get; set; } = "New Stat Attribute";
        public Hash Hash { get; set; }
        public ValuePairContainer Values { get; set; }

        [JsonProperty(Order = 3, PropertyName = "Stats")] public ValuePairContainer m_Stats;
    }
    [DataContract]
    public class CreatureOnSpawnAttribute : ICreatureAttribute
    {
        public string Name { get; set; } = "New OnSpawn Attribute";
        public Hash Hash { get; set; }
        public ValuePairContainer Values { get; set; }

        [JsonProperty(Order = 3, PropertyName = "OnSpawnLuaFunc")] public LuaScript m_OnSpawnLuaFunc;
    }
}
