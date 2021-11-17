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

using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Events
{
    public sealed class OnActorStateChangedEvent : SynchronizedEvent<OnActorStateChangedEvent>
    {
        public Entity<ActorEntity> Entity { get; private set; }
        public ActorStateAttribute.StateInfo Previous { get; private set; }
        public ActorStateAttribute.StateInfo Current { get; private set; }

        public static OnActorStateChangedEvent GetEvent(
            Entity<ActorEntity> actor, ActorStateAttribute.StateInfo prev, ActorStateAttribute.StateInfo cur)
        {
            var temp = Dequeue();

            temp.Entity = actor;
            temp.Previous = prev;
            temp.Current = cur;

            return temp;
        }
        protected override void OnTerminate()
        {
            Entity = Entity<ActorEntity>.Empty;
            Previous = 0;
            Current = 0;
        }
    }
}
