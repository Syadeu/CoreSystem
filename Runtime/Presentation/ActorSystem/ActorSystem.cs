﻿using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Event;
using Syadeu.Presentation.Map;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Actor
{
    public sealed class ActorSystem : PresentationSystemEntity<ActorSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private NativeHashMap<Hash, Entity<ActorEntity>> m_PlayerHashMap;
        //private readonly Dictionary<ActorType, List<Entity<ActorEntity>>> m_Actors = new Dictionary<ActorType, List<Entity<ActorEntity>>>();

        private EntitySystem m_EntitySystem;
        private EventSystem m_EventSystem;

        #region Presentation Methods
        protected override PresentationResult OnInitializeAsync()
        {
            m_PlayerHashMap = new NativeHashMap<Hash, Entity<ActorEntity>>(1024, Allocator.Persistent);

            RequestSystem<EntitySystem>((other) =>
            {
                m_EntitySystem = other;

                m_EntitySystem.OnEntityCreated += M_EntitySystem_OnEntityCreated;
                m_EntitySystem.OnEntityDestroy += M_EntitySystem_OnEntityDestroy;
            });
            RequestSystem<EventSystem>((other) =>
            {
                m_EventSystem = other;

                m_EventSystem.AddEvent<OnMoveStateChangedEvent>(OnActorMoveStateChanged);
            });

            return base.OnInitializeAsync();
        }

        private void M_EntitySystem_OnEntityCreated(EntityData<IEntityData> obj)
        {
            if (!obj.Type.Equals(TypeHelper.TypeOf<ActorEntity>.Type)) return;

            Entity<ActorEntity> actorRef = obj;

            actorRef.Target.m_ActorSystem = this;

            m_PlayerHashMap.Add(actorRef.Idx, actorRef);
        }
        private void M_EntitySystem_OnEntityDestroy(EntityData<IEntityData> obj)
        {
            if (!obj.Type.Equals(TypeHelper.TypeOf<ActorEntity>.Type)) return;

            Entity<ActorEntity> actorRef = obj;

            actorRef.Target.m_ActorSystem = null;

            m_PlayerHashMap.Remove(actorRef.Idx);
        }

        private void OnActorMoveStateChanged(OnMoveStateChangedEvent ev)
        {
            $"{ev.Entity.Name}: {ev.State}".ToLog();
        }

        public override void Dispose()
        {
            m_PlayerHashMap.Dispose();

            m_EntitySystem.OnEntityCreated -= M_EntitySystem_OnEntityCreated;
            m_EntitySystem.OnEntityDestroy -= M_EntitySystem_OnEntityDestroy;

            m_EntitySystem = null;

            base.Dispose();
        }
        #endregion

        #region Raycast

        

        #endregion
    }
}
