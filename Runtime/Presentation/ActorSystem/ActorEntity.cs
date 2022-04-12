// Copyright 2021 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
        [SerializeField]
        [JsonProperty(Order = 0, PropertyName = "Faction")]
        private Reference<ActorFaction> m_Faction = Reference<ActorFaction>.Empty;

        [Space, Header("TriggerAction")]
        [SerializeField]
        [JsonProperty(Order = 1, PropertyName = "OnCreated")]
        internal ArrayWrapper<Reference<TriggerAction>> m_OnCreated = Array.Empty<Reference<TriggerAction>>();
        [SerializeField]
        [JsonProperty(Order = 2, PropertyName = "OnDestroy")]
        internal ArrayWrapper<Reference<TriggerAction>> m_OnDestroy = Array.Empty<Reference<TriggerAction>>();

        [Space]
        [SerializeField]
        [JsonProperty(Order = 3)]
        internal ConstActionReference<int> m_test;

        [JsonIgnore] public Entity<IEntityData> Parent => Entity<IEntityData>.GetEntityWithoutCheck(Idx);
        [JsonIgnore] public ActorFaction Faction => m_Faction.IsValid() ? m_Faction.GetObject() : null;

        [Preserve]
        static void AOTCodeGeneration()
        {
            AotHelper.EnsureType<Reference<ActorEntity>>();
            AotHelper.EnsureList<Reference<ActorEntity>>();
            AotHelper.EnsureType<Entity<ActorEntity>>();
            AotHelper.EnsureList<Entity<ActorEntity>>();
            AotHelper.EnsureType<ActorEntity>();
            AotHelper.EnsureList<ActorEntity>();
        }
    }
    internal sealed class ActorProccesor : EntityProcessor<ActorEntity>
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
                    allies.Add(actor.Faction.m_Allies[i].Hash);
                }
                for (int i = 0; i < actor.Faction.m_Enemies.Length; i++)
                {
                    enemies.Add(actor.Faction.m_Enemies[i].Hash);
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

            Entity<IObject> entity = Entity<IObject>.GetEntityWithoutCheck(actor.Idx);

            entity.AddComponent<ActorFactionComponent>();
            ref var com = ref entity.GetComponent<ActorFactionComponent>();
            com = (new ActorFactionComponent()
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
            Entity<IObject> entity = Entity<IObject>.GetEntityWithoutCheck(actor.Idx);

            actor.m_OnDestroy.Execute(entity);
        }
    }
}
