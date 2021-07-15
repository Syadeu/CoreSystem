using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Database.Converters;
using Syadeu.Database.Lua;
using Syadeu.Internal;
using Syadeu.Presentation;
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
    }

    internal interface ICreatureAttribute
    {
        [DataMember] [JsonProperty(Order = -5)] string Name { get; }
        [DataMember] [JsonProperty(Order = -4)] Hash Hash { get; }
        [DataMember] [JsonProperty(Order = -3)] LuaScript OnEntityStart { get; }
        [DataMember] [JsonProperty(Order = -2)] LuaScript OnEntityDestory { get; }
        [DataMember] [JsonProperty(Order = -1)] ValuePairContainer Values { get; }
    }

    [DataContract]
    public abstract class CreatureAttribute : DataComponentEntity, ICreatureAttribute
    {
        public string Name { get; set; } = "New Attribute";
        public Hash Hash { get; set; }
        public ValuePairContainer Values { get; set; }
        public LuaScript OnEntityStart { get; set; }
        public LuaScript OnEntityDestory { get; set; }
    }
    internal interface ICreatureAttributeProcessor
    {
        Type TargetAttribute { get; }
        void OnCreated(CreatureAttribute attribute, Creature creature, DataGameObject dataObj);
    }
    public abstract class CreatureAttributeProcessor : ICreatureAttributeProcessor
    {
        Type ICreatureAttributeProcessor.TargetAttribute => TargetAttribute;
        void ICreatureAttributeProcessor.OnCreated(CreatureAttribute attribute, Creature creature, DataGameObject dataObj) => OnCreated(attribute, creature, dataObj);

        protected abstract Type TargetAttribute { get; }
        protected abstract void OnCreated(CreatureAttribute attribute, Creature creature, DataGameObject dataObj);
    }
    public abstract class CreatureAttributeProcessor<T> : ICreatureAttributeProcessor where T : CreatureAttribute
    {
        Type ICreatureAttributeProcessor.TargetAttribute => TargetAttribute;
        void ICreatureAttributeProcessor.OnCreated(CreatureAttribute attribute, Creature creature, DataGameObject dataObj) => OnCreated((T)attribute, creature, dataObj);

        private Type TargetAttribute => TypeHelper.TypeOf<T>.Type;
        protected abstract void OnCreated(T attribute, Creature creature, DataGameObject dataObj);
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
