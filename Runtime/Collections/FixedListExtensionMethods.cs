using System.Collections.Generic;

namespace Syadeu.Collections
{
    public static class FixedListExtensionMethods
    {
        public static FixedReferenceList64<T> ToFixedList64<T>(this IEnumerable<FixedReference<T>> t)
            where T : class, IObject
        {
            FixedReferenceList64<T> list = new FixedReferenceList64<T>();
            foreach (var item in t)
            {
                list.Add(item);
            }
            return list;
        }
        public static FixedInstanceList64<T> ToFixedList64<T>(this IEnumerable<Instance<T>> t)
            where T : class, IObject
        {
            FixedInstanceList64<T> list = new FixedInstanceList64<T>();
            foreach (var item in t)
            {
                list.Add(item.Idx);
            }
            return list;
        }
        public static FixedInstanceList16<T> ToFixedList16<T>(this IEnumerable<Instance<T>> t)
            where T : class, IObject
        {
            FixedInstanceList16<T> list = new FixedInstanceList16<T>();
            foreach (var item in t)
            {
                list.Add(item.Idx);
            }
            return list;
        }
    }
}
