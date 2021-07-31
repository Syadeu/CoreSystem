using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Attributes;
using System;

namespace Syadeu.Presentation.Entities
{
    /// <summary><inheritdoc cref="IEntityData"/></summary>
    /// <remarks>
    /// 사용자가 <see cref="EntityDataBase"/>를 좀 더 안전하게 접근할 수 있도록 랩핑하는 struct 입니다.<br/>
    /// 이 struct 는 이미 생성된 엔티티만 담습니다. Raw 데이터 접근은 허용하지 않습니다.<br/>
    /// <br/>
    /// <seealso cref="IEntityData"/>, <seealso cref="EntityDataBase"/>를 상속받는 타입이라면 얼마든지 해당 타입으로 형변환이 가능합니다.<br/>
    /// <see cref="EntityBase"/>는 <seealso cref="Entity{T}"/>를 참조하세요.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public struct EntityData<T> : IValidation, IEquatable<EntityData<T>>, IEquatable<T> where T : class, IEntityData
    {
        public static EntityData<T> Empty => new EntityData<T>(Hash.Empty);
        /// <inheritdoc cref="IEntityData.Idx"/>
        private readonly Hash m_Idx;

        internal T Target => m_Idx.Equals(Hash.Empty) ? null : (T)PresentationSystem<EntitySystem>.System.m_ObjectEntities[m_Idx];

        public string Name => Target.Name;

        internal EntityData(Hash idx)
        {
            m_Idx = idx;
        }

        public bool IsValid() => !m_Idx.Equals(Hash.Empty) && 
            PresentationSystem<EntitySystem>.IsValid() &&
            PresentationSystem<EntitySystem>.System.m_ObjectHashSet.Contains(m_Idx);

        public bool Equals(EntityData<T> other) => m_Idx.Equals(other.m_Idx);
        public bool Equals(T other) => m_Idx.Equals(other.Idx);

        /// <inheritdoc cref="IEntityData.GetAttribute(Type)"/>
        public AttributeBase GetAttribute(Type t) => Target.GetAttribute(t);
        /// <inheritdoc cref="IEntityData.GetAttributes(Type)"/>
        public AttributeBase[] GetAttributes(Type t) => Target.GetAttributes(t);
        /// <inheritdoc cref="IEntityData.GetAttribute(Type)"/>
        public TA GetAttribute<TA>() where TA : AttributeBase => Target.GetAttribute<TA>();
        /// <inheritdoc cref="IEntityData.GetAttributes(Type)"/>
        public TA[] GetAttributes<TA>() where TA : AttributeBase => Target.GetAttributes<TA>();

        public void Destroy() => PresentationSystem<EntitySystem>.System.DestroyObject(m_Idx);

        public static implicit operator T(EntityData<T> a) => (T)a.Target;
        public static implicit operator EntityData<T>(EntityData<IEntityData> a)
        {
            if (a.Target is T) return new EntityData<T>(a.m_Idx);

            CoreSystem.Logger.LogError(Channel.Entity,
                $"Entity({a.Name}) is not a {TypeHelper.TypeOf<T>.Name}. This is an invalid operation and not allowed.");
            return Empty;
        }
        public static implicit operator EntityData<T>(Hash a)
        {
            if (!PresentationSystem<EntitySystem>.System.m_ObjectHashSet.Contains(a))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot found entity({a})");
                return Empty;
            }
            IEntityData target = PresentationSystem<EntitySystem>.System.m_ObjectEntities[a];
            if (target is T) return new EntityData<T>(a);

            CoreSystem.Logger.LogError(Channel.Entity,
                $"Entity({target.Name}) is not a {TypeHelper.TypeOf<T>.Name}. This is an invalid operation and not allowed.");
            return Empty;
        }
    }
    /// <summary><inheritdoc cref="IEntity"/></summary>
    /// <remarks>
    /// 사용자가 <see cref="EntityBase"/>를 좀 더 안전하게 접근할 수 있도록 랩핑하는 struct 입니다.<br/>
    /// 이 struct 는 이미 생성된 엔티티만 담습니다. Raw 데이터 접근은 허용하지 않습니다.<br/>
    /// <br/>
    /// <seealso cref="IEntity"/>, <seealso cref="EntityBase"/>를 상속받는 타입이라면 얼마든지 해당 타입으로 형변환이 가능합니다.<br/>
    /// <see cref="EntityDataBase"/>는 <seealso cref="EntityData{T}"/>를 참조하세요.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public struct Entity<T> : IValidation, IEquatable<Entity<T>>, IEquatable<T> where T : class, IEntity
    {
        public static Entity<T> Empty => new Entity<T>(Hash.Empty);
        /// <inheritdoc cref="IEntityData.Idx"/>
        private readonly Hash m_Idx;

        internal T Target => m_Idx.Equals(Hash.Empty) ? null : (T)PresentationSystem<EntitySystem>.System.m_ObjectEntities[m_Idx];

        public string Name => Target.Name;

#pragma warning disable IDE1006 // Naming Styles
        public DataGameObject gameObject => Target.gameObject;
        public DataTransform transform => Target.transform;
#pragma warning restore IDE1006 // Naming Styles

        internal Entity(Hash idx)
        {
            m_Idx = idx;
        }

        public bool IsValid() => !m_Idx.Equals(Hash.Empty) &&
            PresentationSystem<EntitySystem>.IsValid() &&
            PresentationSystem<EntitySystem>.System.m_ObjectHashSet.Contains(m_Idx);

        public bool Equals(Entity<T> other) => m_Idx.Equals(other.m_Idx);
        public bool Equals(T other) => m_Idx.Equals(other.Idx);

        /// <inheritdoc cref="IEntityData.GetAttribute(Type)"/>
        public AttributeBase GetAttribute(Type t) => Target.GetAttribute(t);
        /// <inheritdoc cref="IEntityData.GetAttributes(Type)"/>
        public AttributeBase[] GetAttributes(Type t) => Target.GetAttributes(t);
        /// <inheritdoc cref="IEntityData.GetAttribute(Type)"/>
        public TA GetAttribute<TA>() where TA : AttributeBase => Target.GetAttribute<TA>();
        /// <inheritdoc cref="IEntityData.GetAttributes(Type)"/>
        public TA[] GetAttributes<TA>() where TA : AttributeBase => Target.GetAttributes<TA>();

        public void Destroy() => PresentationSystem<EntitySystem>.System.DestroyObject(m_Idx);

        public static implicit operator T(Entity<T> a) => a.Target;
        public static implicit operator Entity<T>(Entity<IEntity> a)
        {
            if (a.Target is T) return new Entity<T>(a.m_Idx);

            CoreSystem.Logger.LogError(Channel.Entity,
                $"Entity({a.Name}) is not a {TypeHelper.TypeOf<T>.Name}. This is an invalid operation and not allowed.");
            return Empty;
        }
        public static implicit operator Entity<T>(Hash a)
        {
            if (!PresentationSystem<EntitySystem>.System.m_ObjectHashSet.Contains(a))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot found entity({a})");
                return Empty;
            }

            IEntityData target = PresentationSystem<EntitySystem>.System.m_ObjectEntities[a];
            if (target is T) return new Entity<T>(a);

            CoreSystem.Logger.LogError(Channel.Entity,
                $"Entity({target.Name}) is not a {TypeHelper.TypeOf<T>.Name}. This is an invalid operation and not allowed.");
            return Empty;
        }
    }
}
