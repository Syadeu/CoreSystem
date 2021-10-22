#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Newtonsoft.Json;
using Syadeu.Collections;
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
        protected LogicTriggerAction[] m_OnAttack = Array.Empty<LogicTriggerAction>();
        [JsonProperty(Order = -9, PropertyName = "OnHit")]
        protected LogicTriggerAction[] m_OnHit = Array.Empty<LogicTriggerAction>();

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
#if DEBUG_MODE
            if (m_StatAttribute == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"{Parent.Name} doesn\'t have {nameof(ActorStatAttribute)}");
                return;
            }
#endif
            EntityData<IEntityData> target = ev.AttackFrom.As<ActorEntity, IEntityData>();

            for (int i = 0; i < m_OnHit.Length; i++)
            {
                if (!m_OnHit[i].Schedule(Parent, target))
                {
                    $"{Parent.Name} : hit failed attacked from {target.Name}".ToLog();
                }
            }

            float hp = m_StatAttribute.HP;
            hp -= ev.Damage;
            m_StatAttribute.HP = hp;

            if (hp <= 0)
            {
                ActorLifetimeChangedEvent lifetimeChanged = new ActorLifetimeChangedEvent(ActorLifetimeChangedEvent.State.Dead);
                PostEvent(lifetimeChanged);
            }

            $"{Parent.Name} : {hp} left, dmg {ev.Damage}".ToLog();
        }
        protected virtual void SendHitEvent(Entity<ActorEntity> target, float damage)
        {
            ref ActorControllerComponent component = ref target.GetComponent<ActorControllerComponent>();
            component.ScheduleEvent(new ActorHitEvent(Parent.As<IEntityData, ActorEntity>(), damage));
        }
        protected void AttackEventHandler(ActorAttackEvent ev)
        {
            if (!Parent.HasComponent<ActorWeaponComponent>()) return;

            ActorWeaponComponent component = Parent.GetComponent<ActorWeaponComponent>();

            Instance<ActorWeaponData> currentWeaponIns = component.SelectedWeapon;
            if (currentWeaponIns.IsEmpty() || !currentWeaponIns.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity, $"Entity({Parent.Name}) current weapon is invalid");
                return;
            }

            EntityData<IEntityData> target = ev.Target.GetEntityData<IEntityData>();
            Entity<ActorEntity> parent = Parent.As<IEntityData, ActorEntity>();

            bool isFailed = false;
            for (int i = 0; i < m_OnAttack.Length; i++)
            {
                isFailed |= !m_OnAttack[i].Execute(Parent, target);
            }

            if (isFailed)
            {
                currentWeaponIns.GetObject().FireFXBounds(parent.transform, CoroutineSystem, FXBounds.TriggerOptions.FireOnSuccess);
                SendHitEvent(ev.Target.GetEntity<ActorEntity>(), component.WeaponDamage);
            }
            else
            {
                currentWeaponIns.GetObject().FireFXBounds(parent.transform, CoroutineSystem, FXBounds.TriggerOptions.FireOnFailed);
            }
        }

        public bool IsAlly(Entity<ActorEntity> entity)
        {
            ActorFactionComponent
                my = Parent.GetComponent<ActorFactionComponent>(),
                target = entity.GetComponent<ActorFactionComponent>();

            return my.IsAllies(in target);
        }
        public bool IsEnemy(Entity<ActorEntity> entity)
        {
            ActorFactionComponent
                my = Parent.GetComponent<ActorFactionComponent>(),
                target = entity.GetComponent<ActorFactionComponent>();

            return my.IsEnemies(in target);
        }
    }
}
