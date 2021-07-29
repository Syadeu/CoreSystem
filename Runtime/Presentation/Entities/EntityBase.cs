using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Presentation.Attributes;
using Unity.Mathematics;

namespace Syadeu.Presentation.Entities
{
    /// <summary><inheritdoc cref="IEntity"/></summary>
    /// <remarks>
    /// 이 클래스를 상속받음으로서 새로운 오브젝트를 선언할 수 있습니다.<br/>
    /// 선언된 클래스는 <seealso cref="EntityDataList"/>에 자동으로 타입이 등록되어 추가할 수 있게 됩니다.
    /// </remarks>
    public abstract class EntityBase : EntityDataBase, IEntity
    {
        [JsonIgnore] internal Hash m_GameObjectHash;
        [JsonIgnore] internal Hash m_TransformHash;

        [JsonProperty(Order = -9, PropertyName = "Prefab")] public PrefabReference Prefab { get; set; }

        [JsonIgnore] public DataGameObject gameObject => PresentationSystem<GameObjectProxySystem>.System.GetDataGameObject(m_GameObjectHash);
        [JsonIgnore] public DataTransform transform => PresentationSystem<GameObjectProxySystem>.System.GetDataTransform(m_TransformHash);

        public override bool IsValid()
        {
            if (m_GameObjectHash.Equals(Hash.Empty) || m_TransformHash.Equals(Hash.Empty) || !m_IsCreated || PresentationSystem<GameObjectProxySystem>.System.Disposed) return false;

            return true;
        }

        public sealed class Captured
        {
            public float3 m_Translation;
            public quaternion m_Rotation;
            public float3 m_Scale;
            public bool m_EnableCull;
            public EntityBase m_Obj;
            public AttributeBase[] m_Atts;
        }
        public Captured Capture()
        {
            DataTransform tr = transform;
            EntityBase clone = (EntityBase)Clone();
            Captured payload = new Captured
            {
                m_Translation = tr.m_Position,
                m_Rotation = tr.m_Rotation,
                m_Scale = tr.m_LocalScale,
                m_EnableCull = tr.m_EnableCull,
                m_Obj = clone,
                m_Atts = clone.m_Attributes.ToArray()
            };

            return payload;
        }
    }
}
