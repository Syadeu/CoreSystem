#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Database;
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
    public struct EntityData<T> : IValidation, IEquatable<EntityData<T>>, IEquatable<Hash> where T : class, IEntityData
    {
        private const string c_Invalid = "Invalid";
        private static PresentationSystemID<EntitySystem> s_EntitySystem = PresentationSystemID<EntitySystem>.Null;
        internal static PresentationSystemID<EntityComponentSystem> s_ComponentSystem = PresentationSystemID<EntityComponentSystem>.Null;

        public static EntityData<T> Empty => new EntityData<T>(Hash.Empty, null);

        public static EntityData<T> GetEntity(Hash idx)
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

            return new EntityData<T>(idx, target.Name);
        }
        public static EntityData<T> GetEntityWithoutCheck(Hash idx)
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
            return new EntityData<T>(idx, target.Name);
        }

        /// <inheritdoc cref="IEntityData.Idx"/>
        private readonly Hash m_Idx;
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

        public FixedString128Bytes RawName => m_Name;
        /// <inheritdoc cref="IEntityData.Name"/>
        public string Name => m_Idx.Equals(Hash.Empty) ? c_Invalid : Target.Name;
        /// <inheritdoc cref="IEntityData.Hash"/>
        public Hash Hash => Target.Hash;
        /// <inheritdoc cref="IEntityData.Idx"/>
        public Hash Idx => m_Idx;
        public Type Type => m_Idx.Equals(Hash.Empty) ? null : Target?.GetType();

        internal EntityData(Hash idx, string name)
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

            return !s_EntitySystem.System.IsDestroyed(m_Idx);
        }

        public bool Equals(EntityData<T> other) => m_Idx.Equals(other.m_Idx);
        public bool Equals(Hash other) => m_Idx.Equals(other);

        #region Attributes

        /// <inheritdoc cref="IEntityData.HasAttribute(Hash)"/>
        public bool HasAttribute(Hash attributeHash)
        {
#if DEBUG_MODE
            if (!IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");
                return false;
            }
#endif
            return Target.HasAttribute(attributeHash);
        }
        public bool HasAttribute(Type t)
        {
#if DEBUG_MODE
            if (!IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");
                return false;
            }
#endif
            return Target.HasAttribute(t);
        }
        public bool HasAttribute<TA>() where TA : AttributeBase
        {
#if DEBUG_MODE
            if (!IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");
                return false;
            }
#endif
            return Target.HasAttribute<TA>();
        }
        /// <inheritdoc cref="IEntityData.GetAttribute(Type)"/>
        public AttributeBase GetAttribute(Type t)
        {
#if DEBUG_MODE
            if (!IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");
                return null;
            }
#endif
            return Target.GetAttribute(t);
        }
        /// <inheritdoc cref="IEntityData.GetAttributes(Type)"/>
        public AttributeBase[] GetAttributes(Type t)
        {
#if DEBUG_MODE
            if (!IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");
                return null;
            }
#endif
            return Target.GetAttributes(t);
        }
        /// <inheritdoc cref="IEntityData.GetAttribute(Type)"/>
        public TA GetAttribute<TA>() where TA : AttributeBase
        {
#if DEBUG_MODE
            if (!IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");
                return null;
            }
#endif
            return Target.GetAttribute<TA>();
        }
        /// <inheritdoc cref="IEntityData.GetAttributes(Type)"/>
        public TA[] GetAttributes<TA>() where TA : AttributeBase
        {
#if DEBUG_MODE
            if (!IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");
                return null;
            }
#endif
            return Target.GetAttributes<TA>();
        }

        #endregion

        #region Components

        /// <summary>
        /// <typeparamref name="TComponent"/> 를 이 엔티티에 추가합니다.
        /// </summary>
        /// <remarks>
        /// 추가된 컴포넌트는 <seealso cref="GetComponent{TComponent}"/> 를 통해 받아올 수 있습니다.
        /// </remarks>
        /// <typeparam name="TComponent"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public TComponent AddComponent<TComponent>(in TComponent data)
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (!IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");
                return default(TComponent);
            }
#endif
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

            EntityData<IEntityData> entity = EntityData<IEntityData>.GetEntityWithoutCheck(m_Idx);
#if DEBUG_MODE
            s_EntitySystem.System.Debug_AddComponent<TComponent>(entity);
#endif
            return s_ComponentSystem.System.AddComponent(entity, in data);
        }
        /// <summary>
        /// <typeparamref name="TComponent"/> 컴포넌트가 있는지 반환합니다.
        /// </summary>
        /// <typeparam name="TComponent"></typeparam>
        /// <returns></returns>
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
        /// <summary>
        /// 해당 타입의 컴포넌트가 있는지 반환합니다.
        /// </summary>
        /// <remarks>
        /// 타입이 <seealso cref="IEntityComponent"/> 를 상속받지 않으면 에디터에서만 오류를 반환합니다.
        /// </remarks>
        /// <param name="componentType"></param>
        /// <returns></returns>
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
        /// <summary>
        /// <typeparamref name="TComponent"/> 컴포넌트를 가져옵니다.
        /// </summary>
        /// <remarks>
        /// 컴포넌트가 없는 경우 에러를 뱉습니다. <seealso cref="HasComponent{TComponent}"/> 를 통해
        /// 목표 컴포넌트가 존재하는지 확인 할 수 있습니다.
        /// </remarks>
        /// <typeparam name="TComponent"></typeparam>
        /// <returns></returns>
        public ref TComponent GetComponent<TComponent>()
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (!IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");
                //return default(TComponent);
            }
#endif
            if (s_ComponentSystem.IsNull())
            {
                s_ComponentSystem = SharedStatic<EntityComponentConstrains>.GetOrCreate<EntityComponentSystem>().Data.SystemID;
                if (s_ComponentSystem.IsNull())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"Cannot retrived {nameof(EntityComponentSystem)}.");
                    //return default(TComponent);
                }
            }

            return ref s_ComponentSystem.System.GetComponent<TComponent>(EntityData<IEntityData>.GetEntityWithoutCheck(m_Idx));
        }
        /// <summary>
        /// <typeparamref name="TComponent"/> 컴포넌트를 제거합니다.
        /// </summary>
        /// <remarks>
        /// 컴포넌트를 제거할때 해당 컴포넌트가 <seealso cref="IDisposable"/> 를 상속받고 있으면 자동으로 수행합니다.
        /// </remarks>
        /// <typeparam name="TComponent"></typeparam>
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

            EntityData<IEntityData> entity = EntityData<IEntityData>.GetEntityWithoutCheck(m_Idx);
#if DEBUG_MODE
            s_EntitySystem.System.Debug_RemoveComponent<TComponent>(entity);
#endif
            s_ComponentSystem.System.RemoveComponent<TComponent>(entity);
        }
        /// <summary>
        /// 해당 컴포넌트를 제거합니다.
        /// </summary>
        /// <remarks>
        /// 컴포넌트를 제거할때 해당 컴포넌트가 <seealso cref="IDisposable"/> 를 상속받고 있으면 자동으로 수행합니다.<br/>
        /// 해당 타입이 <seealso cref="IEntityComponent"/> 를 상속받지 않는다면 에디터에서만 오류를 반환합니다.
        /// </remarks>
        /// <param name="componentType"></param>
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

            EntityData<IEntityData> entity = EntityData<IEntityData>.GetEntityWithoutCheck(m_Idx);
#if DEBUG_MODE
            s_EntitySystem.System.Debug_RemoveComponent(entity, componentType);
#endif
            s_ComponentSystem.System.RemoveComponent(entity, componentType);
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

            if (s_EntitySystem.System.m_ObjectEntities.TryGetValue(m_Idx, out ObjectBase value))
            {
                return value.GetHashCode();
            }

            CoreSystem.Logger.LogError(Channel.Entity,
                    $"Destroyed entity.");
            return 0;
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
