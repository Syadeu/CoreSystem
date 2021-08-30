﻿using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Attributes;
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
        public static Entity<T> Empty => new Entity<T>(Hash.Empty);

        private static readonly Dictionary<Hash, Entity<T>> m_Entity = new Dictionary<Hash, Entity<T>>();
        public static Entity<T> GetEntity(Hash idx)
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
            IEntityData target = PresentationSystem<EntitySystem>.System.m_ObjectEntities[idx];
            if (!(target is T))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                $"Entity({target.Name}) is not a {TypeHelper.TypeOf<T>.Name}. This is an invalid operation and not allowed.");
                return Empty;
            }
            #endregion

            if (m_Entity.Count > 2048) m_Entity.Clear();

            if (!m_Entity.TryGetValue(idx, out var value))
            {
                value = new Entity<T>(idx);
                m_Entity.Add(idx, value);
            }
            return value;
        }
        internal static Entity<T> GetEntityWithoutCheck(Hash idx)
        {
            if (!m_Entity.TryGetValue(idx, out var value))
            {
                value = new Entity<T>(idx);
                m_Entity.Add(idx, value);
            }
            return value;
        }

        /// <inheritdoc cref="IEntityData.Idx"/>
        private readonly Hash m_Idx;

        public T Target
        {
            get
            {
                if (m_Idx.Equals(Hash.Empty) ||
                    !PresentationSystem<EntitySystem>.System.m_ObjectEntities.TryGetValue(m_Idx, out var value)) return null;

                return (T)value;
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
                if (m_Idx.Equals(Hash.Empty))
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        "An empty entity reference trying to call transform.");
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
#pragma warning restore IDE1006 // Naming Styles

        private Entity(Hash idx)
        {
            m_Idx = idx;
        }

        public bool IsValid() => !m_Idx.Equals(Hash.Empty) &&
            PresentationSystem<EntitySystem>.IsValid() &&
            PresentationSystem<EntitySystem>.System.m_ObjectEntities.ContainsKey(m_Idx);

        public bool Equals(Entity<T> other) => m_Idx.Equals(other.m_Idx);
        public bool Equals(Hash other) => m_Idx.Equals(other);

        #region Raycast
        //public EntitySystem.Raycaster Raycast(Ray ray) => PresentationSystem<EntitySystem>.System.Raycast(this, ray);
        #endregion

        /// <inheritdoc cref="IEntityData.GetAttribute(Type)"/>
        public AttributeBase GetAttribute(Type t)
        {
            if (!IsValid()) throw new Exception("not valid");
            return Target.GetAttribute(t);
        }
        /// <inheritdoc cref="IEntityData.GetAttributes(Type)"/>
        public AttributeBase[] GetAttributes(Type t)
        {
            if (!IsValid()) throw new Exception("not valid");
            return Target.GetAttributes(t);
        }
        /// <inheritdoc cref="IEntityData.GetAttribute(Type)"/>
        public TA GetAttribute<TA>() where TA : AttributeBase
        {
            if (!IsValid()) throw new Exception("not valid");
            return Target.GetAttribute<TA>();
        }
        /// <inheritdoc cref="IEntityData.GetAttributes(Type)"/>
        public TA[] GetAttributes<TA>() where TA : AttributeBase
        {
            if (!IsValid()) throw new Exception("not valid");
            return Target.GetAttributes<TA>();
        }

        public void Destroy() => PresentationSystem<EntitySystem>.System.InternalDestroyEntity(m_Idx);
        public override int GetHashCode() => Hash.GetHashCode();

        public static implicit operator T(Entity<T> a) => a.Target;
        //public static implicit operator Entity<IEntity>(Entity<T> a) => GetEntity(a.m_Idx);
        //public static implicit operator Entity<T>(Entity<IEntity> a) => GetEntity(a.m_Idx);
        public static implicit operator Entity<T>(Hash a) => GetEntity(a);
        public static implicit operator Entity<T>(EntityData<T> a) => GetEntity(a.Idx);
        public static implicit operator Entity<T>(T a) => GetEntity(a.Idx);
    }
}
