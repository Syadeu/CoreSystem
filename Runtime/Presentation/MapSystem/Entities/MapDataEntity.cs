using Newtonsoft.Json;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using Syadeu.ThreadSafe;
using Unity.Mathematics;

namespace Syadeu.Presentation.Map
{
    [EntityAcceptOnly(typeof(MapDataAttributeBase))]
    public sealed class MapDataEntity : EntityDataBase
    {
        public class Object
        {
            [JsonProperty(Order = 0, PropertyName = "Object")] public Reference<EntityBase> m_Object;
            [JsonProperty(Order = 1, PropertyName = "Translation")] public float3 m_Translation;
            [JsonProperty(Order = 2, PropertyName = "Rotation")] public quaternion m_Rotation;
            [JsonProperty(Order = 3, PropertyName = "Scale")] public float3 m_Scale;
            [JsonProperty(Order = 4, PropertyName = "EnableCull")] public bool m_EnableCull;

            [JsonIgnore] public float3 eulerAngles
            {
                get => m_Rotation.Euler().ToThreadSafe() * UnityEngine.Mathf.Rad2Deg;
                set
                {
                    Vector3 temp = new Vector3(value.x * UnityEngine.Mathf.Deg2Rad, value.y * UnityEngine.Mathf.Deg2Rad, value.z * UnityEngine.Mathf.Deg2Rad);
                    m_Rotation = quaternion.EulerZXY(temp);
                }
            }

            public Object()
            {
                m_Rotation = quaternion.identity;
                m_EnableCull = true;
            }
        }

        [JsonProperty(Order = 0, PropertyName = "Objects")] public Object[] m_Objects;

        [JsonIgnore] public IEntity[] CreatedEntities { get; internal set; }
        [JsonIgnore] public bool DestroyChildOnDestroy { get; set; } = true;

        public override bool IsValid()
        {
            return true;

        }
    }
    public sealed class MapDataProcessor : EntityDataProcessor<MapDataEntity>
    {
        protected override void OnCreated(MapDataEntity entity)
        {
            entity.CreatedEntities = new IEntity[entity.m_Objects.Length];
            for (int i = 0; i < entity.m_Objects.Length; i++)
            {
                entity.CreatedEntities[i] = CreateEntity(entity.m_Objects[i].m_Object, entity.m_Objects[i].m_Translation, entity.m_Objects[i].m_Rotation, entity.m_Objects[i].m_Scale, entity.m_Objects[i].m_EnableCull);
            }
        }
        protected override void OnDestroy(MapDataEntity entity)
        {
            if (!entity.DestroyChildOnDestroy) return;

            for (int i = 0; i < entity.CreatedEntities.Length; i++)
            {
                if (entity.CreatedEntities[i].IsValid())
                {
                    entity.CreatedEntities[i].Destroy();
                }
            }
        }
    }

    [AttributeAcceptOnly(typeof(MapDataEntity))]
    public abstract class MapDataAttributeBase : AttributeBase { }

    
}
