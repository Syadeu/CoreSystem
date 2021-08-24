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
            return EntityData<TA>.GetEntityData(t.Idx);
        }

        public static EntityData<T> AsReference<T>(this T t)
            where T : class, IEntityData
        {
            return EntityData<T>.GetEntityData(t.Idx);
        }
        public static EntityData<TA> AsReference<T, TA>(this T t)
            where T : class, IEntityData
            where TA : class, IEntityData
        {
            return EntityData<TA>.GetEntityData(t.Idx);
        }
    }
}
