using Newtonsoft.Json;
using Newtonsoft.Json.Utilities;
using Syadeu.Collections;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using System;
using System.ComponentModel;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Actor
{
    [DisplayName("Entity: Actor")]
    public sealed class ActorEntity : EntityBase,
        INotifyComponent<ActorFactionComponent>
    {
        [JsonProperty(Order = 0, PropertyName = "Faction")] private Reference<ActorFaction> m_Faction;

        [Space, Header("TriggerAction")]
        [JsonProperty(Order = 1, PropertyName = "OnCreated")]
        internal Reference<TriggerAction>[] m_OnCreated = Array.Empty<Reference<TriggerAction>>();
        [JsonProperty(Order = 2, PropertyName = "OnDestroy")]
        internal Reference<TriggerAction>[] m_OnDestroy = Array.Empty<Reference<TriggerAction>>();

        [JsonIgnore] public EntityData<IEntityData> Parent => EntityData<IEntityData>.GetEntityWithoutCheck(Idx);
        [JsonIgnore] public ActorFaction Faction => m_Faction.IsValid() ? m_Faction.GetObject() : null;

        [Preserve]
        static void AOTCodeGeneration()
        {
            AotHelper.EnsureType<Instance<ActorEntity>>();
            AotHelper.EnsureType<InstanceArray<ActorEntity>>();
            AotHelper.EnsureList<Instance<ActorEntity>>();

            AotHelper.EnsureType<Reference<ActorEntity>>();
            AotHelper.EnsureList<Reference<ActorEntity>>();
            AotHelper.EnsureType<Entity<ActorEntity>>();
            AotHelper.EnsureList<Entity<ActorEntity>>();
            AotHelper.EnsureType<EntityData<ActorEntity>>();
            AotHelper.EnsureList<EntityData<ActorEntity>>();
            AotHelper.EnsureType<ActorEntity>();
            AotHelper.EnsureList<ActorEntity>();
        }
    }
    internal sealed class ActorProccesor : EntityDataProcessor<ActorEntity>
    {
        protected override void OnCreated(ActorEntity actor)
        {
            FixedList512Bytes<Hash> allies = new FixedList512Bytes<Hash>();
            FixedList512Bytes<Hash> enemies = new FixedList512Bytes<Hash>();

            FactionType factionType;
            Hash factionHash;
            if (actor.Faction != null)
            {
                for (int i = 0; i < actor.Faction.m_Allies.Length; i++)
                {
                    allies.Add(actor.Faction.m_Allies[i].m_Hash);
                }
                for (int i = 0; i < actor.Faction.m_Enemies.Length; i++)
                {
                    enemies.Add(actor.Faction.m_Enemies[i].m_Hash);
                }
                factionType = actor.Faction.m_FactionType;
                factionHash = actor.Faction.Hash;
            }
            else
            {
                factionType = FactionType.NPC;
                factionHash = Hash.Empty;

                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Actor({actor.Name}) doesn\'t have any faction. This is not allowed.");
            }

            EntityData<IEntityData> entity = EntityData<IEntityData>.GetEntityWithoutCheck(actor.Idx);

            entity.AddComponent<ActorFactionComponent>(new ActorFactionComponent()
            {
                m_FactionType = factionType,
                m_Hash = factionHash,
                m_Allies = allies,
                m_Enemies = enemies
            });
            actor.m_OnCreated.Execute(entity);
        }
        protected override void OnDestroy(ActorEntity actor)
        {
            EntityData<IEntityData> entity = EntityData<IEntityData>.GetEntityWithoutCheck(actor.Idx);

            actor.m_OnDestroy.Execute(entity);
        }
    }
}
