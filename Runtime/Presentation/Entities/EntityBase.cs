using Newtonsoft.Json;
using Newtonsoft.Json.Utilities;
using Syadeu.Collections;
using Syadeu.Collections.Proxy;
using Syadeu.Internal;
using Syadeu.Presentation.Proxy;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

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
        /// 이 엔티티의 Raw 프리팹 주소입니다.
        /// </summary>
        [JsonProperty(Order = -9, PropertyName = "Prefab")] public PrefabReference<GameObject> Prefab { get; set; } = PrefabReference<GameObject>.None;

        [JsonProperty(Order = -8, PropertyName = "StaticBatching")] public bool StaticBatching { get; set; } = false;

        [ReflectionDescription("AABB 의 Center")]
        [JsonProperty(Order = -7, PropertyName = "Center")] public float3 Center { get; set; }

        [ReflectionDescription("AABB 의 Size")]
        [JsonProperty(Order = -6, PropertyName = "Size")] public float3 Size { get; set; }

        [Space]
        [JsonProperty(Order = -5, PropertyName = "EnableCull")] public bool m_EnableCull = true;

        /// <summary>
        /// <see cref="GameObjectProxySystem"/>을 통해 연결된 <see cref="DataTransform"/> 입니다.
        /// </summary>
        [JsonIgnore] public ITransform transform { get; internal set; }

        public override bool IsValid()
        {
            if (Reserved || PresentationSystem<DefaultPresentationGroup, GameObjectProxySystem>.System.Disposed) return false;

            return true;
        }
        internal override void InternalReserve()
        {
            base.InternalReserve();

            transform = null;
        }

        [Preserve]
        static void AOTCodeGeneration()
        {
            AotHelper.EnsureType<Reference<EntityBase>>();
            AotHelper.EnsureList<Reference<EntityBase>>();

            AotHelper.EnsureType<Entity<EntityBase>>();
            AotHelper.EnsureList<Entity<EntityBase>>();

            AotHelper.EnsureType<EntityData<EntityBase>>();
            AotHelper.EnsureList<EntityData<EntityBase>>();

            AotHelper.EnsureList<EntityBase>();
        }
    }
}
