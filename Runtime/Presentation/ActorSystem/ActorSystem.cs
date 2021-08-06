﻿using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
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

        public override void Dispose()
        {
            m_PlayerHashMap.Dispose();

            base.Dispose();
        }
        #endregion

        #region Raycast

        

        #endregion
    }

    public abstract class ActorEntityBase : EntityBase
    {

    }
    
    public sealed class OnMoveEvent : SynchronizedEvent<OnMoveEvent>
    {
        public Entity<ActorEntity> Entity;
    }

    public sealed class ActorEntity : EntityBase
    {
        [JsonIgnore] internal ActorSystem m_ActorSystem;

        [JsonProperty(Order = 0, PropertyName = "Faction")] private Reference<ActorFaction> m_Faction;

        [JsonIgnore] public ActorFaction Faction => m_Faction.IsValid() ? m_Faction.GetObject() : null;

        //public ActorSystem.Raycaster Raycast(Ray ray) => m_ActorSystem.Raycast(this, ray);
    }

    [AttributeAcceptOnly(typeof(ActorEntity))]
    public abstract class ActorAttributeBase : AttributeBase { }

    public sealed class ActorStatAttribute : ActorAttributeBase
    {
        [JsonProperty(Order = 0, PropertyName = "Stats")] private ValuePairContainer m_Stats = new ValuePairContainer();

        [JsonIgnore] public ValuePairContainer Stats => m_Stats;

        public static Hash ToValueHash(string name) => Hash.NewHash(name);
    }

    //public sealed class ActorGridAttribute : ActorAttributeBase
    //{
    //    [JsonIgnore] public GridSizeAttribute GridSize { get; internal set; }

    //    public void GetNearbyIndices(int range, params int[] ignoreLayers)
    //    {
    //        CoreSystem.Logger.NotNull(GridSize, "GridSizeAttribute not found");

    //        // TODO : 이거 임시, 하나만 계산하는데 나중엔 gridsize에 맞춰서
    //        int index = GridSize.CurrentGridIndices[0];

            
    //    }
    //}
    //[Preserve]
    //internal sealed class ActorGridProcessor : AttributeProcessor<ActorGridAttribute>
    //{
    //    protected override void OnCreated(ActorGridAttribute attribute, EntityData<IEntityData> entity)
    //    {
    //        attribute.GridSize = entity.GetAttribute<GridSizeAttribute>();
    //        CoreSystem.Logger.NotNull(attribute.GridSize, "GridSizeAttribute not found");


    //    }
    //}

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
