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

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using System;

namespace Syadeu.Presentation.Actor
{
    /// <summary>
    /// <see cref="IActorEvent"/> 를 상속받고 <see cref="ActorSystem.ScheduleEvent{TEvent}(Entities.Entity{ActorEntity}, TEvent)"/> 를 수행한 이벤트에 대한 핸들러입니다.
    /// </summary>
    public struct ActorEventHandler : ICustomYieldAwaiter, IEmpty, IValidation, IEquatable<ActorEventHandler>
    {
        public static ActorEventHandler Empty => new ActorEventHandler(Hash.Empty);

        private readonly Hash m_Hash;

        bool ICustomYieldAwaiter.KeepWait => !IsExecuted;

        /// <summary>
        /// 이 Actor Event 가 수행되었나요?
        /// </summary>
        public bool IsExecuted
        {
            get
            {
                if (m_Hash.IsEmpty()) return true;

                ActorSystem system = PresentationSystem<DefaultPresentationGroup, ActorSystem>.System;

                return system.IsExecuted(in this);
            }
        }

        internal ActorEventHandler(Hash hash)
        {
            m_Hash = hash;
        }
        public bool IsEmpty() => m_Hash.Equals(Hash.Empty);
        public bool IsValid() => !IsEmpty();
        public bool Equals(ActorEventHandler other) => m_Hash.Equals(other.m_Hash);
    }
}
