// Copyright 2022 Seung Ha Kim
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

namespace Syadeu.Presentation.Actor
{
    public sealed class ActorOnInteractionEvent : SynchronizedEvent<ActorOnInteractionEvent>
    {
        /// <summary>
        /// <seealso cref="ActorInterationProvider"/>
        /// </summary>
        public InstanceID Actor { get; private set; }
        /// <summary>
        /// <seealso cref="InteractableComponent"/>
        /// </summary>
        public InstanceID Target { get; private set; }

        public static ActorOnInteractionEvent GetEvent(InstanceID actor, InstanceID target)
        {
            var ev = Dequeue();

            ev.Actor = actor;
            ev.Target = target;

            return ev;
        }
        protected override void OnTerminate()
        {
            Actor = InstanceID.Empty;
            Target = InstanceID.Empty;
        }
    }
}
