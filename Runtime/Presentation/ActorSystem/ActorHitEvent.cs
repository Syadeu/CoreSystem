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
    public struct ActorHitEvent : IActorHitEvent
    {
        private Entity<ActorEntity> m_AttackFrom;
        private float m_Damage;

        bool IActorEvent.BurstCompile => true;

        public Entity<ActorEntity> AttackFrom => m_AttackFrom;
        public float Damage => m_Damage;

        public ActorHitEvent(Entity<ActorEntity> attackFrom, float damage)
        {
            m_AttackFrom = attackFrom;
            m_Damage = damage;
        }

        public void OnExecute(Entity<ActorEntity> from)
        {
            UnityEngine.Debug.Log($"{from.RawName} hit");
        }
    }
}
