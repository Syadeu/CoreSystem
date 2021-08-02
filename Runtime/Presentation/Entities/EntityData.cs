using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Attributes;
using System;
using System.Collections.Generic;

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
    public struct EntityData<T> : IValidation, IEquatable<EntityData<T>>, IEquatable<Hash> where T : class, IEntityData
    {
        public static EntityData<T> Empty => new EntityData<T>(Hash.Empty);

        private static readonly Dictionary<Hash, EntityData<T>> m_EntityData = new Dictionary<Hash, EntityData<T>>();
        public static EntityData<T> GetEntityData(Hash idx)
        {
            if (!m_EntityData.TryGetValue(idx, out var value))
            {
                value = new EntityData<T>(idx);
                m_EntityData.Add(idx, value);
            }
            return value;
        }

        /// <inheritdoc cref="IEntityData.Idx"/>
        private readonly Hash m_Idx;

        public T Target => m_Idx.Equals(Hash.Empty) ? null : (T)PresentationSystem<EntitySystem>.System.m_ObjectEntities[m_Idx];

        public string Name => Target.Name;
        public Hash Idx => m_Idx;
        public Type Type => Target.GetType();

        private EntityData(Hash idx)
        {
            m_Idx = idx;
        }

        public bool IsValid() => !m_Idx.Equals(Hash.Empty) && 
            PresentationSystem<EntitySystem>.IsValid() &&
            PresentationSystem<EntitySystem>.System.m_ObjectHashSet.Contains(m_Idx);

        public bool Equals(EntityData<T> other) => m_Idx.Equals(other.m_Idx);
        public bool Equals(Hash other) => m_Idx.Equals(other);

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
            if (a.Target is T) return EntityData<T>.GetEntityData(a.m_Idx);

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
            if (target is T) return EntityData<T>.GetEntityData(a);

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
    public struct Entity<T> : IValidation, IEquatable<Entity<T>>, IEquatable<Hash> where T : class, IEntity
    {
        public static Entity<T> Empty => new Entity<T>(Hash.Empty);

        private static readonly Dictionary<Hash, Entity<T>> m_Entity = new Dictionary<Hash, Entity<T>>();
        public static Entity<T> GetEntity(Hash idx)
        {
            if (m_Entity.Count > 2048) m_Entity.Clear();

            if (!m_Entity.TryGetValue(idx, out var value))
            {
                value = new Entity<T>(idx);
                m_Entity.Add(idx, value);
            }
            return value;
        }

        /// <inheritdoc cref="IEntityData.Idx"/>
        private readonly Hash m_Idx;

        public T Target => m_Idx.Equals(Hash.Empty) ? null : (T)PresentationSystem<EntitySystem>.System.m_ObjectEntities[m_Idx];

        public string Name => Target.Name;
        public Hash Idx => m_Idx;
        public Type Type => Target.GetType();

#pragma warning disable IDE1006 // Naming Styles
        public DataGameObject gameObject => Target.gameObject;
        public DataTransform transform => Target.transform;
#pragma warning restore IDE1006 // Naming Styles

        private Entity(Hash idx)
        {
            m_Idx = idx;
        }

        public bool IsValid() => !m_Idx.Equals(Hash.Empty) &&
            PresentationSystem<EntitySystem>.IsValid() &&
            PresentationSystem<EntitySystem>.System.m_ObjectHashSet.Contains(m_Idx);

        public bool Equals(Entity<T> other) => m_Idx.Equals(other.m_Idx);
        public bool Equals(Hash other) => m_Idx.Equals(other);

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
            if (a.Target is T) return Entity<T>.GetEntity(a.m_Idx);

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
            if (target is T) return Entity<T>.GetEntity(a);

            CoreSystem.Logger.LogError(Channel.Entity,
                $"Entity({target.Name}) is not a {TypeHelper.TypeOf<T>.Name}. This is an invalid operation and not allowed.");
            return Empty;
        }
        public static implicit operator Entity<T>(EntityData<IEntityData> a)
        {
            if (a.Target is T) return Entity<T>.GetEntity(a.Idx);

            CoreSystem.Logger.LogError(Channel.Entity,
                $"Entity({a.Name}) is not a {TypeHelper.TypeOf<T>.Name}. This is an invalid operation and not allowed.");
            return Empty;
        }
    }
}
