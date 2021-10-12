using Syadeu.Collections;
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

        public static Instance GetInstance(this InstanceID id)
        {
            if (id.IsEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Empty instance cannot be converted.");
                return Instance.Empty;
            }

            return new Instance(id);
        }
        public static Instance<T> GetInstance<T>(this InstanceID id) where T : class, IObject
        {
            if (id.IsEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Empty instance cannot be converted.");
                return Instance<T>.Empty;
            }

            Instance<T> temp = new Instance<T>(id);
            if (!temp.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Instance {temp.Idx} is invalid.");
                return Instance<T>.Empty;
            }

            return temp;
        }
    }
}
