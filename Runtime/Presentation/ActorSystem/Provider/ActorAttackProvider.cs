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

using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Internal;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using System;
using System.ComponentModel;
using UnityEngine;

namespace Syadeu.Presentation.Actor
{
    [DisplayName("ActorProvider: Attack Provider")]
    [ActorProviderRequire(typeof(ActorWeaponProvider))]
    public class ActorAttackProvider : ActorProviderBase<ActorAttackComponent>
    {
        [SerializeField, JsonProperty(Order = -10, PropertyName = "OnAttack")]
        protected ArrayWrapper<LogicTriggerAction> m_OnAttack = Array.Empty<LogicTriggerAction>();
        [SerializeField, JsonProperty(Order = -9, PropertyName = "OnHit")]
        protected ArrayWrapper<LogicTriggerAction> m_OnHit = Array.Empty<LogicTriggerAction>();

        protected override void OnEventReceived(IActorEvent ev)
        {
            if (ev is ActorAttackEvent attackEvent)
            {
                AttackEventHandler(attackEvent);
            }
            else if (ev is IActorHitEvent hitEvent)
            {
                HitEventHandler(hitEvent);
            }
        }
        protected void HitEventHandler(IActorHitEvent ev)
        {
            ref ActorStatComponent stat = ref Parent.GetComponent<ActorStatComponent>();
            Entity<IEntityData> target = ev.AttackFrom.ToEntity<IEntityData>();

            float hp = stat.HP;
            hp -= ev.Damage;
            stat.HP = hp;

            if (hp <= 0)
            {
                ActorLifetimeChangedEvent lifetimeChanged = new ActorLifetimeChangedEvent(ActorLifetimeChangedEvent.State.Dead);
                ActorSystem.PostEvent(Parent.ToEntity<ActorEntity>(), lifetimeChanged);
            }
            else
            {
                for (int i = 0; i < m_OnHit.Length; i++)
                {
                    if (!m_OnHit[i].Schedule(Parent, target))
                    {
                        $"{Parent.Name} : hit failed attacked from {target.Name}".ToLog();
                    }
                }
            }

            $"{Parent.Name} : {hp} left, dmg {ev.Damage}".ToLog();
        }
        private void SendHitEvent(Entity<ActorEntity> target, float damage)
        {
            ScheduleEvent(target, new ActorHitEvent(Parent.ToEntity<ActorEntity>(), damage));
        }
        protected void AttackEventHandler(ActorAttackEvent ev)
        {
            if (!Parent.HasComponent<ActorWeaponComponent>()) return;

            ActorWeaponComponent component = Parent.GetComponent<ActorWeaponComponent>();

            InstanceID currentWeaponIns = component.SelectedWeapon;
            if (currentWeaponIns.IsEmpty() || !currentWeaponIns.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity, $"Entity({Parent.Name}) current weapon is invalid");
                return;
            }

            Entity<IEntityData> target = ev.Target.GetEntity<IEntityData>();
            Entity<ActorEntity> parent = Parent.ToEntity<ActorEntity>();

            bool isFailed = false;
            for (int i = 0; i < m_OnAttack.Length; i++)
            {
                isFailed |= !m_OnAttack[i].Execute(Parent, target);
            }

            if (!isFailed)
            {
                "TODO : DO FX".ToLog();
                //currentWeaponIns.GetEntity().Target.FireFXBounds(parent.transform, FXBounds.TriggerOptions.FireOnSuccess);
                SendHitEvent(ev.Target.GetEntity<ActorEntity>(), component.WeaponDamage);
            }
            else
            {
                "TODO : DO FX".ToLog();
                //currentWeaponIns.Target.FireFXBounds(parent.transform, FXBounds.TriggerOptions.FireOnFailed);
            }
        }

        public bool IsAlly(Entity<ActorEntity> entity)
        {
            ActorFactionComponent
                my = Parent.GetComponent<ActorFactionComponent>(),
                target = entity.GetComponent<ActorFactionComponent>();

            return my.IsAlly(in target);
        }
        public bool IsEnemy(Entity<ActorEntity> entity)
        {
            ActorFactionComponent
                my = Parent.GetComponent<ActorFactionComponent>(),
                target = entity.GetComponent<ActorFactionComponent>();

            return my.IsEnemy(in target);
        }
    }

    public struct ActorAttackComponent : IActorProviderComponent
    {

    }
}
