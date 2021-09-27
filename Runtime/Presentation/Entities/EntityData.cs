using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Components;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Burst;

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
    public readonly struct EntityData<T> : IValidation, IEquatable<EntityData<T>>, IEquatable<Hash> where T : class, IEntityData
    {
        private const string c_Invalid = "Invalid";
        private static PresentationSystemID<EntitySystem> s_EntitySystem = PresentationSystemID<EntitySystem>.Null;
        internal static PresentationSystemID<EntityComponentSystem> s_ComponentSystem = PresentationSystemID<EntityComponentSystem>.Null;

        public static EntityData<T> Empty => new EntityData<T>(Hash.Empty);

        public static EntityData<T> GetEntity(Hash idx)
        {
            #region Validation
            if (idx.Equals(Hash.Empty))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                $"Cannot convert an empty hash to Entity. This is an invalid operation and not allowed.");
                return Empty;
            }
            if (!PresentationSystem<EntitySystem>.System.m_ObjectEntities.ContainsKey(idx))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot found entity({idx})");
                return Empty;
            }
            ObjectBase target = PresentationSystem<EntitySystem>.System.m_ObjectEntities[idx];
            if (!(target is T))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                $"Entity({target.Name}) is not a {TypeHelper.TypeOf<T>.Name}. This is an invalid operation and not allowed.");
                return Empty;
            }
            #endregion

            return new EntityData<T>(idx);
        }
        public static EntityData<T> GetEntityWithoutCheck(Hash idx)
        {
            return new EntityData<T>(idx);
        }

        /// <inheritdoc cref="IEntityData.Idx"/>
        private readonly Hash m_Idx;

        public T Target
        {
            get
            {
                if (IsEmpty())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        "An empty entity reference trying to access transform.");
                    return null;
                }

                if (s_EntitySystem.IsNull())
                {
                    s_EntitySystem = PresentationSystem<EntitySystem>.SystemID;
                    if (s_EntitySystem.IsNull())
                    {
                        CoreSystem.Logger.LogError(Channel.Entity,
                            "Cannot retrived EntitySystem.");
                        return null;
                    }
                }

                if (!s_EntitySystem.System.m_ObjectEntities.TryGetValue(m_Idx, out ObjectBase value))
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
                    s_EntitySystem.System.IsMarkedAsDestroyed(m_Idx))
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"Accessing entity({value.Name}) that will be destroy in the next frame.");
                }

                return t;
            }
        }

        /// <inheritdoc cref="IEntityData.Name"/>
        public string Name => m_Idx.Equals(Hash.Empty) ? c_Invalid : Target.Name;
        /// <inheritdoc cref="IEntityData.Hash"/>
        public Hash Hash => Target.Hash;
        /// <inheritdoc cref="IEntityData.Idx"/>
        public Hash Idx => m_Idx;
        public Type Type => m_Idx.Equals(Hash.Empty) ? null : Target?.GetType();

        internal EntityData(Hash idx)
        {
            m_Idx = idx;
        }

        public bool IsEmpty() => Equals(Empty);
        public bool IsValid()
        {
            if (IsEmpty()) return false;

            if (s_EntitySystem.IsNull())
            {
                s_EntitySystem = PresentationSystem<EntitySystem>.SystemID;
                if (s_EntitySystem.IsNull())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        "Cannot retrived EntitySystem.");
                    return false;
                }
            }
            else if (!s_EntitySystem.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                        "Cannot retrived EntitySystem. The system has been destroyed.");
                return false;
            }

            return !s_EntitySystem.System.IsDestroyed(m_Idx);
        }

        public bool Equals(EntityData<T> other) => m_Idx.Equals(other.m_Idx);
        public bool Equals(Hash other) => m_Idx.Equals(other);

        #region Attributes

        /// <inheritdoc cref="IEntityData.HasAttribute(Hash)"/>
        public bool HasAttribute(Hash attributeHash)
        {
            if (!IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");
                return false;
            }

            return Target.HasAttribute(attributeHash);
        }
        public bool HasAttribute(Type t)
        {
            if (!IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");
                return false;
            }

            return Target.HasAttribute(t);
        }
        public bool HasAttribute<TA>() where TA : AttributeBase
        {
            if (!IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");
                return false;
            }

            return Target.HasAttribute<TA>();
        }
        /// <inheritdoc cref="IEntityData.GetAttribute(Type)"/>
        public AttributeBase GetAttribute(Type t)
        {
            if (!IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");
                return null;
            }

            return Target.GetAttribute(t);
        }
        /// <inheritdoc cref="IEntityData.GetAttributes(Type)"/>
        public AttributeBase[] GetAttributes(Type t)
        {
            if (!IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");
                return null;
            }

            return Target.GetAttributes(t);
        }
        /// <inheritdoc cref="IEntityData.GetAttribute(Type)"/>
        public TA GetAttribute<TA>() where TA : AttributeBase
        {
            if (!IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");
                return null;
            }

            return Target.GetAttribute<TA>();
        }
        /// <inheritdoc cref="IEntityData.GetAttributes(Type)"/>
        public TA[] GetAttributes<TA>() where TA : AttributeBase
        {
            if (!IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");
                return null;
            }

            return Target.GetAttributes<TA>();
        }

        #endregion

        #region Components

        public TComponent AddComponent<TComponent>(TComponent data)
            where TComponent : unmanaged, IEntityComponent
        {
            if (!IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");
                return default(TComponent);
            }

            if (s_ComponentSystem.IsNull())
            {
                s_ComponentSystem = SharedStatic<EntityComponentConstrains>.GetOrCreate<EntityComponentSystem>().Data.SystemID;
                if (s_ComponentSystem.IsNull())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"Cannot retrived {nameof(EntityComponentSystem)}.");
                    return default(TComponent);
                }
            }

            return s_ComponentSystem.System.AddComponent(EntityData<IEntityData>.GetEntityWithoutCheck(m_Idx), data);
        }
        public bool HasComponent<TComponent>()
            where TComponent : unmanaged, IEntityComponent
        {
            if (!IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");
                return false;
            }

            if (s_ComponentSystem.IsNull())
            {
                s_ComponentSystem = SharedStatic<EntityComponentConstrains>.GetOrCreate<EntityComponentSystem>().Data.SystemID;
                if (s_ComponentSystem.IsNull())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"Cannot retrived {nameof(EntityComponentSystem)}.");
                    return false;
                }
            }

            return s_ComponentSystem.System.HasComponent<TComponent>(EntityData<IEntityData>.GetEntityWithoutCheck(m_Idx));
        }
        public bool HasComponent(Type componentType)
        {
            if (!IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");
                return false;
            }

            if (s_ComponentSystem.IsNull())
            {
                s_ComponentSystem = SharedStatic<EntityComponentConstrains>.GetOrCreate<EntityComponentSystem>().Data.SystemID;
                if (s_ComponentSystem.IsNull())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"Cannot retrived {nameof(EntityComponentSystem)}.");
                    return false;
                }
            }

            return s_ComponentSystem.System.HasComponent(EntityData<IEntityData>.GetEntityWithoutCheck(m_Idx), componentType);
        }
        public TComponent GetComponent<TComponent>()
            where TComponent : unmanaged, IEntityComponent
        {
            if (!IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");
                return default(TComponent);
            }

            if (s_ComponentSystem.IsNull())
            {
                s_ComponentSystem = SharedStatic<EntityComponentConstrains>.GetOrCreate<EntityComponentSystem>().Data.SystemID;
                if (s_ComponentSystem.IsNull())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"Cannot retrived {nameof(EntityComponentSystem)}.");
                    return default(TComponent);
                }
            }

            return s_ComponentSystem.System.GetComponent<TComponent>(EntityData<IEntityData>.GetEntityWithoutCheck(m_Idx));
        }
        public void RemoveComponent<TComponent>()
            where TComponent : unmanaged, IEntityComponent
        {
            if (!IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");
                return;
            }

            if (s_ComponentSystem.IsNull())
            {
                s_ComponentSystem = SharedStatic<EntityComponentConstrains>.GetOrCreate<EntityComponentSystem>().Data.SystemID;
                if (s_ComponentSystem.IsNull())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"Cannot retrived {nameof(EntityComponentSystem)}.");
                    return;
                }
            }

            s_ComponentSystem.System.RemoveComponent<TComponent>(EntityData<IEntityData>.GetEntityWithoutCheck(m_Idx));
        }
        public void RemoveComponent(Type componentType)
        {
            if (!IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");
                return;
            }

            if (s_ComponentSystem.IsNull())
            {
                s_ComponentSystem = SharedStatic<EntityComponentConstrains>.GetOrCreate<EntityComponentSystem>().Data.SystemID;
                if (s_ComponentSystem.IsNull())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"Cannot retrived {nameof(EntityComponentSystem)}.");
                    return;
                }
            }

            s_ComponentSystem.System.RemoveComponent(EntityData<IEntityData>.GetEntityWithoutCheck(m_Idx), componentType);
        }

        #endregion

        public void Destroy()
        {
            if (s_EntitySystem.IsNull())
            {
                s_EntitySystem = PresentationSystem<EntitySystem>.SystemID;
                if (s_EntitySystem.IsNull())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        "Cannot retrived EntitySystem.");
                    return;
                }
            }

            s_EntitySystem.System.InternalDestroyEntity(m_Idx);
        }

        public override int GetHashCode()
        {
            if (IsEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    "An empty entity reference trying to access transform.");
                return 0;
            }

            if (s_EntitySystem.IsNull())
            {
                s_EntitySystem = PresentationSystem<EntitySystem>.SystemID;
                if (s_EntitySystem.IsNull())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        "Cannot retrived EntitySystem.");
                    return 0;
                }
            }

            if (!s_EntitySystem.System.m_ObjectEntities.TryGetValue(m_Idx, out ObjectBase value))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Destroyed entity.");
                return 0;
            }

            return value.GetHashCode();
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
        public static implicit operator EntityData<T>(Instance<T> a)
        {
            if (a.IsEmpty() || !a.IsValid()) return Empty;
            return GetEntityWithoutCheck(a.Idx);
        }
    }
}
