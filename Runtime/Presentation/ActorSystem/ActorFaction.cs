using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using System;
using Unity.Collections;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Actor
{
    [Serializable, AttributeAcceptOnly]
    public sealed class ActorFaction : AttributeBase
    {
#pragma warning disable IDE0044 // Add readonly modifier
        [JsonProperty(Order = 0, PropertyName = "Allies")] private Reference<ActorFaction>[] m_Allies;
        [JsonProperty(Order = 1, PropertyName = "Enemies")] private Reference<ActorFaction>[] m_Enemies;
#pragma warning restore IDE0044 // Add readonly modifier

        [JsonIgnore] private NativeHashSet<Hash> m_AlliesHashSet;
        [JsonIgnore] private NativeHashSet<Hash> m_EnemiesHashSet;

        internal void CreateHashSet()
        {
            m_AlliesHashSet = new NativeHashSet<Hash>(64, Allocator.Persistent);
            for (int i = 0; i < m_Allies.Length; i++)
            {
                m_AlliesHashSet.Add(m_Allies[i].m_Hash);
            }
            m_EnemiesHashSet = new NativeHashSet<Hash>(64, Allocator.Persistent);
            for (int i = 0; i < m_Enemies.Length; i++)
            {
                m_EnemiesHashSet.Add(m_Enemies[i].m_Hash);
            }
        }
        internal void DisposeHashSet()
        {
            m_AlliesHashSet.Dispose();
            m_EnemiesHashSet.Dispose();
        }

        public bool IsAlly(ActorFaction faction)
        {
            if (m_AlliesHashSet.Contains(faction.Hash)) return true;
            return false;
        }
        public bool IsEnemy(ActorFaction faction)
        {
            if (m_EnemiesHashSet.Contains(faction.Hash)) return true;
            return false;
        }
    }
    [Preserve]
    internal sealed class ActorFactionProcessor : AttributeProcessor<ActorFaction>
    {
        protected override void OnCreated(ActorFaction attribute, EntityData<IEntityData> entity)
        {
            attribute.CreateHashSet();
        }
        protected override void OnDestroy(ActorFaction attribute, EntityData<IEntityData> entity)
        {
            attribute.DisposeHashSet();
        }
    }
}
