#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using Syadeu.Internal;
using Syadeu.Mono;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.ThreadSafe;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace Syadeu.Presentation
{
    public static class EntityExtensionMethods
    {
        public static EntityShortID GetShortID(this EntityID id)
        {
            return PresentationSystem<DefaultPresentationGroup, EntitySystem>.System.Convert(id);
        }
        public static EntityID GetEntityID(this EntityShortID id)
        {
            return PresentationSystem<DefaultPresentationGroup, EntitySystem>.System.Convert(id);
        }

        #region Converts

        public static Entity<TA> As<T, TA>(this EntityData<T> t)
            where T : class, IEntityData
            where TA : class, IEntity
        {
            return Entity<TA>.GetEntity(t.Idx);
        }
        public static Entity<T> As<T>(this EntityData<T> t)
            where T : class, IEntity
        {
            return Entity<T>.GetEntity(t.Idx);
        }
        public static EntityData<TA> As<T, TA>(this Entity<T> t)
            where T : class, IEntity
            where TA : class, IEntityData
        {
            return EntityData<TA>.GetEntity(t.Idx);
        }

        public static Entity<TA> Cast<T, TA>(this Entity<T> t)
            where T : class, IEntity
            where TA : class, IEntity
        {
            return Entity<TA>.GetEntity(t.Idx);
        }
        public static EntityData<TA> Cast<T, TA>(this EntityData<T> t)
            where T : class, IEntityData
            where TA : class, IEntityData
        {
            return EntityData<TA>.GetEntity(t.Idx);
        }

        public static bool TryAsReference<T>(this T t, out EntityData<T> entity)
            where T : class, IEntityData
        {
            entity = EntityData<T>.Empty;

            if (!t.IsValid()) return false;

            entity = EntityData<T>.GetEntityWithoutCheck(t.Idx);
            return true;
        }
        public static EntityData<T> AsReference<T>(this T t)
            where T : class, IEntityData
        {
            return EntityData<T>.GetEntity(t.Idx);
        }
        public static EntityData<TA> AsReference<T, TA>(this T t)
            where T : class, IEntityData
            where TA : class, IEntityData
        {
            return EntityData<TA>.GetEntity(t.Idx);
        }

        public static EntityData<T> As<T>(this Instance<T> t)
            where T : class, IEntityData
        {
            return EntityData<T>.GetEntity(t.Idx);
        }
        public static Instance<T> AsInstance<T>(this Entity<T> entity)
            where T : class, IEntity
        {
            return new Instance<T>(entity.Idx);
        }
        public static Instance<T> AsInstance<T>(this EntityData<T> entity)
            where T : class, IEntityData
        {
            return new Instance<T>(entity.Idx);
        }

        public static Entity<T> CreateEntity<T>(this Reference<T> other, in float3 pos)
            where T : class, IEntity
        {
            return Instance<T>.CreateInstance(in other, in pos);
        }
        public static Entity<T> CreateEntity<T>(this Reference<T> other, float3 pos, quaternion rot, float3 localScale)
            where T : class, IEntity
        {
            return Instance<T>.CreateInstance(in other, in pos, in rot, in localScale);
        }
        public static EntityData<T> CreateEntityData<T>(this Reference<T> other)
            where T : class, IEntityData
        {
            return Instance<T>.CreateInstance(other).As();
        }

        public static Entity<T> GetEntity<T>(this EntityID id)
            where T : class, IEntity
        {
            EntitySystem entitySystem = PresentationSystem<DefaultPresentationGroup, EntitySystem>.System;

            ObjectBase obj = entitySystem.GetEntityByID(id);
#if DEBUG_MODE
            if (obj == null)
            {
                return Entity<T>.Empty;
            }
            else if (!(obj is IEntity))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Instance({obj.Name}) is not entity but you trying to get with {nameof(EntityID)}.");
                return Entity<T>.Empty;
            }
#endif
            return Entity<T>.GetEntityWithoutCheck(id);
        }
        public static Entity<T> GetEntity<T>(this InstanceID id)
            where T : class, IEntity
        {
            EntitySystem entitySystem = PresentationSystem<DefaultPresentationGroup, EntitySystem>.System;

            ObjectBase obj = entitySystem.GetEntityByID(id);
#if DEBUG_MODE
            if (obj == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Instance({id.Hash}) is invalid id.");
                return Entity<T>.Empty;
            }
            else if (!(obj is IEntity))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Instance({obj.Name}) is not entity but you trying to get with {nameof(InstanceID)}.");
                return Entity<T>.Empty;
            }
#endif
            return Entity<T>.GetEntityWithoutCheck(id);
        }
        public static EntityData<T> GetEntityData<T>(this EntityID id)
            where T : class, IEntityData
        {
            EntitySystem entitySystem = PresentationSystem<DefaultPresentationGroup, EntitySystem>.System;

            ObjectBase obj = entitySystem.GetEntityByID(id);
#if DEBUG_MODE
            if (obj == null)
            {
                return EntityData<T>.Empty;
            }
#endif
            return EntityData<T>.GetEntityWithoutCheck(id);
        }
        public static EntityData<T> GetEntityData<T>(this InstanceID id)
            where T : class, IEntityData
        {
            EntitySystem entitySystem = PresentationSystem<DefaultPresentationGroup, EntitySystem>.System;

            ObjectBase obj = entitySystem.GetEntityByID(id);
#if DEBUG_MODE
            if (obj == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Instance({id.Hash}) is invalid id.");
                return EntityData<T>.Empty;
            }
            else if (!(obj is IEntityData))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Instance({obj.Name}) is not entity but you trying to get with {nameof(InstanceID)}.");
                return EntityData<T>.Empty;
            }
#endif
            return EntityData<T>.GetEntityWithoutCheck(id);
        }

        public static Instance GetInstance(this EntityID id)
        {
            EntitySystem entitySystem = PresentationSystem<DefaultPresentationGroup, EntitySystem>.System;

            ObjectBase obj = entitySystem.GetEntityByID(id);
            return new Instance(obj);
        }
        public static Instance<T> GetInstance<T>(this EntityID id)
            where T : class, IObject
        {
            EntitySystem entitySystem = PresentationSystem<DefaultPresentationGroup, EntitySystem>.System;

            ObjectBase obj = entitySystem.GetEntityByID(id);
            return new Instance<T>(obj);
        }
        public static Instance GetInstance(this InstanceID id)
        {
            EntitySystem entitySystem = PresentationSystem<DefaultPresentationGroup, EntitySystem>.System;

            ObjectBase obj = entitySystem.GetEntityByID(id);
            return new Instance(obj);
        }
        public static Instance<T> GetInstance<T>(this InstanceID id)
            where T : class, IObject
        {
            EntitySystem entitySystem = PresentationSystem<DefaultPresentationGroup, EntitySystem>.System;

            ObjectBase obj = entitySystem.GetEntityByID(id);
            return new Instance<T>(obj);
        }

        #endregion

        #region IEntityDataID

        #region Attributes

        /// <inheritdoc cref="IEntityData.HasAttribute(Hash)"/>
        public static bool HasAttribute(this IEntityDataID t, Hash attributeHash)
        {
#if DEBUG_MODE
            if (!t.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");
                return false;
            }
#endif
            return t.Target.HasAttribute(attributeHash);
        }
        public static bool HasAttribute(this IEntityDataID t, Type type)
        {
#if DEBUG_MODE
            if (!t.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");
                return false;
            }
#endif
            return t.Target.HasAttribute(type);
        }
        public static bool HasAttribute<TAttribute>(this IEntityDataID t) where TAttribute : AttributeBase
        {
#if DEBUG_MODE
            if (!t.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");
                return false;
            }
#endif
            return t.Target.HasAttribute<TAttribute>();
        }
        /// <inheritdoc cref="IEntityData.GetAttribute(Type)"/>
        public static IAttribute GetAttribute(this IEntityDataID t, Type type)
        {
#if DEBUG_MODE
            if (!t.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");
                return null;
            }
#endif
            return t.Target.GetAttribute(type);
        }
        /// <inheritdoc cref="IEntityData.GetAttributes(Type)"/>
        public static IAttribute[] GetAttributes(this IEntityDataID t, Type type)
        {
#if DEBUG_MODE
            if (!t.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");
                return null;
            }
#endif
            return t.Target.GetAttributes(type);
        }
        /// <inheritdoc cref="IEntityData.GetAttribute(Type)"/>
        public static TAttribute GetAttribute<TAttribute>(this IEntityDataID t) where TAttribute : AttributeBase
        {
#if DEBUG_MODE
            if (!t.IsValid())
            {
                var entitySystem = PresentationSystem<DefaultPresentationGroup, EntitySystem>.System;

                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.\n" +
                    $"d:{entitySystem.IsDestroyed(t.Idx)}, dq:{entitySystem.IsMarkedAsDestroyed(t.Idx)}");
                return null;
            }
#endif
            return t.Target.GetAttribute<TAttribute>();
        }
        /// <inheritdoc cref="IEntityData.GetAttributes(Type)"/>
        public static TAttribute[] GetAttributes<TAttribute>(this IEntityDataID t) where TAttribute : AttributeBase
        {
#if DEBUG_MODE
            if (!t.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");
                return null;
            }
#endif
            return t.Target.GetAttributes<TAttribute>();
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
        public static void AddComponent<TComponent>(this IEntityDataID t)
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (!t.IsValid())
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

            EntityData<IEntityData> entity = EntityData<IEntityData>.GetEntityWithoutCheck(t.Idx);
#if DEBUG_MODE
            PresentationSystem<DefaultPresentationGroup, EntitySystem>.System.Debug_AddComponent<TComponent>(entity);
#endif
            EntityComponentSystem.Constants.SystemID.System.AddComponent<TComponent>(entity);
            //return ref EntityComponentSystem.Constants.SystemID.System.AddComponent<TComponent>(entity);
        }
        /// <summary>
        /// <typeparamref name="TComponent"/> 컴포넌트가 있는지 반환합니다.
        /// </summary>
        /// <typeparam name="TComponent"></typeparam>
        /// <returns></returns>
        public static bool HasComponent<TComponent>(this IEntityDataID t)
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (!t.IsValid())
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

            return EntityComponentSystem.Constants.SystemID.System.HasComponent<TComponent>(EntityData<IEntityData>.GetEntityWithoutCheck(t.Idx));
        }
        /// <summary>
        /// 해당 타입의 컴포넌트가 있는지 반환합니다.
        /// </summary>
        /// <remarks>
        /// 타입이 <seealso cref="IEntityComponent"/> 를 상속받지 않으면 에디터에서만 오류를 반환합니다.
        /// </remarks>
        /// <param name="componentType"></param>
        /// <returns></returns>
        public static bool HasComponent(this IEntityDataID t, Type componentType)
        {
#if DEBUG_MODE
            if (!TypeHelper.TypeOf<IEntityComponent>.Type.IsAssignableFrom(componentType))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Type {TypeHelper.ToString(componentType)} is not an {nameof(IEntityComponent)}.");
                return false;
            }

            if (!t.IsValid())
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

            return EntityComponentSystem.Constants.SystemID.System.HasComponent(EntityData<IEntityData>.GetEntityWithoutCheck(t.Idx), componentType);
        }
        /// <summary>
        /// <typeparamref name="TComponent"/> 컴포넌트를 가져옵니다.
        /// </summary>
        /// <remarks>
        /// <seealso cref="IJobParallelForEntities{TComponent}"/> Job 이 수행 중이라면 완료 후 반환합니다.
        /// 읽기만 필요하다면 <seealso cref="GetComponentReadOnly{TComponent}"/> 를 사용하세요.<br/>
        /// <br/>
        /// 컴포넌트가 없는 경우 에러를 뱉습니다. <seealso cref="HasComponent{TComponent}"/> 를 통해
        /// 목표 컴포넌트가 존재하는지 확인 할 수 있습니다.
        /// </remarks>
        /// <typeparam name="TComponent"></typeparam>
        /// <returns></returns>
        public static ref TComponent GetComponent<TComponent>(this IEntityDataID t)
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (!t.IsValid())
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

            return ref EntityComponentSystem.Constants.SystemID.System.GetComponent<TComponent>(EntityData<IEntityData>.GetEntityWithoutCheck(t.Idx));
        }
        /// <summary>
        /// 박싱된 <typeparamref name="TComponent"/> 컴포넌트를 가져옵니다.
        /// </summary>
        /// <remarks>
        /// 컴포넌트가 없는 경우 에러를 뱉습니다. <seealso cref="HasComponent{TComponent}"/> 를 통해
        /// 목표 컴포넌트가 존재하는지 확인 할 수 있습니다.
        /// </remarks>
        /// <typeparam name="TComponent"></typeparam>
        /// <returns></returns>
        public static TComponent GetComponentReadOnly<TComponent>(this IEntityDataID t)
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (!t.IsValid())
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

            return EntityComponentSystem.Constants.SystemID.System.GetComponentReadOnly<TComponent>(EntityData<IEntityData>.GetEntityWithoutCheck(t.Idx));
        }
        /// <summary>
        /// <typeparamref name="TComponent"/> 의 포인터 주소를 가져옵니다.
        /// </summary>
        /// <typeparam name="TComponent"></typeparam>
        /// <returns></returns>
        unsafe public static TComponent* GetComponentPointer<TComponent>(this IEntityDataID t)
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (!t.IsValid())
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

            return EntityComponentSystem.Constants.SystemID.System.GetComponentPointer<TComponent>(EntityData<IEntityData>.GetEntityWithoutCheck(t.Idx));
        }
        /// <summary>
        /// <typeparamref name="TComponent"/> 컴포넌트를 제거합니다.
        /// </summary>
        /// <remarks>
        /// 컴포넌트를 제거할때 해당 컴포넌트가 <seealso cref="IDisposable"/> 를 상속받고 있으면 자동으로 수행합니다.
        /// </remarks>
        /// <typeparam name="TComponent"></typeparam>
        public static void RemoveComponent<TComponent>(this IEntityDataID t) 
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (!t.IsValid())
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

            EntityData<IEntityData> entity = EntityData<IEntityData>.GetEntityWithoutCheck(t.Idx);
#if DEBUG_MODE
            PresentationSystem<DefaultPresentationGroup, EntitySystem>.System.Debug_RemoveComponent<TComponent>(entity);
#endif
            EntityComponentSystem.Constants.SystemID.System.RemoveComponent<TComponent>(entity);
        }
        /// <summary>
        /// 해당 컴포넌트를 제거합니다.
        /// </summary>
        /// <remarks>
        /// 컴포넌트를 제거할때 해당 컴포넌트가 <seealso cref="IDisposable"/> 를 상속받고 있으면 자동으로 수행합니다.<br/>
        /// 해당 타입이 <seealso cref="IEntityComponent"/> 를 상속받지 않는다면 에디터에서만 오류를 반환합니다.
        /// </remarks>
        /// <param name="componentType"></param>
        public static void RemoveComponent(this IEntityDataID t, Type componentType) 
        {
#if DEBUG_MODE
            if (!TypeHelper.TypeOf<IEntityComponent>.Type.IsAssignableFrom(componentType))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Type {TypeHelper.ToString(componentType)} is not an {nameof(IEntityComponent)}.");
                return;
            }

            if (!t.IsValid())
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

            EntityData<IEntityData> entity = EntityData<IEntityData>.GetEntityWithoutCheck(t.Idx);
#if DEBUG_MODE
            PresentationSystem<DefaultPresentationGroup, EntitySystem>.System.Debug_RemoveComponent(entity, componentType);
#endif
            EntityComponentSystem.Constants.SystemID.System.RemoveComponent(entity, componentType);
        }

        #endregion

        public static void Destroy(this IEntityDataID t)
        {
#if DEBUG_MODE
            if (t.IsEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    "An empty entity reference trying to destroy.");
                return;
            }
#endif
            PresentationSystem<DefaultPresentationGroup, EntitySystem>.System.InternalDestroyEntity(t.Idx);
        }

        #endregion
    }

    public static class EntityDataHelper
    {
        public static EntityData<T> GetEntity<T>(InstanceID idx)
            where T : class, IEntityData
        {
            #region Validation
#if DEBUG_MODE
            if (idx.Equals(Hash.Empty))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                $"Cannot convert an empty hash to Entity. This is an invalid operation and not allowed.");
                return EntityData<T>.Empty;
            }
#endif
            PresentationSystem<DefaultPresentationGroup, EntitySystem>.System.GetEntityByID(idx);
            
            ObjectBase target = PresentationSystem<DefaultPresentationGroup, EntitySystem>.System.GetEntityByID(idx);
            if (target == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot found entity({idx})");
                return EntityData<T>.Empty;
            }
            else if (!(target is T))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                $"Entity({target.Name}) is not a {TypeHelper.TypeOf<T>.Name}. This is an invalid operation and not allowed.");
                return EntityData<T>.Empty;
            }

            #endregion

            int hash = target.GetHashCode();
            if (hash == 0)
            {
                "internal error hash 0".ToLogError();
            }

            return new EntityData<T>(idx, target.GetHashCode(), target.Name);
        }
        public static EntityData<T> GetEntityWithoutCheck<T>(InstanceID idx)
            where T : class, IEntityData
        {
            ObjectBase target = PresentationSystem<DefaultPresentationGroup, EntitySystem>.System.GetEntityByID(idx);
#if DEBUG_MODE
            if (target == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Target Not Found.");
                return EntityData<T>.Empty;
            }
#endif
            return new EntityData<T>(idx, target.GetHashCode(), target.Name);
        }
    }
}
