using JetBrains.Annotations;
using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Entities;
using Syadeu.ThreadSafe;
using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.AI;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Attributes
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
        
        [JsonIgnore] public Entity<EntityBase> CreatedEntity { get; internal set; }
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
