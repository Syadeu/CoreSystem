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
    public sealed class Creature : ICloneable
    {
        [JsonProperty(Order = 0, PropertyName = "Name")] public string m_Name = "New Creature Entity";
        [JsonProperty(Order = 1, PropertyName = "Hash")] public Hash m_Hash;
        [JsonProperty(Order = 2, PropertyName = "PrefabIdx")] public int m_PrefabIdx;
        [JsonProperty(Order = 3, PropertyName = "Attributes")] public List<Hash> m_Attributes;
        [JsonProperty(Order = 4, PropertyName = "HP")] public float m_HP;
        [JsonProperty(Order = 5, PropertyName = "Values")] public ValuePairContainer m_Values = new ValuePairContainer();

        public object Clone()
        {
            Creature dup = (Creature)MemberwiseClone();
            dup.m_Attributes = new List<Hash>(m_Attributes);
            dup.m_Values = (ValuePairContainer)m_Values.Clone();

            return dup;
        }
    }
}
