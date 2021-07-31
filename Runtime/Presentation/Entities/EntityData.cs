using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Attributes;
using System;

namespace Syadeu.Presentation.Entities
{
    /// <summary>
    /// 사용자가 <see cref="EntityDataBase"/>를 좀 더 안전하게 접근할 수 있도록 랩핑하는 struct 입니다.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct EntityData<T> : IValidation, IEquatable<EntityData<T>>, IEquatable<T> where T : class, IObject
    {
        public static EntityData<T> Empty => new EntityData<T>(Hash.Empty);
        private readonly Hash m_Idx;

        public T Target => m_Idx.Equals(Hash.Empty) ? null : (T)PresentationSystem<EntitySystem>.System.m_ObjectEntities[m_Idx];

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

        /// <inheritdoc cref="IObject.GetAttribute(Type)"/>
        public AttributeBase GetAttribute(Type t) => Target.GetAttribute(t);
        /// <inheritdoc cref="IObject.GetAttributes(Type)"/>
        public AttributeBase[] GetAttributes(Type t) => Target.GetAttributes(t);
        /// <inheritdoc cref="IObject.GetAttribute(Type)"/>
        public TA GetAttribute<TA>() where TA : AttributeBase => Target.GetAttribute<TA>();
        /// <inheritdoc cref="IObject.GetAttributes(Type)"/>
        public TA[] GetAttributes<TA>() where TA : AttributeBase => Target.GetAttributes<TA>();

        public void Destroy() => PresentationSystem<EntitySystem>.System.DestroyObject(m_Idx);

        public static implicit operator T(EntityData<T> a) => (T)a.Target;
        public static implicit operator EntityData<T>(EntityData<IObject> a)
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
            IObject target = PresentationSystem<EntitySystem>.System.m_ObjectEntities[a];
            if (target is T) return new EntityData<T>(a);

            CoreSystem.Logger.LogError(Channel.Entity,
                $"Entity({target.Name}) is not a {TypeHelper.TypeOf<T>.Name}. This is an invalid operation and not allowed.");
            return Empty;
        }
    }
    /// <summary>
    /// 사용자가 <see cref="EntityBase"/>를 좀 더 안전하게 접근할 수 있도록 랩핑하는 struct 입니다.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct Entity<T> : IValidation, IEquatable<Entity<T>>, IEquatable<T> where T : class, IEntity
    {
        public static Entity<T> Empty => new Entity<T>(Hash.Empty);
        private readonly Hash m_Idx;

        public T Target => m_Idx.Equals(Hash.Empty) ? null : (T)PresentationSystem<EntitySystem>.System.m_ObjectEntities[m_Idx];

        public string Name => Target.Name;

        internal Entity(Hash idx)
        {
            m_Idx = idx;
        }

        public bool IsValid() => !m_Idx.Equals(Hash.Empty) &&
            PresentationSystem<EntitySystem>.IsValid() &&
            PresentationSystem<EntitySystem>.System.m_ObjectHashSet.Contains(m_Idx);

        public bool Equals(Entity<T> other) => m_Idx.Equals(other.m_Idx);
        public bool Equals(T other) => m_Idx.Equals(other.Idx);

        /// <inheritdoc cref="IObject.GetAttribute(Type)"/>
        public AttributeBase GetAttribute(Type t) => Target.GetAttribute(t);
        /// <inheritdoc cref="IObject.GetAttributes(Type)"/>
        public AttributeBase[] GetAttributes(Type t) => Target.GetAttributes(t);
        /// <inheritdoc cref="IObject.GetAttribute(Type)"/>
        public TA GetAttribute<TA>() where TA : AttributeBase => Target.GetAttribute<TA>();
        /// <inheritdoc cref="IObject.GetAttributes(Type)"/>
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

            IObject target = PresentationSystem<EntitySystem>.System.m_ObjectEntities[a];
            if (target is T) return new Entity<T>(a);

            CoreSystem.Logger.LogError(Channel.Entity,
                $"Entity({target.Name}) is not a {TypeHelper.TypeOf<T>.Name}. This is an invalid operation and not allowed.");
            return Empty;
        }
    }
}
