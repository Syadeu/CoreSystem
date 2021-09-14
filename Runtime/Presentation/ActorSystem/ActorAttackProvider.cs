using Newtonsoft.Json;
using Syadeu.Internal;
using Syadeu.Presentation.Entities;
using System;
using UnityEngine;

namespace Syadeu.Presentation.Actor
{
    public class ActorAttackProvider : ActorProviderBase
    {
        protected override Type[] ReceiveEventOnly => new Type[] { TypeHelper.TypeOf<IActorAttackEvent>.Type };

        protected override void OnEventReceived<TEvent>(TEvent ev)
        {
            if (ev is IActorAttackEvent attackEvent)
            {
                AttackEventHandler(attackEvent);
            }
        }
        protected virtual void AttackEventHandler(IActorAttackEvent ev)
        {
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
    }
}
