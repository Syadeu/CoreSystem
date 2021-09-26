﻿using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Proxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
    public readonly struct Entity<T> : IValidation, IEquatable<Entity<T>>, IEquatable<Hash> where T : class, IEntity
    {
        private const string c_Invalid = "Invalid";
        private static PresentationSystemID<EntitySystem> s_EntitySystem = PresentationSystemID<EntitySystem>.Null;

        public static Entity<T> Empty => new Entity<T>(Hash.Empty);

        public static Entity<T> GetEntity(Hash idx)
        {
            #region Validation
            if (s_EntitySystem.IsNull())
            {
                s_EntitySystem = PresentationSystem<EntitySystem>.SystemID;
                if (s_EntitySystem.IsNull())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        "Cannot retrived EntitySystem.");
                    return Empty;
                }
            }
            if (idx.Equals(Hash.Empty))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                $"Cannot convert an empty hash to Entity. This is an invalid operation and not allowed.");
                return Empty;
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

            return new Entity<T>(idx);
        }
        internal static Entity<T> GetEntityWithoutCheck(Hash idx)
        {
            return new Entity<T>(idx);
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

                if (!s_EntitySystem.System.m_ObjectEntities.TryGetValue(m_Idx, out var value) ||
                    !(value is T t))
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"Entity validation error. This entity is not an {TypeHelper.TypeOf<T>.ToString()} but {TypeHelper.ToString(value.GetType())}.");
                    return null;
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
        public Type Type => m_Idx.Equals(Hash.Empty) ? null : Target.GetType();

#pragma warning disable IDE1006 // Naming Styles
        /// <inheritdoc cref="EntityBase.transform"/>
        public ITransform transform
        {
            get
            {
                if (IsEmpty())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        "An empty entity reference trying to access transform.");
                    return null;
                }

                return Target.transform;
            }
        }
        public bool hasProxy
        {
            get
            {
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
                return tr.proxy;
            }
        }
#pragma warning restore IDE1006 // Naming Styles

        private Entity(Hash idx)
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

            return s_EntitySystem.System.m_ObjectEntities.ContainsKey(m_Idx);
        }

        public bool Equals(Entity<T> other) => m_Idx.Equals(other.m_Idx);
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

            return PresentationSystem<EntityComponentSystem>.System.AddComponent(
                EntityData<IEntityData>.GetEntityWithoutCheck(m_Idx), data);
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

            return PresentationSystem<EntityComponentSystem>.System.HasComponent<TComponent>(
                EntityData<IEntityData>.GetEntityWithoutCheck(m_Idx));
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

            return PresentationSystem<EntityComponentSystem>.System.GetComponent<TComponent>(EntityData<IEntityData>.GetEntityWithoutCheck(m_Idx));
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

            PresentationSystem<EntityComponentSystem>.System.RemoveComponent<TComponent>(EntityData<IEntityData>.GetEntityWithoutCheck(m_Idx));
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

        public override int GetHashCode() => Target.GetHashCode();

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
