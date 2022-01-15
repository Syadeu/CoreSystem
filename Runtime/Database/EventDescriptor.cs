// Copyright 2021 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Syadeu.Collections
{
    public class EventDescriptor<TEvent> : IEventDescriptor, IDisposable
    {
        private readonly ConcurrentDictionary<Hash, Action<TEvent>> m_Events = new ConcurrentDictionary<Hash, Action<TEvent>>();

        public void AddEvent(string key, Action<TEvent> e) => AddEvent(Hash.NewHash(key), e);
        public void AddEvent(Hash key, Action<TEvent> e)
        {
            if (!m_Events.TryGetValue(key, out Action<TEvent> value))
            {
                m_Events.TryAdd(key, e);
                return;
            }

            m_Events[key] += e;
        }

        void IEventDescriptor.AddEvent(string key, Delegate e) => AddEvent(key, (Action<TEvent>)e);
        void IEventDescriptor.AddEvent(Hash key, Delegate e) => AddEvent(key, (Action<TEvent>)e);

        public void RemoveEvent(string key, Action<TEvent> e) => RemoveEvent(Hash.NewHash(key), e);
        public void RemoveEvent(Hash key, Action<TEvent> e)
        {
            if (!m_Events.ContainsKey(key))
            {
                return;
            }

            m_Events[key] -= e;
            var invocationList = m_Events[key].GetInvocationList();
            if (invocationList == null || invocationList.Length == 0)
            {
                m_Events.TryRemove(key, out _);
            }
        }

        void IEventDescriptor.RemoveEvent(string key, Delegate e) => RemoveEvent(key, (Action<TEvent>)e);
        void IEventDescriptor.RemoveEvent(Hash key, Delegate e) => RemoveEvent(key, (Action<TEvent>)e);

        void IEventDescriptor.Invoke(string key, object ev) => Invoke(Hash.NewHash(key), (TEvent)ev);
        void IEventDescriptor.Invoke(Hash key, object ev) => Invoke(key, (TEvent)ev);

        public void Invoke(string key, TEvent t) => Invoke(Hash.NewHash(key), t);
        public void Invoke(Hash key, TEvent t)
        {
            if (!m_Events.TryGetValue(key, out var value)) return;
            value?.Invoke(t);
        }

        public void Dispose()
        {
            m_Events.Clear();
        }
    }
    public interface IEventDescriptor : IDisposable
    {
        void AddEvent(string key, Delegate e);
        void AddEvent(Hash key, Delegate e);

        void RemoveEvent(string key, Delegate e);
        void RemoveEvent(Hash key, Delegate e);

        void Invoke(string key, object ev);
        void Invoke(Hash key, object ev);
    }
}
