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
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.TurnTable
{
    public static class TRPGExtenstionMethods
    {
        public static void Attack(this Entity<ActorEntity> other, Entity<ActorEntity> target)
        {
            ActorAttackEvent ev = new ActorAttackEvent(target);
            ev.ScheduleEvent(other);
        }
        public static void Attack(this Entity<ActorEntity> other, int index)
        {
            if (!other.HasComponent<ActorControllerComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This entity({other.Name}) doesn\'t have any {nameof(ActorControllerComponent)}.");
                return;
            }

            
//#if DEBUG_MODE
            if (!other.HasComponent<TRPGActorAttackComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This entity({other.Name}) doesn\'t have any {nameof(TRPGActorAttackProvider)}.");
                return;
            }

            var attProvider = other.GetComponent<TRPGActorAttackComponent>();

            if (attProvider.TargetCount < index)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Index({index}) is out of range. Target count is {attProvider.TargetCount}.");
                return;
            }
//#endif
            var weapon = other.GetComponent<ActorWeaponComponent>();

            ActorAttackEvent ev = new ActorAttackEvent(
                attProvider.GetTargetAt(index).GetEntity<ActorEntity>());

            ev.ScheduleEvent(other);
        }
    }
}

