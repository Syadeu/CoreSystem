using Newtonsoft.Json;
using Newtonsoft.Json.Utilities;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Entities;
using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Actor
{
    [DisplayName("Entity: Actor")]
    public sealed class ActorEntity : EntityBase
    {
        [JsonProperty(Order = 0, PropertyName = "Faction")] private Reference<ActorFaction> m_Faction;

        [Space, Header("TriggerAction")]
        [JsonProperty(Order = 1, PropertyName = "OnCreated")]
        internal Reference<TriggerAction>[] m_OnCreated = Array.Empty<Reference<TriggerAction>>();
        [JsonProperty(Order = 2, PropertyName = "OnDestroy")]
        internal Reference<TriggerAction>[] m_OnDestroy = Array.Empty<Reference<TriggerAction>>();

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
        protected override void OnCreated(EntityData<ActorEntity> entity)
        {
            entity.Target.m_OnCreated.Execute(entity.Cast<ActorEntity, IEntityData>());
        }
        protected override void OnDestroy(EntityData<ActorEntity> entity)
        {
            entity.Target.m_OnDestroy.Execute(entity.Cast<ActorEntity, IEntityData>());
        }
    }
}
