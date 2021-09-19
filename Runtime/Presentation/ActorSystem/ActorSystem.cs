using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
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

        private EntitySystem m_EntitySystem;
        private EventSystem m_EventSystem;

        #region Presentation Methods
        protected override PresentationResult OnInitializeAsync()
        {
            RequestSystem<EntitySystem>(Bind);
            RequestSystem<EventSystem>(Bind);

            return base.OnInitializeAsync();
        }

        #region Bind

        private void Bind(EntitySystem other)
        {
            m_EntitySystem = other;

            m_EntitySystem.OnEntityCreated += M_EntitySystem_OnEntityCreated;
            m_EntitySystem.OnEntityDestroy += M_EntitySystem_OnEntityDestroy;
        }
        private void M_EntitySystem_OnEntityCreated(EntityData<IEntityData> obj)
        {
            if (!TypeHelper.TypeOf<ActorEntity>.Type.IsAssignableFrom(obj.Type)) return;

            //Entity<ActorEntity> entity = obj.As<IEntityData, ActorEntity>();

            //m_PlayerHashMap.Add(obj.Idx, obj.As<IEntityData, ActorEntity>());
        }
        private void M_EntitySystem_OnEntityDestroy(EntityData<IEntityData> obj)
        {
            if (!TypeHelper.TypeOf<ActorEntity>.Type.IsAssignableFrom(obj.Type)) return;

            //Entity<ActorEntity> entity = obj.As<IEntityData, ActorEntity>();

            //m_PlayerHashMap.Remove(obj.Idx);
        }
        private void Bind(EventSystem other)
        {
            m_EventSystem = other;

            m_EventSystem.AddEvent<OnMoveStateChangedEvent>(OnActorMoveStateChanged);
        }

        #endregion

        private void OnActorMoveStateChanged(OnMoveStateChangedEvent ev)
        {
            //$"{ev.Entity.Name}: {ev.State}".ToLog();
        }

        public override void OnDispose()
        {
            m_EntitySystem.OnEntityCreated -= M_EntitySystem_OnEntityCreated;
            m_EntitySystem.OnEntityDestroy -= M_EntitySystem_OnEntityDestroy;

            m_EntitySystem = null;

            m_EventSystem.RemoveEvent<OnMoveStateChangedEvent>(OnActorMoveStateChanged);

            m_EventSystem = null;
        }
        #endregion
    }

    //public sealed class AIActorTargetSystem : PresentationSystemEntity<AIActorTargetSystem>
    //{
    //    public override bool EnableBeforePresentation => false;
    //    public override bool EnableOnPresentation => false;
    //    public override bool EnableAfterPresentation => false;

    //}
}
