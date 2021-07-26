using Newtonsoft.Json;
using Syadeu.ThreadSafe;
using Unity.Mathematics;

namespace Syadeu.Presentation
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

            public Object()
            {
                m_Rotation = quaternion.identity;
            }
        }

        [JsonProperty(Order = 0, PropertyName = "Objects")] public Object[] m_Objects;

        public override bool IsValid()
        {
            return true;

        }
    }
    public sealed class TestMapProcessor : EntityDataProcessor<MapDataEntity>
    {
        protected override void OnCreated(MapDataEntity entity)
        {
            "entity in".ToLog();
            //CreateEntity(entity.m_Object, Vector3.Zero, quaternion.identity);
        }
    }

    [AttributeAcceptOnly(typeof(MapDataEntity))]
    public abstract class MapDataAttributeBase : AttributeBase { }

    public sealed class MapGridAttribute : MapDataAttributeBase
    {
        [JsonProperty(Order = 0, PropertyName = "Center")] public int3 m_Center;
        [JsonProperty(Order = 1, PropertyName = "Size")] public int3 m_Size;
    }
}
