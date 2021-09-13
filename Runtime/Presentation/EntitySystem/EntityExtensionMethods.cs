using Syadeu.Internal;
using Syadeu.Mono;
using Syadeu.Presentation.Entities;
using Syadeu.ThreadSafe;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Syadeu.Presentation
{
    public static class EntityExtensionMethods
    {
        public static Entity<TA> As<T, TA>(this EntityData<T> t)
            where T : class, IEntityData
            where TA : class, IEntity
        {
            return Entity<TA>.GetEntity(t.Idx);
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

        public static Entity<TA> CastAs<T, TA>(this EntityData<T> t)
            where T : class, IEntityData
            where TA : class, IEntity
        {
            return Entity<TA>.GetEntity(t.Idx);
        }
        public static EntityData<TA> CastAs<T, TA>(this Entity<T> t)
            where T : class, IEntity
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
            where T : EntityDataBase
        {
            return new EntityData<T>(t.Idx);
        }
    }
}
