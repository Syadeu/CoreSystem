using Syadeu.Internal;
using Syadeu.Mono;
using Syadeu.Presentation.Attributes;
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

        public static Entity<T> CreateInstance<T>(this Reference<T> other, in float3 pos)
            where T : class, IEntity
        {
            return Instance<T>.CreateInstance(in other, in pos);
        }
        public static Entity<T> CreateInstance<T>(this Reference<T> other, float3 pos, quaternion rot, float3 localScale)
            where T : class, IEntity
        {
            return Instance<T>.CreateInstance(in other, in pos, in rot, in localScale);
        }
        public static EntityData<T> CreateInstance<T>(this Reference<T> other)
            where T : class, IEntityData
        {
            return Instance<T>.CreateInstance(other).As();
        }
    }
}
