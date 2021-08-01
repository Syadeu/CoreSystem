﻿using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Map;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Actor
{
    public sealed class ActorSystem : PresentationSystemEntity<ActorSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private NativeHashMap<Hash, Entity<ActorEntity>> m_Players;
        private readonly Dictionary<ActorType, List<Entity<ActorEntity>>> m_Actors = new Dictionary<ActorType, List<Entity<ActorEntity>>>();

        private EntitySystem m_EntitySystem;

        protected override PresentationResult OnInitializeAsync()
        {
            m_Players = new NativeHashMap<Hash, Entity<ActorEntity>>(1024, Allocator.Persistent);

            RequestSystem<EntitySystem>((other) =>
            {
                m_EntitySystem = other;

                m_EntitySystem.OnEntityCreated += M_EntitySystem_OnEntityCreated;
                m_EntitySystem.OnEntityDestroy += M_EntitySystem_OnEntityDestroy;
            });



            return base.OnInitializeAsync();
        }

        private void M_EntitySystem_OnEntityCreated(EntityData<IEntityData> obj)
        {
            if (!obj.Type.Equals(TypeHelper.TypeOf<ActorEntity>.Type)) return;

            Entity<ActorEntity> actorRef = obj;
            m_Players.Add(actorRef.Idx, actorRef);

            ActorEntity actor = actorRef;
            if (!m_Actors.TryGetValue(actor.ActorType, out var actorList))
            {
                actorList = new List<Entity<ActorEntity>>();
                m_Actors.Add(actor.ActorType, actorList);
            }
            actorList.Add(actorRef);
        }
        private void M_EntitySystem_OnEntityDestroy(EntityData<IEntityData> obj)
        {
            if (!obj.Type.Equals(TypeHelper.TypeOf<ActorEntity>.Type)) return;

            Entity<ActorEntity> actorRef = obj;
            m_Players.Remove(actorRef.Idx);

            ActorEntity actor = actorRef;
            m_Actors[actor.ActorType].Remove(actorRef);
        }

        public override void Dispose()
        {
            m_Players.Dispose();
            m_Actors.Clear();

            base.Dispose();
        }
    }

    public sealed class ActorEntity : EntityBase
    {
        [JsonProperty(Order = 0, PropertyName = "ActorType")] private ActorType m_ActorType;

        [JsonIgnore] public ActorType ActorType => m_ActorType;
    }

    [AttributeAcceptOnly(typeof(ActorEntity))]
    public abstract class ActorAttributeBase : AttributeBase { }

    //[ReflectionDescription("이 액터의 타입을 설정합니다.")]
    //public sealed class ActorTypeAttribute : ActorAttributeBase
    //{
    //    [JsonProperty(Order = 0, PropertyName = "ActorType")] public ActorType m_ActorType;
    //}
    //[Preserve]
    //internal sealed class ActorTypeProcessor : AttributeProcessor<ActorTypeAttribute>
    //{
    //    protected override void OnCreated(ActorTypeAttribute attribute, EntityData<IEntityData> entity)
    //    {
    //        PresentationSystem<ActorSystem>.System.m_Players.Add(entity);
    //    }
    //}
}
