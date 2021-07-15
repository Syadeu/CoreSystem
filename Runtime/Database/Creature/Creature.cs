using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Database.Converters;
using Syadeu.Database.Lua;
using Syadeu.Internal;
using Syadeu.Mono;
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
    public sealed class CreatureStatProcessor : CreatureAttributeProcessor<CreatureStatAttribute>
    {
        protected override void OnCreated(CreatureStatAttribute attribute, Creature creature, DataGameObject dataObj, CreatureBrain monoObj)
        {
            CreatureStat stat = monoObj.Stat;
            if (stat == null)
            {
                stat = monoObj.gameObject.AddComponent<CreatureStat>();
                monoObj.InitializeCreatureEntity(stat);
            }

            stat.Values = attribute.m_Stats;
        }
    }

    public sealed class CreatureOnSpawnAttribute : CreatureAttribute
    {
        [JsonProperty(Order = 0, PropertyName = "OnSpawn")] public LuaScriptContainer m_OnSpawn;
    }
    public sealed class CreatureOnSpawnProcessor : CreatureAttributeProcessor<CreatureOnSpawnAttribute>
    {
        const string c_ScriptError = "On Spawn Attribute has an invalid lua function({0}) at Entity({1}). Request ignored.";

        protected override void OnCreated(CreatureOnSpawnAttribute attribute, Creature creature, DataGameObject dataObj, CreatureBrain monoObj)
        {
            "in".ToLog();
            if (attribute.m_OnSpawn.m_Scripts == null) return;
            for (int i = 0; i < attribute.m_OnSpawn.m_Scripts.Count; i++)
            {
                if (attribute.m_OnSpawn.m_Scripts[i] == null) continue;
                if (!attribute.m_OnSpawn.m_Scripts[i].IsValid())
                {
                    CoreSystem.Logger.LogWarning(Channel.Lua, string.Format(c_ScriptError, $"OnSpawn: {i}", creature.m_Name));
                    continue;
                }

                CreatureSystem.InvokeLua(attribute.m_OnSpawn.m_Scripts[i], creature, dataObj, monoObj,
                    calledAttName: TypeHelper.TypeOf<CreatureOnSpawnAttribute>.Name,
                    calledScriptName: "OnSpawn");
            }
        }
    }
}
