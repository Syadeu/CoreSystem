#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using Syadeu.Presentation.Entities;
using System;
using Unity.Burst;

namespace Syadeu.Presentation.Components
{
    public static class EntityComponentExtensionMethods
    {
        [BurstDiscard]
        public static ref TComponent GetComponent<TComponent>(in this EntityID id)
            where TComponent : unmanaged, IEntityComponent
        {
            EntityData<IEntityData> entity = id.GetEntityData<IEntityData>();
            
            return ref EntityComponentSystem.Constants.SystemID.System.GetComponent<TComponent>(entity);
        }

        #region IEntityDataID Components

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
    }
}
