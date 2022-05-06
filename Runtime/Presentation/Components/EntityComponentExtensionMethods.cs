﻿// Copyright 2021 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using Syadeu.Presentation.Entities;
using System;

namespace Syadeu.Presentation.Components
{
    public static class EntityComponentExtensionMethods
    {
        internal static EntityComponentSystem s_ComponentSystem;

#line hidden
        /// <summary>
        /// <typeparamref name="TComponent"/> 를 이 엔티티에 추가합니다.
        /// </summary>
        /// <remarks>
        /// 추가된 컴포넌트는 <seealso cref="GetComponent{TComponent}"/> 를 통해 받아올 수 있습니다.
        /// </remarks>
        /// <typeparam name="TComponent"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static void AddComponent<TComponent>(this InstanceID t)
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (t.IsEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot access a component({nameof(TComponent)}) with an empty entity id.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
            else if (!t.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity({t.Hash}). This is not allowed.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
            else if (s_ComponentSystem == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot retrived {nameof(EntityComponentSystem)}.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
#endif
            s_ComponentSystem.AddComponent<TComponent>(t);
        }
        /// <summary>
        /// <typeparamref name="TComponent"/> 컴포넌트가 있는지 반환합니다.
        /// </summary>
        /// <typeparam name="TComponent"></typeparam>
        /// <returns></returns>
        public static bool HasComponent<TComponent>(this InstanceID t)
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (t.IsEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot access a component({nameof(TComponent)}) with an empty entity id.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
            else if (!t.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity({t.Hash}). This is not allowed.");
                return false;
            }
            else if (s_ComponentSystem == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot retrived {nameof(EntityComponentSystem)}.");
                return false;
            }
#endif

            return s_ComponentSystem.HasComponent<TComponent>(t);
        }
        /// <summary>
        /// 해당 타입의 컴포넌트가 있는지 반환합니다.
        /// </summary>
        /// <remarks>
        /// 타입이 <seealso cref="IEntityComponent"/> 를 상속받지 않으면 에디터에서만 오류를 반환합니다.
        /// </remarks>
        /// <param name="componentType"></param>
        /// <returns></returns>
        public static bool HasComponent(in this InstanceID t, Type componentType)
        {
#if DEBUG_MODE
            if (!TypeHelper.TypeOf<IEntityComponent>.Type.IsAssignableFrom(componentType))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Type {TypeHelper.ToString(componentType)} is not an {nameof(IEntityComponent)}.");
                return false;
            }

            if (t.IsEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot access a component({componentType.Name}) with an empty entity id.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
            else if (!t.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");
                return false;
            }
            else if (s_ComponentSystem == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot retrived {nameof(EntityComponentSystem)}.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
#endif

            return s_ComponentSystem.HasComponent(t, componentType);
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
        public static ref TComponent GetComponent<TComponent>(this InstanceID t)
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (t.IsEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot access a component({nameof(TComponent)}) with an empty entity id.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
            else if (!t.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity({t.Hash}). This is not allowed.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
            else if (s_ComponentSystem == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot retrived {nameof(EntityComponentSystem)}.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
#endif
            return ref s_ComponentSystem.GetComponent<TComponent>(t);
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
        public static TComponent GetComponentReadOnly<TComponent>(this InstanceID t)
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (t.IsEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot access a component({nameof(TComponent)}) with an empty entity id.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
            else if (!t.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity({t.Hash}). This is not allowed.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
            else if (s_ComponentSystem == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot retrived {nameof(EntityComponentSystem)}.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
#endif

            return s_ComponentSystem.GetComponentReadOnly<TComponent>(t);
        }
        /// <summary>
        /// <typeparamref name="TComponent"/> 의 포인터 주소를 가져옵니다.
        /// </summary>
        /// <typeparam name="TComponent"></typeparam>
        /// <returns></returns>
        unsafe public static Collections.Buffer.LowLevel.UnsafeReference<TComponent> GetComponentPointer<TComponent>(this InstanceID t)
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (t.IsEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot access a component({nameof(TComponent)}) with an empty entity id.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
            else if (!t.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity({t.Hash}). This is not allowed.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
            else if (s_ComponentSystem == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot retrived {nameof(EntityComponentSystem)}.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
#endif
            return s_ComponentSystem.GetComponentPointer<TComponent>(t);
        }
        /// <summary>
        /// <typeparamref name="TComponent"/> 컴포넌트를 제거합니다.
        /// </summary>
        /// <remarks>
        /// 컴포넌트를 제거할때 해당 컴포넌트가 <seealso cref="IDisposable"/> 를 상속받고 있으면 자동으로 수행합니다.
        /// </remarks>
        /// <typeparam name="TComponent"></typeparam>
        public static void RemoveComponent<TComponent>(this InstanceID t)
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (t.IsEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot access a component({nameof(TComponent)}) with an empty entity id.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
            else if (!t.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity({t.Hash}). This is not allowed.");
                return;
            }
            else if (s_ComponentSystem == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot retrived {nameof(EntityComponentSystem)}.");
                return;
            }
#endif
            s_ComponentSystem.RemoveComponent<TComponent>(t);
        }
        /// <summary>
        /// 해당 컴포넌트를 제거합니다.
        /// </summary>
        /// <remarks>
        /// 컴포넌트를 제거할때 해당 컴포넌트가 <seealso cref="IDisposable"/> 를 상속받고 있으면 자동으로 수행합니다.<br/>
        /// 해당 타입이 <seealso cref="IEntityComponent"/> 를 상속받지 않는다면 에디터에서만 오류를 반환합니다.
        /// </remarks>
        /// <param name="componentType"></param>
        public static void RemoveComponent(this InstanceID t, Type componentType)
        {
#if DEBUG_MODE
            if (!TypeHelper.TypeOf<IEntityComponent>.Type.IsAssignableFrom(componentType))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Type {TypeHelper.ToString(componentType)} is not an {nameof(IEntityComponent)}.");
                return;
            }
            else if (t.IsEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot access a component({componentType.Name}) with an empty entity id.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
            else if (!t.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity({t.Hash}). This is not allowed.");
                return;
            }
            else if (s_ComponentSystem == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot retrived {nameof(EntityComponentSystem)}.");
                return;
            }
#endif
            s_ComponentSystem.RemoveComponent(t, componentType);
        }

        #region InstanceID<T> Components

        /// <inheritdoc cref="AddComponent{TComponent}(in InstanceID)"/>
        public static void AddComponent<T, TComponent>(this InstanceID<T> t)
            where T : class, IObject
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (t.IsEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot access a component({nameof(TComponent)}) with an empty entity id.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
            else if (!t.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity({t.Hash}). This is not allowed.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
            else if (s_ComponentSystem == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot retrived {nameof(EntityComponentSystem)}.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
#endif
            s_ComponentSystem.AddComponent<TComponent>(t);
        }
        /// <inheritdoc cref="HasComponent{TComponent}(in InstanceID)"/>
        public static bool HasComponent<T, TComponent>(this InstanceID<T> t)
            where T : class, IObject
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (t.IsEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot access a component({nameof(TComponent)}) with an empty entity id.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
            else if (!t.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity({t.Hash}). This is not allowed.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
            else if (s_ComponentSystem == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot retrived {nameof(EntityComponentSystem)}.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
#endif
            return s_ComponentSystem.HasComponent<TComponent>(t);
        }
        /// <inheritdoc cref="HasComponent(in InstanceID, Type)"/>
        public static bool HasComponent<T>(this InstanceID<T> t, Type componentType)
            where T : class, IObject
        {
#if DEBUG_MODE
            if (!TypeHelper.TypeOf<IEntityComponent>.Type.IsAssignableFrom(componentType))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Type {TypeHelper.ToString(componentType)} is not an {nameof(IEntityComponent)}.");
                return false;
            }
            else if (t.IsEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot access a component({componentType.Name}) with an empty entity id.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
            else if (!t.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity({t.Hash}). This is not allowed.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
            else if (s_ComponentSystem == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot retrived {nameof(EntityComponentSystem)}.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
#endif
            return s_ComponentSystem.HasComponent(t, componentType);
        }
        /// <inheritdoc cref="GetComponent{TComponent}(in InstanceID)"/>
        public static ref TComponent GetComponent<T, TComponent>(this InstanceID<T> t)
            where T : class, IObject
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (t.IsEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot access a component({nameof(TComponent)}) with an empty entity id.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
            else if (!t.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity({t.Hash}). This is not allowed.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
            else if (s_ComponentSystem == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot retrived {nameof(EntityComponentSystem)}.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
#endif
            return ref s_ComponentSystem.GetComponent<TComponent>(t);
        }
        /// <inheritdoc cref="GetComponentReadOnly{TComponent}(in InstanceID)"/>
        public static TComponent GetComponentReadOnly<T, TComponent>(this InstanceID<T> t)
            where T : class, IObject
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (t.IsEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot access a component({nameof(TComponent)}) with an empty entity id.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
            else if (!t.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity({t.Hash}). This is not allowed.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
            else if (s_ComponentSystem == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot retrived {nameof(EntityComponentSystem)}.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
#endif
            return s_ComponentSystem.GetComponentReadOnly<TComponent>(t);
        }
        /// <inheritdoc cref="GetComponentPointer{TComponent}(in InstanceID)"/>
        unsafe public static TComponent* GetComponentPointer<T, TComponent>(this InstanceID<T> t)
            where T : class, IObject
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (t.IsEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot access a component({nameof(TComponent)}) with an empty entity id.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
            else if (!t.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity({t.Hash}). This is not allowed.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
            else if (s_ComponentSystem == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot retrived {nameof(EntityComponentSystem)}.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
#endif
            return s_ComponentSystem.GetComponentPointer<TComponent>(t);
        }
        /// <inheritdoc cref="RemoveComponent{TComponent}(in InstanceID)"/>
        public static void RemoveComponent<T, TComponent>(this InstanceID<T> t)
            where T : class, IObject
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (t.IsEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot access a component({nameof(TComponent)}) with an empty entity id.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
            else if (!t.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity({t.Hash}). This is not allowed.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
            else if (s_ComponentSystem == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot retrived {nameof(EntityComponentSystem)}.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
#endif
            s_ComponentSystem.RemoveComponent<TComponent>(t);
        }
        /// <inheritdoc cref="RemoveComponent(in InstanceID, Type)"/>
        public static void RemoveComponent<T>(this InstanceID<T> t, Type componentType)
            where T : class, IObject
        {
#if DEBUG_MODE
            if (!TypeHelper.TypeOf<IEntityComponent>.Type.IsAssignableFrom(componentType))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Type {TypeHelper.ToString(componentType)} is not an {nameof(IEntityComponent)}.");
                return;
            }
            else if (t.IsEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot access a component({componentType.Name}) with an empty entity id.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
            else if (!t.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity({t.Hash}). This is not allowed.");
                return;
            }
            else if (s_ComponentSystem == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot retrived {nameof(EntityComponentSystem)}.");
                return;
            }
#endif
            s_ComponentSystem.RemoveComponent(t, componentType);
        }

        #endregion

        #region IEntityDataID Components

        /// <inheritdoc cref="AddComponent{TComponent}(in InstanceID)"/>
        public static void AddComponent<TComponent>(this IEntityDataID t)
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (!t.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity({t.Hash}, {t.RawName}). This is not allowed.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
            else if (s_ComponentSystem == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot retrived {nameof(EntityComponentSystem)}.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
#endif
            s_ComponentSystem.AddComponent<TComponent>(t.Idx);
        }
        /// <inheritdoc cref="HasComponent{TComponent}(in InstanceID)"/>
        public static bool HasComponent<TComponent>(this IEntityDataID t)
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (!t.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity({t.Hash}, {t.RawName}). This is not allowed.");
                return false;
            }
            else if (s_ComponentSystem == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot retrived {nameof(EntityComponentSystem)}.");
                return false;
            }
#endif
            return s_ComponentSystem.HasComponent<TComponent>(t.Idx);
        }
        /// <inheritdoc cref="HasComponent(in InstanceID, Type)"/>
        public static bool HasComponent(this IEntityDataID t, Type componentType)
        {
#if DEBUG_MODE
            if (!TypeHelper.TypeOf<IEntityComponent>.Type.IsAssignableFrom(componentType))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Type {TypeHelper.ToString(componentType)} is not an {nameof(IEntityComponent)}.");
                return false;
            }
            else if (!t.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");
                return false;
            }
            else if (s_ComponentSystem == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot retrived {nameof(EntityComponentSystem)}.");
                return false;
            }
#endif
            return s_ComponentSystem.HasComponent(t.Idx, componentType);
        }
        /// <inheritdoc cref="GetComponent{TComponent}(in InstanceID)"/>
        public static ref TComponent GetComponent<TComponent>(this IEntityDataID t)
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (!t.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity(t:{TypeHelper.ToString(t.GetType())}, {t.Hash}, {t.RawName}). This is not allowed.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
#endif
            if (s_ComponentSystem == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot retrived {nameof(EntityComponentSystem)}.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }

            return ref s_ComponentSystem.GetComponent<TComponent>(t.Idx);
        }
        /// <inheritdoc cref="GetComponentReadOnly{TComponent}(in InstanceID)"/>
        public static TComponent GetComponentReadOnly<TComponent>(this IEntityDataID t)
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (!t.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity({t.Hash}, {t.RawName}). This is not allowed.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
            else if (s_ComponentSystem == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot retrived {nameof(EntityComponentSystem)}.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
#endif
            return s_ComponentSystem.GetComponentReadOnly<TComponent>(t.Idx);
        }
        /// <inheritdoc cref="GetComponentPointer{TComponent}(in InstanceID)"/>
        unsafe public static TComponent* GetComponentPointer<TComponent>(this IEntityDataID t)
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (!t.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity({t.Hash}, {t.RawName}). This is not allowed.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
            else if (s_ComponentSystem == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot retrived {nameof(EntityComponentSystem)}.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
#endif
            return s_ComponentSystem.GetComponentPointer<TComponent>(t.Idx);
        }
        /// <inheritdoc cref="RemoveComponent{TComponent}(in InstanceID)"/>
        public static void RemoveComponent<TComponent>(this IEntityDataID t)
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (!t.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity({t.Hash}, {t.RawName}). This is not allowed.");
                return;
            }
            else if (s_ComponentSystem == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot retrived {nameof(EntityComponentSystem)}.");
                return;
            }
#endif
            s_ComponentSystem.RemoveComponent<TComponent>(t.Idx);
        }
        /// <inheritdoc cref="RemoveComponent(in InstanceID, Type)"/>
        public static void RemoveComponent(this IEntityDataID t, Type componentType)
        {
#if DEBUG_MODE
            if (!TypeHelper.TypeOf<IEntityComponent>.Type.IsAssignableFrom(componentType))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Type {TypeHelper.ToString(componentType)} is not an {nameof(IEntityComponent)}.");
                return;
            }
            else if (!t.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You\'re trying to access to an invalid entity({t.Hash}, {t.RawName}). This is not allowed.");
                return;
            }
            else if (s_ComponentSystem == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot retrived {nameof(EntityComponentSystem)}.");
                return;
            }
#endif
            s_ComponentSystem.RemoveComponent(t.Idx, componentType);
        }

        #endregion

        /// <inheritdoc cref="AddComponent{TComponent}(in InstanceID)"/>
        public static void AddComponent<TComponent>(this IObject t)
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (s_ComponentSystem == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot retrived {nameof(EntityComponentSystem)}.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
#endif
            s_ComponentSystem.AddComponent<TComponent>(t.Idx);
        }
        /// <inheritdoc cref="HasComponent{TComponent}(in InstanceID)"/>
        public static bool HasComponent<TComponent>(this IObject t)
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (s_ComponentSystem == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot retrived {nameof(EntityComponentSystem)}.");
                return false;
            }
#endif
            return s_ComponentSystem.HasComponent<TComponent>(t.Idx);
        }
        /// <inheritdoc cref="HasComponent(in InstanceID, Type)"/>
        public static bool HasComponent(this IObject t, Type componentType)
        {
#if DEBUG_MODE
            if (!TypeHelper.TypeOf<IEntityComponent>.Type.IsAssignableFrom(componentType))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Type {TypeHelper.ToString(componentType)} is not an {nameof(IEntityComponent)}.");
                return false;
            }
            else if (s_ComponentSystem == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot retrived {nameof(EntityComponentSystem)}.");
                return false;
            }
#endif
            return s_ComponentSystem.HasComponent(t.Idx, componentType);
        }
        /// <inheritdoc cref="GetComponent{TComponent}(in InstanceID)"/>
        public static ref TComponent GetComponent<TComponent>(this IObject t)
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (s_ComponentSystem == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot retrived {nameof(EntityComponentSystem)}.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
#endif
            return ref s_ComponentSystem.GetComponent<TComponent>(t.Idx);
        }
        /// <inheritdoc cref="GetComponentReadOnly{TComponent}(in InstanceID)"/>
        public static TComponent GetComponentReadOnly<TComponent>(this IObject t)
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (s_ComponentSystem == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot retrived {nameof(EntityComponentSystem)}.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
#endif
            return s_ComponentSystem.GetComponentReadOnly<TComponent>(t.Idx);
        }
        /// <inheritdoc cref="GetComponentPointer{TComponent}(in InstanceID)"/>
        unsafe public static TComponent* GetComponentPointer<TComponent>(this IObject t)
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (s_ComponentSystem == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot retrived {nameof(EntityComponentSystem)}.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
#endif
            return s_ComponentSystem.GetComponentPointer<TComponent>(t.Idx);
        }
        /// <inheritdoc cref="RemoveComponent{TComponent}(in InstanceID)"/>
        public static void RemoveComponent<TComponent>(this IObject t)
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (s_ComponentSystem == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot retrived {nameof(EntityComponentSystem)}.");
                return;
            }
#endif
            s_ComponentSystem.RemoveComponent<TComponent>(t.Idx);
        }
        /// <inheritdoc cref="RemoveComponent(in InstanceID, Type)"/>
        public static void RemoveComponent(this IObject t, Type componentType)
        {
#if DEBUG_MODE
            if (!TypeHelper.TypeOf<IEntityComponent>.Type.IsAssignableFrom(componentType))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Type {TypeHelper.ToString(componentType)} is not an {nameof(IEntityComponent)}.");
                return;
            }
            else if (s_ComponentSystem == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot retrived {nameof(EntityComponentSystem)}.");
                return;
            }
#endif
            s_ComponentSystem.RemoveComponent(t.Idx, componentType);
        }
#line default
    }
}
