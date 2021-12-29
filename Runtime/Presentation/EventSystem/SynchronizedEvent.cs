﻿// Copyright 2021 Seung Ha Kim
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

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using Syadeu.Internal;
using System;
using System.Collections.Generic;

namespace Syadeu.Presentation
{
    /// <summary>
    /// <see cref="Events.EventSystem"/>에서 호출되는 이벤트를 작성할 수 있는 기본 <see langword="abstract"/>입니다.
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    public abstract class SynchronizedEvent<TEvent> : SynchronizedEventBase where TEvent : SynchronizedEvent<TEvent>, new()
    {
        internal static readonly Hash s_Key = Hash.NewHash(TypeHelper.TypeOf<TEvent>.Name);
        private static readonly Queue<TEvent> m_Pool = new Queue<TEvent>();

        private static Unity.Profiling.ProfilerMarker
            s_Marker = new Unity.Profiling.ProfilerMarker($"Execute Event ({TypeHelper.TypeOf<TEvent>.ToString()})");

        private static readonly Dictionary<int, ActionWrapper<TEvent>>
            s_EventActions = new Dictionary<int, ActionWrapper<TEvent>>();

        public override sealed string Name => TypeHelper.TypeOf<TEvent>.Name;
        public override sealed Type EventType => TypeHelper.TypeOf<TEvent>.Type;

        internal static void AddEvent(Action<TEvent> ev)
        {
            int hash = ev.GetHashCode();
            if (s_EventActions.ContainsKey(hash))
            {
                CoreSystem.Logger.LogError(Channel.Event,
                    $"Already added event delegate. This is not allowed.");
                return;
            }

            var temp = ActionWrapper<TEvent>.GetWrapper();
            temp.SetProfiler($"{ev.Method.DeclaringType.Name}.{ev.Method.Name}");
            temp.SetAction(ev);
            s_EventActions.Add(hash, temp);

            EventDescriptor<TEvent>.AddEvent(s_Key, temp.Invoke);
        }
        internal static void RemoveEvent(Action<TEvent> ev)
        {
            int hash = ev.GetHashCode();
            if (!s_EventActions.TryGetValue(hash, out var temp))
            {
                CoreSystem.Logger.LogError(Channel.Event,
                    $"Event({TypeHelper.TypeOf<TEvent>.Name}) " +
                    $"doesn\'t have method({ev.Method.DeclaringType.Name}.{ev.Method.Name}) but you trying to remove.");

                return;
            }

            EventDescriptor<TEvent>.RemoveEvent(s_Key, temp.Invoke);
            temp.Reserve();
            s_EventActions.Remove(hash);
        }
        
        internal override sealed void InternalPost()
        {
            using (s_Marker.Auto())
            {
                EventDescriptor<TEvent>.Invoke(s_Key, (TEvent)this);
            }
        }
        internal override sealed void InternalTerminate()
        {
            OnTerminate();

            m_Pool.Enqueue((TEvent)this);
        }

        public static TEvent Dequeue()
        {
            if (m_Pool.Count == 0)
            {
                TEvent temp = new TEvent();
                return temp;
            }
            return m_Pool.Dequeue();
        }
        protected abstract void OnTerminate();
    }
}
