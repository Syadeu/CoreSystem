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
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using System;
using Unity.Mathematics;

namespace Syadeu.Presentation
{
    public static class EntityExtensionMethods
    {
        internal static EntitySystem s_EntitySystem;
        internal static EntityRecycleModule s_EntityRecycleModule;

        public static EntityShortID GetShortID(this InstanceID id)
        {
            return PresentationSystem<DefaultPresentationGroup, EntitySystem>.System.Convert(id);
        }
        public static InstanceID GetID(this EntityShortID id)
        {
            return PresentationSystem<DefaultPresentationGroup, EntitySystem>.System.Convert(id);
        }

        #region Converts

        public static Entity<T> ToEntity<T>(this IEntityDataID t)
            where T : class, IObject
        {
#if DEBUG_MODE
            if (!(t.Target is T))
            {
                CoreSystem.Logger.LogError(LogChannel.Entity,
                    $"Entity({t.RawName}) is not {TypeHelper.TypeOf<T>.ToString()}.");

                return Entity<T>.Empty;
            }
#endif
            return Entity<T>.GetEntity(t.Idx);
        }

        public static bool TryAsReference<T>(this T t, out Entity<T> entity)
            where T : class, IObject
        {
            entity = Entity<T>.Empty;

            if (t is IValidation validation && !validation.IsValid()) return false;

            entity = Entity<T>.GetEntityWithoutCheck(t.Idx);
            return true;
        }
        public static Entity<T> AsReference<T>(this T t)
            where T : class, IObject
        {
            return Entity<T>.GetEntity(t.Idx);
        }

        public static FixedReference<T> AsOriginal<T>(this T t)
            where T : class, IObject
        {
            return new FixedReference<T>(t.Hash);
        }
        public static FixedReference<T> AsOriginal<T>(this Entity<T> t)
            where T : class, IObject
        {
            return new FixedReference<T>(t.Hash);
        }
        public static FixedReference<T> AsOriginal<T>(this in InstanceID t)
            where T : class, IObject
        {
            return new FixedReference<T>(t.GetEntity().Hash);
        }

        public static InstanceID CreateEntity(this IFixedReference t)
        {
            Entity<IObject> ins = s_EntitySystem.CreateEntity(t);
            return ins.Idx;
        }

        public static Entity<T> CreateEntity<T>(this Reference<T> other)
            where T : class, IObject
        {
            Entity<T> ins = s_EntitySystem.CreateEntity(other);
            return ins;
        }
        public static Entity<T> CreateEntity<T>(this IFixedReference<T> other)
            where T : class, IObject
        {
            Entity<IObject> ins = s_EntitySystem.CreateEntity(other);
            return ins.ToEntity<T>();
        }
        public static Entity<T> CreateEntity<T>(this Reference<T> other, in float3 pos)
            where T : class, IObject
        {
            Entity<IEntity> ins = s_EntitySystem.CreateEntity(other, in pos);
            return ins.ToEntity<T>();
        }
        public static Entity<T> CreateEntity<T>(this IFixedReference<T> other, in float3 pos)
            where T : class, IObject
        {
            Entity<IEntity> ins = s_EntitySystem.CreateEntity(other, in pos);
            return ins.ToEntity<T>();
        }
        public static Entity<T> CreateEntity<T>(this Reference<T> other, float3 pos, quaternion rot, float3 localScale)
            where T : class, IEntity
        {
            Entity<IEntity> ins = s_EntitySystem.CreateEntity(other, in pos, in rot, in localScale);
            return ins.ToEntity<T>();
        }
        public static Entity<T> CreateEntity<T>(this IFixedReference<T> other, float3 pos, quaternion rot, float3 localScale)
            where T : class, IEntity
        {
            Entity<IEntity> ins = s_EntitySystem.CreateEntity(other, in pos, in rot, in localScale);
            return ins.ToEntity<T>();
        }
        
        /// <summary>
        /// 이 엔티티의 부모가 <typeparamref name="T"/>를 부모로 삼는지 반환합니다.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool IsEntity<T>(this in InstanceID id) where T : IObject
        {
            EntitySystem entitySystem = PresentationSystem<DefaultPresentationGroup, EntitySystem>.System;

            ObjectBase obj = entitySystem.GetEntityByID(id);
            if (obj is T) return true;

            return false;
        }
        public static bool IsEntity(this in InstanceID id, Type type)
        {
            EntitySystem entitySystem = PresentationSystem<DefaultPresentationGroup, EntitySystem>.System;

            ObjectBase obj = entitySystem.GetEntityByID(id);
            if (type.IsAssignableFrom(obj.GetType())) return true;

            return false;
        }
        public static bool IsDestroyed(this in InstanceID id)
        {
            return s_EntitySystem.IsDestroyed(in id) || s_EntitySystem.IsMarkedAsDestroyed(in id);
        }
        /// <inheritdoc cref="EntitySystem.DestroyEntity(InstanceID)"/>
        public static void Destroy(this in InstanceID id)
        {
            s_EntitySystem.DestroyEntity(id);
        }
        /// <inheritdoc cref="EntitySystem.DestroyEntity(InstanceID)"/>
        public static void Destroy<T>(this in InstanceID<T> id)
            where T : class, IObject
        {
            s_EntitySystem.DestroyEntity(id);
        }

        public static Entity<IObject> GetEntity(this InstanceID id) => GetEntity<IObject>(id);
        public static Entity<T> GetEntity<T>(this InstanceID id)
            where T : class, IObject
        {
            return Entity<T>.GetEntity(id);
        }
        public static Entity<T> GetEntity<T>(this InstanceID<T> id)
           where T : class, IObject
        {
            return Entity<T>.GetEntity(id);
        }
        public static Entity<T> GetEntityWithoutCheck<T>(this in InstanceID idx)
            where T : class, IObject
            => Entity<T>.GetEntityWithoutCheck(in idx);
        public static Entity<T> GetEntityWithoutCheck<T>(this in InstanceID<T> idx)
            where T : class, IObject
            => Entity<T>.GetEntityWithoutCheck(idx);

        public static ObjectBase GetObject(this InstanceID id)
        {
            ObjectBase obj = s_EntitySystem.GetEntityByID(id);
            return obj;
        }
        public static T GetObject<T>(this InstanceID id)
            where T : IObject
        {
            IObject obj = s_EntitySystem.GetEntityByID(id);
            return (T)obj;
        }
        public static T GetObject<T>(this InstanceID<T> id)
            where T : ObjectBase
        {
            ObjectBase obj = s_EntitySystem.GetEntityByID(id);
            return (T)obj;
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
                CoreSystem.Logger.LogError(LogChannel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");
                return false;
            }
#endif
            if (t.Target is EntityDataBase entityDataBase)
            {
                return entityDataBase.HasAttribute(attributeHash);
            }
            return false;
        }
        public static bool HasAttribute(this IEntityDataID t, Type type)
        {
#if DEBUG_MODE
            if (!t.IsValid())
            {
                CoreSystem.Logger.LogError(LogChannel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");
                return false;
            }
#endif
            if (t.Target is EntityDataBase entityDataBase)
            {
                return entityDataBase.HasAttribute(type);
            }
            return false;
        }
        public static bool HasAttribute<TAttribute>(this IEntityDataID t) where TAttribute : AttributeBase
        {
#if DEBUG_MODE
            if (!t.IsValid())
            {
                CoreSystem.Logger.LogError(LogChannel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");
                return false;
            }
#endif
            if (t.Target is EntityDataBase entityDataBase)
            {
                return entityDataBase.HasAttribute<TAttribute>();
            }
            return false;
        }
        /// <inheritdoc cref="IEntityData.GetAttribute(Type)"/>
        public static IAttribute GetAttribute(this IEntityDataID t, Type type)
        {
#if DEBUG_MODE
            if (!t.IsValid())
            {
                CoreSystem.Logger.LogError(LogChannel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");
                return null;
            }
#endif
            if (t.Target is IEntityData entityDataBase)
            {
                return entityDataBase.GetAttribute(type);
            }
            return null;
        }
        /// <inheritdoc cref="IEntityData.GetAttributes(Type)"/>
        public static IAttribute[] GetAttributes(this IEntityDataID t, Type type)
        {
#if DEBUG_MODE
            if (!t.IsValid())
            {
                CoreSystem.Logger.LogError(LogChannel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");
                return null;
            }
#endif
            if (t.Target is IEntityData entityDataBase)
            {
                return entityDataBase.GetAttributes(type);
            }
            return Array.Empty<IAttribute>();
        }
        /// <inheritdoc cref="IEntityData.GetAttribute(Type)"/>
        public static TAttribute GetAttribute<TAttribute>(this IEntityDataID t) where TAttribute : AttributeBase
        {
#if DEBUG_MODE
            if (!t.IsValid())
            {
                var entitySystem = PresentationSystem<DefaultPresentationGroup, EntitySystem>.System;

                CoreSystem.Logger.LogError(LogChannel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.\n" +
                    $"d:{entitySystem.IsDestroyed(t.Idx)}, dq:{entitySystem.IsMarkedAsDestroyed(t.Idx)}, resv:{((ObjectBase)t.Target).Reserved}, tr:{((t.Target is EntityBase entity) ? $"{entity.HasTransform()}" : "notr")}");
                return null;
            }
#endif
            if (t.Target is IEntityData entityDataBase)
            {
                return entityDataBase.GetAttribute<TAttribute>();
            }
            return null;
        }
        /// <inheritdoc cref="IEntityData.GetAttributes(Type)"/>
        public static TAttribute[] GetAttributes<TAttribute>(this IEntityDataID t) where TAttribute : AttributeBase
        {
#if DEBUG_MODE
            if (!t.IsValid())
            {
                CoreSystem.Logger.LogError(LogChannel.Entity,
                    $"You\'re trying to access to an invalid entity. This is not allowed.");
                return null;
            }
#endif
            if (t.Target is IEntityData entityDataBase)
            {
                return entityDataBase.GetAttributes<TAttribute>();
            }
            return Array.Empty<TAttribute>();
        }

        #endregion

        /// <inheritdoc cref="EntitySystem.DestroyEntity(InstanceID)"/>
        public static void Destroy(this IEntityDataID t)
        {
#if DEBUG_MODE
            if (t.IsEmpty())
            {
                CoreSystem.Logger.LogError(LogChannel.Entity,
                    "An empty entity reference trying to destroy.");
                return;
            }
#endif
            s_EntitySystem.InternalDestroyEntity(t.Idx);
        }

        #endregion

        #region PrefabReference

        /// <summary>
        /// <see cref="EntityRecycleModule"/> 을 통해 재사용 가능한 인스턴스를 가져옵니다.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static UnityEngine.Object GetOrCreateInstance(this in PrefabReference t)
        {
            return s_EntityRecycleModule.GetOrCreatePrefab(t);
        }
        /// <inheritdoc cref="GetOrCreateInstance(in PrefabReference)"/>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <returns></returns>
        public static T GetOrCreateInstance<T>(this in PrefabReference t)
            where T : UnityEngine.Object
        {
            return (T)s_EntityRecycleModule.GetOrCreatePrefab(t);
        }
        /// <inheritdoc cref="GetOrCreateInstance(in PrefabReference)"/>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <returns></returns>
        public static T GetOrCreateInstance<T>(this in PrefabReference<T> t)
            where T : UnityEngine.Object
        {
            return (T)s_EntityRecycleModule.GetOrCreatePrefab(t);
        }
        /// <summary>
        /// 재사용 인스턴스를 반환합니다.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="obj"></param>
        public static void ReserveInstance(this in PrefabReference t, UnityEngine.Object obj)
        {
            s_EntityRecycleModule.ReservePrefab(t, obj);
        }

        #endregion
    }

    public static class EntityDataHelper
    {
        public static Entity<T> GetEntity<T>(in InstanceID idx)
            where T : class, IObject
        {
            #region Validation
#if DEBUG_MODE
            if (idx.Equals(Hash.Empty))
            {
                CoreSystem.Logger.LogError(LogChannel.Entity,
                $"Cannot convert an empty hash to Entity. This is an invalid operation and not allowed.");
                return Entity<T>.Empty;
            }
#endif
            PresentationSystem<DefaultPresentationGroup, EntitySystem>.System.GetEntityByID(idx);
            
            ObjectBase target = PresentationSystem<DefaultPresentationGroup, EntitySystem>.System.GetEntityByID(idx);
            if (target == null)
            {
                CoreSystem.Logger.LogError(LogChannel.Entity,
                    $"Cannot found entity({idx})");
                return Entity<T>.Empty;
            }
            else if (!(target is T))
            {
                CoreSystem.Logger.LogError(LogChannel.Entity,
                $"Entity({target.Name}) is not a {TypeHelper.TypeOf<T>.Name}. This is an invalid operation and not allowed.");
                return Entity<T>.Empty;
            }

            #endregion

            int hash = idx.GetHashCode();
            if (hash == 0)
            {
                "internal error hash 0".ToLogError();
            }

            return new Entity<T>(idx, idx.GetHashCode(), target.Name);
        }
    }
}
