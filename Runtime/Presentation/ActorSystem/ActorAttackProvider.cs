using Newtonsoft.Json;
using Syadeu.Internal;
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
        [JsonIgnore] protected override Type[] ReceiveEventOnly => new Type[] { TypeHelper.TypeOf<IActorAttackEvent>.Type };

        protected override void OnEventReceived<TEvent>(TEvent ev)
        {
            if (ev is IActorAttackEvent attackEvent)
            {
                AttackEventHandler(attackEvent);
            }
        }
        protected void AttackEventHandler(IActorAttackEvent ev)
        {
            Instance<ActorWeaponProvider> weaponProvider = GetProvider<ActorWeaponProvider>();
            if (weaponProvider.IsEmpty()) return;

            Instance<ActorWeaponData> currentWeaponIns = weaponProvider.Object.SelectedWeapon;
            if (currentWeaponIns.IsEmpty() || currentWeaponIns.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity, $"Entity({Parent.Name}) current weapon is invalid");
                return;
            }

            currentWeaponIns.Object.FireFXBounds(FXBounds.TriggerOptions.OnFire);

            for (int i = 0; i < ev.Targets.Length; i++)
            {
                ev.Targets[i].GetController().PostEvent(new TestActorHitEvent());
            }

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
