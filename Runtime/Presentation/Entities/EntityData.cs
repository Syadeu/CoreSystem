#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using Syadeu.Internal;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Components;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;

namespace Syadeu.Presentation.Entities
{
    [Serializable, StructLayout(LayoutKind.Sequential)]
    /// <summary><inheritdoc cref="IEntityData"/></summary>
    /// <remarks>
    /// 사용자가 <see cref="EntityDataBase"/>를 좀 더 안전하게 접근할 수 있도록 랩핑하는 struct 입니다.<br/>
    /// 이 struct 는 이미 생성된 엔티티만 담습니다. Raw 데이터 접근은 허용하지 않습니다.<br/>
    /// <br/>
    /// <seealso cref="IEntityData"/>, <seealso cref="EntityDataBase"/>를 상속받는 타입이라면 얼마든지 해당 타입으로 형변환이 가능합니다.<br/>
    /// <see cref="EntityBase"/>는 <seealso cref="Entity{T}"/>를 참조하세요.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public struct EntityData<T> : IEntityDataID, IValidation, IEquatable<EntityData<T>>, IEquatable<EntityID> 
        where T : class, IEntityData
    {
        private const string c_Invalid = "Invalid";

        public static readonly EntityData<T> Empty = new EntityData<T>(Hash.Empty, 0, null);

        public static EntityData<T> GetEntity(InstanceID id) => EntityDataHelper.GetEntity<T>(id);
        public static EntityData<T> GetEntityWithoutCheck(InstanceID id) => EntityDataHelper.GetEntityWithoutCheck<T>(in id);

        /// <inheritdoc cref="IEntityData.Idx"/>
        private readonly EntityID m_Idx;
        private readonly int m_HashCode;
        private FixedString128Bytes m_Name;

        IEntityData IEntityDataID.Target => Target;
        public T Target
        {
            get
            {
#if DEBUG_MODE
                if (IsEmpty())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        "An empty entity reference trying to access transform.");
                    return null;
                }
#endif
                ObjectBase value = PresentationSystem<DefaultPresentationGroup, EntitySystem>.System.GetEntityByID(Idx);
                if (value == null)
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"Destroyed entity.");
                    return null;
                }

                if (!(value is T t))
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"Entity validation error. This entity is not an {TypeHelper.TypeOf<T>.ToString()} but {TypeHelper.ToString(value?.GetType())}.");
                    return null;
                }

                if (!CoreSystem.BlockCreateInstance &&
                    PresentationSystem<DefaultPresentationGroup, EntitySystem>.System.IsMarkedAsDestroyed(m_Idx))
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"Accessing entity({value.Name}) that will be destroy in the next frame.");
                }

                return t;
            }
        }

        public FixedString128Bytes RawName => m_Name;
        /// <inheritdoc cref="IEntityData.Name"/>
        public string Name => m_Idx.IsEmpty() ? c_Invalid : Target.Name;
        /// <inheritdoc cref="IEntityData.Hash"/>
        public Hash Hash => Target.Hash;
        /// <inheritdoc cref="IEntityData.Idx"/>
        public EntityID Idx => m_Idx;
        public Type Type => m_Idx.IsEmpty() ? null : Target?.GetType();

        internal EntityData(InstanceID id, int hashCode, string name)
        {
            m_Idx = id.Hash;
            if (string.IsNullOrEmpty(name))
            {
                m_Name = default(FixedString128Bytes);
            }
            else m_Name = name;

            m_HashCode = hashCode;
        }
        internal EntityData(Hash idx, int hashCode, string name)
        {
            m_Idx = idx;
            if (string.IsNullOrEmpty(name))
            {
                m_Name = default(FixedString128Bytes);
            }
            else m_Name = name;

            m_HashCode = hashCode;
        }

        public bool IsEmpty() => Equals(Empty);
        public bool IsValid()
        {
            if (IsEmpty() || !Target.IsValid()) return false;

            EntitySystem system = PresentationSystem<DefaultPresentationGroup, EntitySystem>.System;

            return !system.IsDestroyed(m_Idx) &&
                !system.IsMarkedAsDestroyed(m_Idx);
        }

        public bool Equals(EntityData<T> other) => m_Idx.Equals(other.m_Idx);
        public bool Equals(EntityID other) => m_Idx.Equals(other);
        public bool Equals(IEntityDataID other) => m_Idx.Equals(other.Idx);

        public override int GetHashCode()
        {
#if DEBUG_MODE
            if (IsEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    "An empty entity reference trying to access transform.");
                return 0;
            }
#endif
            return m_HashCode;
            //if (s_EntitySystem.IsNull())
            //{
            //    s_EntitySystem = PresentationSystem<DefaultPresentationGroup, EntitySystem>.SystemID;
            //    if (s_EntitySystem.IsNull())
            //    {
            //        CoreSystem.Logger.LogError(Channel.Entity,
            //            "Cannot retrived EntitySystem.");
            //        return 0;
            //    }
            //}

            //if (s_EntitySystem.System.m_ObjectEntities.TryGetValue(m_Idx, out ObjectBase value))
            //{
            //    return value.GetHashCode();
            //}

            //CoreSystem.Logger.LogError(Channel.Entity,
            //        $"Destroyed entity({RawName}).");
            //return 0;
        }

        public static implicit operator T(EntityData<T> a) => a.Target;
        //public static implicit operator EntityData<IEntityData>(EntityData<T> a) => GetEntityData(a.m_Idx);
        //public static implicit operator EntityData<T>(Entity<T> a) => GetEntityData(a.m_Idx);
        public static implicit operator EntityData<T>(Hash a) => GetEntity(a);
        public static implicit operator EntityData<T>(T a)
        {
            if (a == null)
            {
                return Empty;
            }
            return GetEntity(a.Idx);
        }
        //public static implicit operator EntityData<T>(Instance<T> a)
        //{
        //    if (a.IsEmpty() || !a.IsValid()) return Empty;
        //    return GetEntityWithoutCheck(a.Idx);
        //}
    }
}
