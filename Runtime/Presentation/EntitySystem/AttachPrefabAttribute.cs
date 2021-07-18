using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Mono;
using Syadeu.Mono.TurnTable;
using Syadeu.ThreadSafe;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.AI;
using UnityEngine.Scripting;

namespace Syadeu.Presentation
{
    public sealed class AttachPrefabAttribute : AttributeBase
    {
        [JsonProperty(Order = 0, PropertyName = "Prefab")] public PrefabReference m_Prefab;

        [JsonIgnore] public DataGameObject PrefabInstance { get; internal set; }
    }
    [Preserve]
    internal sealed class AttachPrefabProcessor : AttributeProcessor<AttachPrefabAttribute>
    {
        protected override void OnCreated(AttachPrefabAttribute attribute, IEntity entity)
        {
            Vector3 pos = entity.transform.position;
            attribute.PrefabInstance = CreatePrefab(attribute.m_Prefab, pos, quaternion.identity);
        }
        //public void OnProxyCreated(AttributeBase attribute, IEntity entity)
        //{
        //    "in".ToLog();
        //}
    }

    public sealed class CreatureBrainAttribute : AttributeBase
    {

        
    }
    [Preserve]
    internal sealed class CreatureBrainProcessor : AttributeProcessor<CreatureBrainAttribute>
    {
        protected override void OnCreated(CreatureBrainAttribute attribute, IEntity entity)
        {
            if (entity.transform.m_EnableCull) entity.transform.SetCulling(false);

            CreatureAttributeBase[] creatureAttributes = entity.GetAttributes<CreatureAttributeBase>();
            for (int i = 0; i < creatureAttributes.Length; i++)
            {
                creatureAttributes[i].InternalOnCreatureCreated(attribute, entity);
            }
        }
    }

    public abstract class CreatureAttributeBase : AttributeBase
    {
        internal void InternalOnCreatureCreated(CreatureBrainAttribute attribute, IEntity entity)
        {
            try
            {
                OnCreatureCreated(attribute, entity);
            }
            catch (System.Exception ex)
            {
                CoreSystem.Logger.LogError(Channel.Entity, $"An error raised during OnCreatureCreated at {entity.Name} : {GetType().Name}\n" + ex.Message);
            }
        }

        protected virtual void OnCreatureCreated(CreatureBrainAttribute attribute, IEntity entity) { }
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
    }
    [Preserve]
    internal sealed class TurnPlayerProcessor : AttributeProcessor<TurnPlayerAttribute>
    {
        protected override void OnCreated(TurnPlayerAttribute attribute, IEntity entity)
        {
            attribute.ActivateTurn = attribute.ActivateOnCreate;
            TurnTableManager.AddPlayer(attribute);
        }
        protected override void OnDestory(TurnPlayerAttribute attribute, IEntity entity)
        {
            TurnTableManager.RemovePlayer(attribute);
        }
    }

    public sealed class CreatureStatAttribute : CreatureAttributeBase
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
        [JsonProperty(Order = 0, PropertyName = "Entity")] public EntityReference m_Entity;

        [JsonIgnore] public IEntity CreatedEntity { get; internal set; }
    }
    [Preserve]
    internal sealed class CreateEntityProcessor : AttributeProcessor<CreateEntityAttribute>
    {
        protected override void OnCreated(CreateEntityAttribute attribute, IEntity entity)
        {
            DataTransform tr = entity.transform;
            attribute.CreatedEntity = CreateEntity(attribute.m_Entity, tr.position, tr.rotation);
        }
    }
    public sealed class ObjectEntity : EntityBase
    {

    }
}
