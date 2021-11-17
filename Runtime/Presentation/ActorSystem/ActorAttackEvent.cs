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

using Syadeu.Collections;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Actor
{
    public struct ActorAttackEvent : IActorEvent
    {
        private EntityID m_Target;

        public bool BurstCompile => true;
        public EntityID Target => m_Target;

        public ActorAttackEvent(IEntityDataID target)
        {
            m_Target = target.Idx;
        }

        void IActorEvent.OnExecute(Entity<ActorEntity> from)
        {

        }

        [UnityEngine.Scripting.Preserve]
        static void AOTCodeGeneration()
        {
            ActorSystem.AOTCodeGenerator<ActorAttackEvent>();

            throw new System.InvalidOperationException();
        }
    }
}
