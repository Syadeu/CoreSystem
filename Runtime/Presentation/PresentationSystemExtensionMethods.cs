﻿using Syadeu.Collections;
using Syadeu.Presentation.Entities;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Syadeu.Presentation
{
    public static class PresentationSystemExtensionMethods
    {
        //public static ReferenceArray<T> ToBuffer<T>(this T[] t, Allocator allocator)
        //    where T : unmanaged, IReference
        //{
        //    return new ReferenceArray<T>(t, allocator);
        //}
        public static FixedReferenceList64<T> ToFixedList64<T>(this IEnumerable<Reference<T>> t)
            where T : class, IObject
        {
            FixedReferenceList64<T> list = new FixedReferenceList64<T>();
            foreach (var item in t)
            {
                FixedReference<T> temp = item;
                list.Add(temp);
            }
            return list;
        }
        public static FixedReference<T> As<T>(this IFixedReference reference)
            where T : class, IObject
        {
            return new FixedReference<T>(reference.Hash);
        }

        public static Reference<T> As<T>(this IFixedReference<T> reference)
            where T : class, IObject
        {
            return new Reference<T>(reference.Hash);
        }

        public static Instance<TA> Cast<T, TA>(this Instance<T> t)
            where T : class, IObject
            where TA : class, IObject
        {
            return new Instance<TA>(t.Idx);
        }
        public static Reference<T> AsOriginal<T>(this Instance<T> t)
            where T : class, IObject
        {
            return new Reference<T>(t.GetObject().Hash);
        }

        public static T GetObject<T>(this IFixedReference<T> t)
            where T : class, IObject
        {
            if (t.IsEmpty())
            {
                return null;
            }
            else if (EntityDataList.Instance.m_Objects.TryGetValue(t.Hash, out ObjectBase value) &&
                value is T target)
            {
                return target;
            }
            return null;
        }
        public static ObjectBase GetObject(this IFixedReference t)
        {
            if (t.IsEmpty())
            {
                return null;
            }
            else if (EntityDataList.Instance.m_Objects.TryGetValue(t.Hash, out ObjectBase value))
            {
                return value;
            }
            return null;
        }
        public static Instance<T> CreateInstance<T>(this IFixedReference<T> target)
            where T : class, IObject
        {
            if (target.IsEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Entity, "You cannot create instance of null reference.");
                return Instance<T>.Empty;
            }

            Type t = target.GetObject().GetType();
            if (TypeHelper.TypeOf<EntityBase>.Type.IsAssignableFrom(t))
            {
                var temp = PresentationSystem<DefaultPresentationGroup, EntitySystem>.System.CreateEntity(target.Hash, float3.zero);
                return new Instance<T>(temp.Idx);
            }
            else if (TypeHelper.TypeOf<EntityDataBase>.Type.IsAssignableFrom(t))
            {
                var temp = PresentationSystem<DefaultPresentationGroup, EntitySystem>.System.CreateObject(target.Hash);
                return new Instance<T>(temp.Idx);
            }

            return PresentationSystem<DefaultPresentationGroup, EntitySystem>.System.CreateInstance<T>(target.GetObject());
        }
    }
}
