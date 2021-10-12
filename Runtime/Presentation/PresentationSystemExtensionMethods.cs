﻿using Syadeu.Collections;
using Syadeu.Presentation.Entities;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Syadeu.Presentation
{
    public static class PresentationSystemExtensionMethods
    {
        public static ReferenceArray<T> ToBuffer<T>(this T[] t, Allocator allocator)
            where T : unmanaged, IReference
        {
            return new ReferenceArray<T>(t, allocator);
        }
        public static T[] ToArray<T>(this ReferenceArray<T> t)
            where T : unmanaged, IReference
        {
            T[] array = new T[t.Length];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = t[i];
            }
            return array;
        }
        public static List<T> ToList<T>(this ReferenceArray<T> t)
            where T : unmanaged, IReference
        {
            List<T> list = new List<T>();
            for (int i = 0; i < t.Length; i++)
            {
                list.Add(t[i]);
            }
            return list;
        }

        public static Reference<T> As<T>(this IReference reference)
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
            return new Reference<T>(t.Object.Hash);
        }

        //public static NativeArray<T> GetNativeArray<T>(this ArrayWrapper<T> wrapper) where T : unmanaged
        //{
        //    if (!wrapper.m_NativeArray)
        //    {
        //        CoreSystem.Logger.LogError(Channel.Data, "Is not NativeArray");
        //        return default(NativeArray<T>);
        //    }

        //    AtomicSafetyHandle.CheckGetSecondaryDataPointerAndThrow(wrapper.m_Safety);

        //    NativeArray<T> arr;
        //    unsafe
        //    {
        //        arr = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(
        //            wrapper.m_Buffer,
        //            wrapper.Length,
        //            Allocator.Invalid);
        //    }

        //    AtomicSafetyHandle safety = wrapper.m_Safety;

        //    AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(safety);
        //    AtomicSafetyHandle.UseSecondaryVersion(ref safety);
        //    AtomicSafetyHandle.SetAllowSecondaryVersionWriting(safety, true);

        //    NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref arr, safety);

        //    return arr;
        //}
    }
}
