﻿#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using System.Collections.Generic;
using System.ComponentModel;

namespace Syadeu.Presentation.TurnTable
{
    [DisplayName("TriggerAction: TRPG Find Targets")]
    public sealed class TRPGFindTargetsTriggerAction : TriggerAction
    {
        protected override void OnExecute(Entity<IObject> entity)
        {
#if DEBUG_MODE
            if (!(entity.Target is ActorEntity))
            {
                CoreSystem.Logger.LogError(LogChannel.Entity,
                    $"This entity({entity.Name}) is not a {nameof(ActorEntity)}.");
                return;
            }
#endif
            Entity<ActorEntity> actor = entity.ToEntity<ActorEntity>();
            ActorControllerAttribute ctr = actor.GetController();
#if DEBUG_MODE
            if (!actor.HasComponent<ActorControllerComponent>())
            {
                CoreSystem.Logger.LogError(LogChannel.Entity,
                    $"This entity({entity.Name}) doesn\'t have any {nameof(ActorControllerComponent)}.");
                return;
            }
#endif
            Entity<TRPGActorAttackProvider> attProvider = actor.GetComponent<ActorControllerComponent>().GetProvider<TRPGActorAttackProvider>();
#if DEBUG_MODE
            if (attProvider.IsEmpty())
            {
                CoreSystem.Logger.LogError(LogChannel.Entity,
                    $"This entity({entity.Name}) doesn\'t have any {nameof(TRPGActorAttackProvider)}.");
                return;
            }
#endif
            var list = attProvider.Target.GetTargetsInRange();
            CoreSystem.Logger.Log(LogChannel.Debug,
                $"Entity({entity.Name}) found {list.Length} targets.");
        }
    }
}