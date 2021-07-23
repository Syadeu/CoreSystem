using Newtonsoft.Json;
using Unity.Mathematics;

namespace Syadeu.Presentation
{
    public abstract class MapBase : ObjectBase
    {
        public class Object
        {
            [JsonProperty(PropertyName = "Object")] public Reference<EntityBase> m_Object;
            [JsonProperty] public float3 m_Translation;
            [JsonProperty] public quaternion m_Rotation;
            [JsonProperty] public float3 m_Scale;

            public int testInt;
            public string testString;
        }

        [JsonProperty(Order = 0, PropertyName = "Objects")] public Object[] m_Objects;
        public Reference<EntityBase> m_Object;
    }
    public sealed class TestMap : MapBase
    {

    }
    //public sealed class TestMapProcessor : 
}
