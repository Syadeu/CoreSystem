using Newtonsoft.Json;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Proxy;
using System;
using Unity.Mathematics;
using UnityEngine;
using AABB = Syadeu.Collections.AABB;

namespace Syadeu.Presentation.Map
{
    public abstract class MapDataEntityBase : EntityDataBase
    {
        [Serializable]
        public class Object : ICloneable
        {
            [JsonProperty(Order = 0, PropertyName = "Object")] public Reference<EntityBase> m_Object;
            [JsonProperty(Order = 1, PropertyName = "Translation")] public float3 m_Translation;
            [JsonProperty(Order = 2, PropertyName = "Rotation")] public quaternion m_Rotation = quaternion.identity;
            [JsonProperty(Order = 3, PropertyName = "Scale")] public float3 m_Scale;

            [Space]
            [JsonProperty(Order = 4, PropertyName = "Static")]
            public bool m_Static = false;

            [JsonIgnore] public AABB aabb
            {
                get
                {
                    EntityBase entity = m_Object.GetObject();
                    return new AABB(entity.Center + m_Translation, entity.Size).Rotation(in m_Rotation, in m_Scale);
                }
            }
            [JsonIgnore] public TRS trs => new TRS(m_Translation, m_Rotation, m_Scale);

            public Object()
            {
                m_Rotation = quaternion.identity;
            }
            public object Clone() => MemberwiseClone();
        }

        [JsonProperty(Order = 0, PropertyName = "Center")] public float3 m_Center = float3.zero;
        [JsonProperty(Order = 1, PropertyName = "Objects")] public Object[] m_Objects = Array.Empty<Object>();
    }
}
