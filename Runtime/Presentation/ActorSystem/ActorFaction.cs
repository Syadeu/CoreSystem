using Newtonsoft.Json;
using Newtonsoft.Json.Utilities;
using Syadeu.Database;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Data;
using Syadeu.Presentation.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Actor
{
    [DisplayName("Data: Actor Faction")]
    public sealed class ActorFaction : DataObjectBase
    {
#pragma warning disable IDE0044 // Add readonly modifier
        [JsonProperty(Order = 0, PropertyName = "Allies")]
        private Reference<ActorFaction>[] m_Allies = Array.Empty<Reference<ActorFaction>>();
        [JsonProperty(Order = 1, PropertyName = "Enemies")] 
        private Reference<ActorFaction>[] m_Enemies = Array.Empty<Reference<ActorFaction>>();
#pragma warning restore IDE0044 // Add readonly modifier

        [JsonIgnore] private HashSet<Hash> m_AlliesHashSet;
        [JsonIgnore] private HashSet<Hash> m_EnemiesHashSet;

        internal void CreateHashSet()
        {
            m_AlliesHashSet = new HashSet<Hash>();
            for (int i = 0; i < m_Allies.Length; i++)
            {
                m_AlliesHashSet.Add(m_Allies[i].m_Hash);
            }
            m_EnemiesHashSet = new HashSet<Hash>();
            for (int i = 0; i < m_Enemies.Length; i++)
            {
                m_EnemiesHashSet.Add(m_Enemies[i].m_Hash);
            }
        }

        public bool IsAlly(ActorFaction faction)
        {
            if (m_AlliesHashSet == null) CreateHashSet();

            if (m_AlliesHashSet.Contains(faction.Hash)) return true;
            return false;
        }
        public bool IsEnemy(ActorFaction faction)
        {
            if (m_EnemiesHashSet == null) CreateHashSet();

            if (m_EnemiesHashSet.Contains(faction.Hash)) return true;
            return false;
        }

        [Preserve]
        static void AOTCodeGeneration()
        {
            AotHelper.EnsureType<Instance<ActorFaction>>();
            AotHelper.EnsureType<InstanceArray<ActorFaction>>();
            AotHelper.EnsureList<Instance<ActorFaction>>();

            AotHelper.EnsureType<Reference<ActorFaction>>();
            AotHelper.EnsureList<Reference<ActorFaction>>();
            AotHelper.EnsureType<ActorFaction>();
            AotHelper.EnsureList<ActorFaction>();
        }
    }
}
