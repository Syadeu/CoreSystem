using JetBrains.Annotations;
using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Mono;
using Syadeu.Mono.TurnTable;
using Syadeu.ThreadSafe;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.AI;
using UnityEngine.Scripting;

namespace Syadeu.Presentation
{
    public sealed class CreatePrefabAttribute : AttributeBase
    {
        [JsonProperty(Order = 0, PropertyName = "Prefab")] public PrefabReference m_Prefab;

        [JsonIgnore] public DataGameObject PrefabInstance { get; internal set; }
    }
    [Preserve]
    internal sealed class CreatePrefabProcessor : AttributeProcessor<CreatePrefabAttribute>
    {
        protected override void OnCreated(CreatePrefabAttribute attribute, IObject entity)
        {
            Vector3 pos = ((IEntity)entity).transform.position;
            attribute.PrefabInstance = CreatePrefab(attribute.m_Prefab, pos, quaternion.identity);
        }
        //public void OnProxyCreated(AttributeBase attribute, IEntity entity)
        //{
        //    "in".ToLog();
        //}
    }

    public sealed class TurnPlayerAttribute : AttributeBase, ITurnPlayer
    {
        [JsonProperty(Order = 0, PropertyName = "ActivateOnCreate")] private bool m_ActivateOnCreate = true;
        [JsonProperty(Order = 1, PropertyName = "TurnSpeed")] private float m_TurnSpeed = 0;
        [JsonProperty(Order = 2, PropertyName = "MaxActionPoint")] private int m_MaxActionPoint = 6;

        [JsonIgnore] private int m_CurrentActionPoint = 6;

        [JsonIgnore] public bool ActivateOnCreate => m_ActivateOnCreate;
        [JsonIgnore] public string DisplayName => Name;
        [JsonIgnore] public float TurnSpeed => m_TurnSpeed;
        [JsonIgnore] public bool ActivateTurn { get; set; }
        [JsonIgnore] public int MaxActionPoint => m_MaxActionPoint;
        [JsonIgnore] public int ActionPoint
        {
            get => m_CurrentActionPoint;
            set => m_CurrentActionPoint = value;
        }

        public void StartTurn()
        {
            CoreSystem.Logger.Log(Channel.Entity, $"{Name} turn start");
        }
        public void EndTurn()
        {
            CoreSystem.Logger.Log(Channel.Entity, $"{Name} turn end");
        }
        public void ResetTurnTable()
        {
            m_CurrentActionPoint = m_MaxActionPoint;

            CoreSystem.Logger.Log(Channel.Entity, $"{Name} reset turn");
        }

        public void SetMaxActionPoint(int ap) => m_MaxActionPoint = ap;
        public int UseActionPoint(int ap) => m_CurrentActionPoint -= ap;

        [System.Obsolete("", true)]
        public IReadOnlyList<int2> GetMoveableCells()
        {
            IEntity parent = (IEntity)Parent;

            ref GridManager.GridCell cell = ref parent.GetCurrentCell();
            return TurnTableManager.GetMoveableCells(in cell, ActionPoint);
        }
    }
    [Preserve]
    internal sealed class TurnPlayerProcessor : AttributeProcessor<TurnPlayerAttribute>
    {
        protected override void OnCreated(TurnPlayerAttribute attribute, IObject entity)
        {
            attribute.ActivateTurn = attribute.ActivateOnCreate;
            TurnTableManager.AddPlayer(attribute);
        }
        protected override void OnDestroy(TurnPlayerAttribute attribute, IObject entity)
        {
            TurnTableManager.RemovePlayer(attribute);
        }
    }

    public sealed class CreatureStatAttribute : AttributeBase
    {
        [JsonProperty(Order = 0, PropertyName = "Stats")] public ValuePairContainer m_Stats;
    }
    //[Preserve]
    //internal sealed class CreatureStatProcessor : AttributeProcessor<CreatureStatAttribute>, IAttributeOnProxyCreatedSync
    //{
    //    public void OnProxyCreatedSync(AttributeBase attribute, IEntity entity)
    //    {
    //        //CreatureBrain brain = (CreatureBrain)entity.gameObject.GetProxyObject();

    //        //CreatureStat stat = brain.Stat;
    //        //if (stat == null)
    //        //{
    //        //    stat = brain.gameObject.AddComponent<CreatureStat>();
    //        //    brain.InitializeCreatureEntity(stat);
    //        //}

    //        //stat.Values = ((CreatureStatAttribute)attribute).m_Stats;
    //    }
    //}

    [ReflectionDescription(
        "이 어트리뷰트가 달린 Entity가 생성될 때, 같이 생성될 Entity 를 설정할 수 있습니다."
        )]
    public sealed class CreateEntityAttribute : AttributeBase
    {
        [JsonProperty(Order = 0, PropertyName = "Entity")] public Reference<EntityBase> m_Entity;
        
        [JsonIgnore] public IEntity CreatedEntity { get; internal set; }
    }
    [Preserve]
    internal sealed class CreateEntityProcessor : AttributeProcessor<CreateEntityAttribute>
    {
        protected override void OnCreated(CreateEntityAttribute attribute, IObject entity)
        {
            DataTransform tr = ((IEntity)entity).transform;
            attribute.CreatedEntity = CreateEntity(attribute.m_Entity, tr.position, tr.rotation);
        }
    }

    public sealed class TestAttribute : AttributeBase
    {
        public string[] m_TestString;
        public int[] m_TestInt;
        public bool[] m_TestBoolen;
        public Reference<EntityBase>[] m_TestEntityList;
    }
}
