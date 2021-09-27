﻿using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Entities;
using System;
using System.ComponentModel;
using UnityEngine;

namespace Syadeu.Presentation.Actor
{
    [DisplayName("ActorProvider: Attack Provider")]
    [ActorProviderRequire(typeof(ActorWeaponProvider))]
    public class ActorAttackProvider : ActorProviderBase
    {
        [JsonProperty(Order = -10, PropertyName = "OnAttack")]
        protected LogicTriggerAction m_OnAttack = new LogicTriggerAction();
        [JsonProperty(Order = -9, PropertyName = "OnHit")]
        protected LogicTriggerAction m_OnHit = new LogicTriggerAction();

        [JsonIgnore] private ActorStatAttribute m_StatAttribute;

        protected override void OnCreated(Entity<ActorEntity> entity)
        {
            m_StatAttribute = entity.GetAttribute<ActorStatAttribute>();
            if (m_StatAttribute == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({entity.Name}) doesn\'t have any {nameof(ActorStatAttribute)}.");
            }
        }
        protected override void OnEventReceived<TEvent>(TEvent ev)
        {
            if (ev is IActorAttackEvent attackEvent)
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
#if UNITY_EDITOR
            if (m_StatAttribute == null) return;
#endif
            EntityData<IEntityData>
                    parent = Parent.As<ActorEntity, IEntityData>(),
                    target = ev.AttackFrom.As<ActorEntity, IEntityData>();

            if (!m_OnHit.Execute(parent, target))
            {
                $"{Parent.Name} : hit failed attacked from {target.Name}".ToLog();
                return;
            }

            int hp = m_StatAttribute.GetValue<int>(ev.HPStatNameHash);
            hp -= Mathf.RoundToInt(ev.Damage);
            m_StatAttribute.SetValue(ev.HPStatNameHash, hp);

            if (hp <= 0)
            {
                ActorLifetimeChangedEvent lifetimeChanged = new ActorLifetimeChangedEvent(ActorLifetimeChangedEvent.State.Dead);
                PostEvent(lifetimeChanged);
            }

            $"{Parent.Name} : {hp} left".ToLog();
        }
        protected virtual void SendHitEvent(Entity<ActorEntity> target, Hash hpStatName, float damage)
        {
            ActorControllerComponent component = target.GetComponent<ActorControllerComponent>();
            component.ScheduleEvent(new ActorHitEvent(Parent, hpStatName, damage));
        }
        protected void AttackEventHandler(IActorAttackEvent ev)
        {
            Instance<ActorWeaponProvider> weaponProvider = GetProvider<ActorWeaponProvider>();
            if (weaponProvider.IsEmpty()) return;

            Instance<ActorWeaponData> currentWeaponIns = weaponProvider.Object.SelectedWeapon;
            if (currentWeaponIns.IsEmpty() || !currentWeaponIns.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity, $"Entity({Parent.Name}) current weapon is invalid");
                return;
            }

            EntityData<IEntityData>
                    parent = Parent.As<ActorEntity, IEntityData>(),
                    target = ev.Target.As<ActorEntity, IEntityData>();

            

            if (m_OnAttack.Schedule(parent, target))
            {
                currentWeaponIns.Object.FireFXBounds(Parent.transform, CoroutineSystem, FXBounds.TriggerOptions.FireOnSuccess);
                SendHitEvent(ev.Target, ev.HPStatNameHash, ev.Damage);
            }
            else
            {
                currentWeaponIns.Object.FireFXBounds(Parent.transform, CoroutineSystem, FXBounds.TriggerOptions.FireOnFailed);
            }

            //for (int i = 0; i < ev.Targets.Length; i++)
            //{
            //    ev.Targets[i].GetController().PostEvent(new TestActorHitEvent());
            //}

            //var weaponProvider = GetProvider<ActorWeaponProvider>();
            //if (weaponProvider.IsEmpty())
            //{
            //    CoreSystem.Logger.LogError(Channel.Entity,
            //           $"This entity({Parent.Name}) doesn\'t have any {nameof(ActorWeaponProvider)}.");
            //    return;
            //}

            //for (int i = 0; i < ev.Targets.Length; i++)
            //{
            //    var targetStat = ev.Targets[i].GetAttribute<ActorEntity, ActorStatAttribute>();
            //    if (targetStat == null)
            //    {
            //        CoreSystem.Logger.LogError(Channel.Entity,
            //            $"Target entity({ev.Targets[i].Object.Name}) doesn\'t have any {nameof(ActorStatAttribute)}.");
            //        continue;
            //    }

            //    int hp = targetStat.GetValue<int>(ev.HPStatNameHash);
            //    hp -= Mathf.RoundToInt(weaponProvider.Object.WeaponDamage);
            //    targetStat.SetValue(ev.HPStatNameHash, hp);
            //}
        }

        public bool IsAlly(Entity<ActorEntity> entity) => Parent.Target.Faction.IsAlly(entity.Target.Faction);
        public bool IsEnemy(Entity<ActorEntity> entity) => Parent.Target.Faction.IsEnemy(entity.Target.Faction);
    }
}
