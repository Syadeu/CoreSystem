#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using Syadeu.Collections.Proxy;
using Syadeu.Internal;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Proxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.Entities
{
    [Serializable, StructLayout(LayoutKind.Sequential)]
    /// <summary><inheritdoc cref="IEntity"/></summary>
    /// <remarks>
    /// 사용자가 <see cref="EntityBase"/>를 좀 더 안전하게 접근할 수 있도록 랩핑하는 struct 입니다.<br/>
    /// 이 struct 는 이미 생성된 엔티티만 담습니다. Raw 데이터 접근은 허용하지 않습니다.<br/>
    /// <br/>
    /// <seealso cref="IEntity"/>, <seealso cref="EntityBase"/>를 상속받는 타입이라면 얼마든지 해당 타입으로 형변환이 가능합니다.<br/>
    /// <see cref="EntityDataBase"/>는 <seealso cref="EntityData{T}"/>를 참조하세요.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public struct Entity<T> : IValidation, IEquatable<Entity<T>>, IEquatable<EntityID> where T : class, IEntity
    {
        private const string c_Invalid = "Invalid";
        private static PresentationSystemID<EntitySystem> s_EntitySystem = PresentationSystemID<EntitySystem>.Null;

        public static Entity<T> Empty => new Entity<T>(Hash.Empty, null);

        public static Entity<T> GetEntity(in InstanceID id) => GetEntity(id.Hash);
        public static Entity<T> GetEntity(Hash idx)
        {
            #region Validation
#if DEBUG_MODE
            if (idx.Equals(Hash.Empty))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                $"Cannot convert an empty hash to Entity. This is an invalid operation and not allowed.");
                return Empty;
            }
#endif
            if (s_EntitySystem.IsNull())
            {
                s_EntitySystem = PresentationSystem<DefaultPresentationGroup, EntitySystem>.SystemID;
                if (s_EntitySystem.IsNull())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        "Cannot retrived EntitySystem.");
                    return Empty;
                }
            }
            if (!s_EntitySystem.System.m_ObjectEntities.ContainsKey(idx))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot found entity({idx})");
                return Empty;
            }
            ObjectBase target = s_EntitySystem.System.m_ObjectEntities[idx];
            if (!(target is T))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                $"Entity({target.Name}) is not a {TypeHelper.TypeOf<T>.Name}. This is an invalid operation and not allowed.");
                return Empty;
            }
            #endregion

            return new Entity<T>(idx, target.Name);
        }
        internal static Entity<T> GetEntityWithoutCheck(in InstanceID id) => GetEntityWithoutCheck(id.Hash);
        internal static Entity<T> GetEntityWithoutCheck(Hash idx)
        {
            if (s_EntitySystem.IsNull())
            {
                s_EntitySystem = PresentationSystem<DefaultPresentationGroup, EntitySystem>.SystemID;
                if (s_EntitySystem.IsNull())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        "Cannot retrived EntitySystem.");
                    return Empty;
                }
            }
            ObjectBase target = s_EntitySystem.System.m_ObjectEntities[idx];
            return new Entity<T>(idx, target.Name);
        }

        /// <inheritdoc cref="IEntityData.Idx"/>
        private readonly EntityID m_Idx;
        private FixedString128Bytes m_Name;

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
                if (s_EntitySystem.IsNull())
                {
                    s_EntitySystem = PresentationSystem<DefaultPresentationGroup, EntitySystem>.SystemID;
                    if (s_EntitySystem.IsNull())
                    {
                        CoreSystem.Logger.LogError(Channel.Entity,
                            "Cannot retrived EntitySystem.");
                        return null;
                    }
                }

                if (!s_EntitySystem.System.m_ObjectEntities.TryGetValue(m_Idx, out var value) ||
                    !(value is T t))
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"Entity validation error. This entity is not an {TypeHelper.TypeOf<T>.ToString()} but {TypeHelper.ToString(value.GetType())}.");
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

        public FixedString128Bytes RawName => m_Name;
        /// <inheritdoc cref="IEntityData.Name"/>
        public string Name => m_Idx.IsEmpty() ? c_Invalid : m_Name.ConvertToString();
        /// <inheritdoc cref="IEntityData.Hash"/>
        public Hash Hash => Target.Hash;
        /// <inheritdoc cref="IEntityData.Idx"/>
        public EntityID Idx => m_Idx;
        public Type Type => m_Idx.IsEmpty() ? null : Target.GetType();

#pragma warning disable IDE1006 // Naming Styles
        /// <inheritdoc cref="EntityBase.transform"/>
        public ITransform transform
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
                return Target.transform;
            }
        }
        public bool hasProxy
        {
            get
            {
#if DEBUG_MODE
                if (IsEmpty())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        "An empty entity reference trying to access.");
                    return false;
                }
                else if (!IsValid())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        "An invalid entity reference trying to access.");
                    return false;
                }
#endif
                if (transform is IUnityTransform) return true;
                else
                {
                    IProxyTransform tr = (IProxyTransform)transform;
                    return tr.hasProxy && !tr.hasProxyQueued;
                }
            }
        }
        public UnityEngine.Object proxy
        {
            get
            {
                if (transform is IUnityTransform unity) return unity.provider;

                IProxyTransform tr = (IProxyTransform)transform;
                return (UnityEngine.Object)tr.proxy;
            }
        }

#pragma warning restore IDE1006 // Naming Styles

        private Entity(Hash idx, string name)
        {
            m_Idx = idx;

            if (string.IsNullOrEmpty(name))
            {
                m_Name = default(FixedString128Bytes);
            }
            else m_Name = name;
        }

        public bool IsEmpty() => Equals(Empty);
        public bool IsValid()
        {
            if (IsEmpty()) return false;

            if (s_EntitySystem.IsNull())
            {
                s_EntitySystem = PresentationSystem<DefaultPresentationGroup, EntitySystem>.SystemID;
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

            return !s_EntitySystem.System.IsDestroyed(m_Idx) && 
                !s_EntitySystem.System.IsMarkedAsDestroyed(m_Idx);
        }

        public bool Equals(Entity<T> other) => m_Idx.Equals(other.m_Idx);
        public bool Equals(EntityID other) => m_Idx.Equals(other);

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
        public IAttribute GetAttribute(Type t)
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
        public IAttribute[] GetAttributes(Type t)
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

        /// <inheritdoc cref="EntityData{T}.AddComponent{TComponent}(TComponent)"/>
        public TComponent AddComponent<TComponent>(in TComponent data)
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (!IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
#endif
            if (EntityComponentSystem.Constants.SystemID.IsNull())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot retrived {nameof(EntityComponentSystem)}.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }

            EntityData<IEntityData> entity = EntityData<IEntityData>.GetEntityWithoutCheck(m_Idx);
#if DEBUG_MODE
            s_EntitySystem.System.Debug_AddComponent<TComponent>(entity);
#endif
            return EntityComponentSystem.Constants.SystemID.System.AddComponent(entity, in data);
        }
        /// <inheritdoc cref="EntityData{T}.HasComponent{TComponent}"/>
        public bool HasComponent<TComponent>()
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (!IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");
                return false;
            }
#endif
            if (EntityComponentSystem.Constants.SystemID.IsNull())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot retrived {nameof(EntityComponentSystem)}.");
                return false;
            }

            return EntityComponentSystem.Constants.SystemID.System.HasComponent<TComponent>(EntityData<IEntityData>.GetEntityWithoutCheck(m_Idx));
        }
        /// <inheritdoc cref="EntityData{T}.HasComponent(Type)"/>
        public bool HasComponent(Type componentType)
        {
#if DEBUG_MODE
            if (!TypeHelper.TypeOf<IEntityComponent>.Type.IsAssignableFrom(componentType))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Type {TypeHelper.ToString(componentType)} is not an {nameof(IEntityComponent)}.");
                return false;
            }

            if (!IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");
                return false;
            }
#endif
            if (EntityComponentSystem.Constants.SystemID.IsNull())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot retrived {nameof(EntityComponentSystem)}.");
                return false;
            }

            return EntityComponentSystem.Constants.SystemID.System.HasComponent(EntityData<IEntityData>.GetEntityWithoutCheck(m_Idx), componentType);
        }
        /// <inheritdoc cref="EntityData{T}.GetComponent{TComponent}"/>
        public ref TComponent GetComponent<TComponent>()
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (IsEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to empty entity. This is not allowed.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
            else if (!IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
#endif
            if (EntityComponentSystem.Constants.SystemID.IsNull())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot retrived {nameof(EntityComponentSystem)}.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }

            return ref EntityComponentSystem.Constants.SystemID.System.GetComponent<TComponent>(EntityData<IEntityData>.GetEntityWithoutCheck(m_Idx));
        }
        /// <inheritdoc cref="EntityData{T}.GetComponentReadOnly{TComponent}"/>
        public TComponent GetComponentReadOnly<TComponent>()
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (!IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
#endif
            if (EntityComponentSystem.Constants.SystemID.IsNull())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot retrived {nameof(EntityComponentSystem)}.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }

            return EntityComponentSystem.Constants.SystemID.System.GetComponentReadOnly<TComponent>(EntityData<IEntityData>.GetEntityWithoutCheck(m_Idx));
        }
        /// <inheritdoc cref="EntityData{T}.GetComponentPointer{TComponent}"/>
        unsafe public TComponent* GetComponentPointer<TComponent>()
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (!IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
#endif
            if (EntityComponentSystem.Constants.SystemID.IsNull())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot retrived {nameof(EntityComponentSystem)}.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }

            return EntityComponentSystem.Constants.SystemID.System.GetComponentPointer<TComponent>(EntityData<IEntityData>.GetEntityWithoutCheck(m_Idx));
        }
        /// <inheritdoc cref="EntityData{T}.RemoveComponent{TComponent}"/>
        public void RemoveComponent<TComponent>()
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (!IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");
                return;
            }
#endif
            if (EntityComponentSystem.Constants.SystemID.IsNull())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot retrived {nameof(EntityComponentSystem)}.");
                return;
            }

            EntityData<IEntityData> entity = EntityData<IEntityData>.GetEntityWithoutCheck(m_Idx);
#if DEBUG_MODE
            s_EntitySystem.System.Debug_RemoveComponent<TComponent>(entity);
#endif
            EntityComponentSystem.Constants.SystemID.System.RemoveComponent<TComponent>(entity);
        }
        /// <inheritdoc cref="EntityData{T}.RemoveComponent(Type)"/>
        public void RemoveComponent(Type componentType)
        {
#if DEBUG_MODE
            if (!TypeHelper.TypeOf<IEntityComponent>.Type.IsAssignableFrom(componentType))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Type {TypeHelper.ToString(componentType)} is not an {nameof(IEntityComponent)}.");
                return;
            }

            if (!IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");
                return;
            }
#endif
            if (EntityComponentSystem.Constants.SystemID.IsNull())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot retrived {nameof(EntityComponentSystem)}.");
                return;
            }

            EntityData<IEntityData> entity = EntityData<IEntityData>.GetEntityWithoutCheck(m_Idx);
#if DEBUG_MODE
            s_EntitySystem.System.Debug_RemoveComponent(entity, componentType);
#endif
            EntityComponentSystem.Constants.SystemID.System.RemoveComponent(entity, componentType);
        }

        #endregion

        public void Destroy()
        {
#if DEBUG_MODE
            if (IsEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    "An empty entity reference trying to destroy.");
                return;
            }
#endif
            if (s_EntitySystem.IsNull())
            {
                s_EntitySystem = PresentationSystem<DefaultPresentationGroup, EntitySystem>.SystemID;
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
#if DEBUG_MODE
            if (IsEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    "An empty entity reference trying to access transform.");
                return 0;
            }
#endif
            if (s_EntitySystem.IsNull())
            {
                s_EntitySystem = PresentationSystem<DefaultPresentationGroup, EntitySystem>.SystemID;
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

        public static implicit operator T(Entity<T> a) => a.Target;
        //public static implicit operator Entity<IEntity>(Entity<T> a) => GetEntity(a.m_Idx);
        //public static implicit operator Entity<T>(Entity<IEntity> a) => GetEntity(a.m_Idx);
        public static implicit operator Entity<T>(Hash a) => GetEntity(a);
        public static implicit operator Entity<T>(EntityData<T> a) => GetEntity(a.Idx);
        public static implicit operator Entity<T>(T a)
        {
            if (a == null)
            {
                return Empty;
            }
            return GetEntity(a.Idx);
        }
        public static implicit operator Entity<T>(Instance<T> a)
        {
            if (a.IsEmpty() || !a.IsValid()) return Empty;
            return GetEntityWithoutCheck(a.Idx);
        }
    }
}
