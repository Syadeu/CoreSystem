using Syadeu.Presentation.Entities;
using System.Collections.Generic;
using Unity.Collections;

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

        public static Instance<TA> Cast<T, TA>(this Instance<T> t)
            where T : ObjectBase
            where TA : ObjectBase
        {
            return new Instance<TA>(t.Idx);
        }
        public static Reference<T> AsOriginal<T>(this Instance<T> t)
            where T : ObjectBase
        {
            return new Reference<T>(t.Object.Hash);
        }
    }
}
