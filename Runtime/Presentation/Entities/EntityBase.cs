using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Attributes;
using Unity.Mathematics;

namespace Syadeu.Presentation.Entities
{
    /// <summary><inheritdoc cref="IEntity"/></summary>
    /// <remarks>
    /// class 맴버 선언을 그리 추천하고 싶지 않지만, 필요에 의해 선언이 내부에 되었다면,<br/>
    /// 해당 값을 복사하여 인스턴스를 만들기 위해 <see cref="ObjectBase.Copy"/>을 override 하여 해당 값을 복사하여야합니다.<br/>
    /// <br/>
    /// 이 클래스를 상속받음으로서 새로운 오브젝트를 선언할 수 있습니다.<br/>
    /// 선언된 클래스는 <seealso cref="EntityDataList"/>에 자동으로 타입이 등록되어 추가할 수 있게 됩니다.
    /// </remarks>
    public abstract class EntityBase : EntityDataBase, IEntity
    {
        /// <summary>
        /// 연결된 <see cref="DataGameObject"/>의 해쉬 값입니다.
        /// </summary>
        [JsonIgnore] internal Hash m_GameObjectHash;
        /// <summary>
        /// 연결된 <see cref="DataTransform"/>의 해쉬 값입니다.
        /// </summary>
        [JsonIgnore] internal Hash m_TransformHash;

        /// <summary>
        /// 이 엔티티의 Raw 프리팹 주소입니다.
        /// </summary>
        [JsonProperty(Order = -9, PropertyName = "Prefab")] public PrefabReference Prefab { get; set; }

        [ReflectionDescription("AABB 의 Center"), JsonProperty(Order = -8, PropertyName = "Center")] public float3 Center { get; set; }
        [ReflectionDescription("AABB 의 Size"), JsonProperty(Order = -7, PropertyName = "Size")] public float3 Size { get; set; }

        /// <summary>
        /// <see cref="GameObjectProxySystem"/>을 통해 연결된 <see cref="DataGameObject"/> 입니다.
        /// </summary>
        [JsonIgnore] public DataGameObject gameObject => PresentationSystem<GameObjectProxySystem>.System.GetDataGameObject(m_GameObjectHash);
        /// <summary>
        /// <see cref="GameObjectProxySystem"/>을 통해 연결된 <see cref="DataTransform"/> 입니다.
        /// </summary>
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
