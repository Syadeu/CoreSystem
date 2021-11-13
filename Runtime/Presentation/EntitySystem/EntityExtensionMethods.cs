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
            var system = PresentationSystem<DefaultPresentationGroup, EntitySystem>.System;

            Entity<IEntity> ins = system.CreateEntity(other, in pos);
            return ins.Cast<IEntity, T>();
        }
        public static Entity<T> CreateEntity<T>(this Reference<T> other, float3 pos, quaternion rot, float3 localScale)
            where T : class, IEntity
        {
            var system = PresentationSystem<DefaultPresentationGroup, EntitySystem>.System;

            Entity<IEntity> ins = system.CreateEntity(other, in pos, in rot, in localScale);
            return ins.Cast<IEntity, T>();
        }
        public static EntityData<T> CreateEntityData<T>(this Reference<T> other)
            where T : class, IEntityData
        {
            var system = PresentationSystem<DefaultPresentationGroup, EntitySystem>.System;

            EntityData<IEntityData> ins = system.CreateObject(other);
            return ins.Cast<IEntityData, T>();
        }

        public static bool IsEntity(this in InstanceID id)
        {
            EntitySystem entitySystem = PresentationSystem<DefaultPresentationGroup, EntitySystem>.System;

            ObjectBase obj = entitySystem.GetEntityByID(id);
            if (obj is IEntity) return true;

            return false;
        }
        public static bool IsEntity(this in EntityID id)
        {
            EntitySystem entitySystem = PresentationSystem<DefaultPresentationGroup, EntitySystem>.System;

            ObjectBase obj = entitySystem.GetEntityByID(id);
            if (obj is IEntity) return true;

            return false;
        }
        public static bool IsEntity<T>(this in EntityID id) where T : IEntityData
        {
            EntitySystem entitySystem = PresentationSystem<DefaultPresentationGroup, EntitySystem>.System;

            ObjectBase obj = entitySystem.GetEntityByID(id);
            if (obj is T) return true;

            return false;
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

        #region IInstance

        public static IObject GetObject(this IInstance other)
        {
            if (other.IsEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                        "Cannot retrived null instance.");
                return null;
            }

            if (!PresentationSystem<DefaultPresentationGroup, EntitySystem>.System.m_ObjectEntities.TryGetValue(other.Idx, out ObjectBase obj))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                        $"Target({other.Idx}) is not exist.");
                return null;
            }

            return obj;
        }
        public static T GetObject<T>(this IInstance<T> other)
            where T : class, IObject
        {
            if (other.IsEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                        "Cannot retrived null instance.");
                return null;
            }

            if (!PresentationSystem<DefaultPresentationGroup, EntitySystem>.System.m_ObjectEntities.TryGetValue(other.Idx, out ObjectBase obj))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                        $"Target({other.Idx}) is not exist.");
                return null;
            }
            if (!(obj is T t))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Target is not a {TypeHelper.TypeOf<T>.Name}.");
                return null;
            }

            return t;
        }
        public static bool IsValid(this IInstance other)
        {
            var system = PresentationSystem<DefaultPresentationGroup, EntitySystem>.System;

            if (other.IsEmpty()) return false;
            else if (!system.m_ObjectEntities.ContainsKey(other.Idx)) return false;

            return true;
        }
        public static bool IsValid<T>(this IInstance<T> other)
            where T : class, IObject
        {
            var system = PresentationSystem<DefaultPresentationGroup, EntitySystem>.System;

            if (other.IsEmpty()) return false;
            else if (!system.m_ObjectEntities.ContainsKey(other.Idx)) return false;
            else if (!(system.m_ObjectEntities[other.Idx] is T))
            {
                return false;
            }
            else if (system.IsDestroyed(other.Idx) ||
                    system.IsMarkedAsDestroyed(other.Idx))
            {
                return false;
            }

            return true;
        }
        public static void Destroy(this IInstance other)
        {
            if (other.IsEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    "Cannot destroy an empty instance");
                return;
            }

            PresentationSystem<DefaultPresentationGroup, EntitySystem>.System.DestroyObject(other);
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
