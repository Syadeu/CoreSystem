using JetBrains.Annotations;
using Newtonsoft.Json;
using Syadeu.Database;
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
        protected override void OnCreated(CreatePrefabAttribute attribute, EntityData<IEntityData> entity)
        {
            Vector3 pos = ((EntityData<IEntity>)entity).Target.transform.position;
            attribute.PrefabInstance = CreatePrefab(attribute.m_Prefab, pos, quaternion.identity);
        }
        //public void OnProxyCreated(AttributeBase attribute, IEntity entity)
        //{
        //    "in".ToLog();
        //}
    }
    

    public sealed class TestAttribute : AttributeBase
    {
        public string[] m_TestString;
        public int[] m_TestInt;
        public bool[] m_TestBoolen;
        public Reference<EntityBase>[] m_TestEntityList;
    }
}
