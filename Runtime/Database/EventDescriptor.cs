using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Syadeu.Collections
{
    public class EventDescriptor<T> : CLRSingleTone<EventDescriptor<T>>
    {
        private readonly ConcurrentDictionary<Hash, Action<T>> m_Events = new ConcurrentDictionary<Hash, Action<T>>();

        public static void AddEvent(string key, Action<T> e)
            => AddEvent(Hash.NewHash(key), e);
        public static void AddEvent(Hash key, Action<T> e)
        {
            if (!Instance.m_Events.TryGetValue(key, out Action<T> value))
            {
                Instance.m_Events.TryAdd(key, e);
                return;
            }

            Instance.m_Events[key] += e;
        }

        public static void RemoveEvent(string key, Action<T> e)
            => RemoveEvent(Hash.NewHash(key), e);
        public static void RemoveEvent(Hash key, Action<T> e)
        {
            if (!Instance.m_Events.TryGetValue(key, out Action<T> value))
            {
                return;
            }

            Instance.m_Events[key] -= e;
        }

        public static void Invoke(string key, T t) => Invoke(Hash.NewHash(key), t);
        public static void Invoke(Hash key, T t)
        {
            if (!Instance.m_Events.TryGetValue(key, out var value)) return;
            value.Invoke(t);
        }

        public override void Dispose()
        {
            m_Events.Clear();
        }
    } 
}
