using Syadeu.Collections;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Entities;
using System.Collections.Generic;
using System.ComponentModel;

namespace Syadeu.Presentation.TurnTable
{
    [DisplayName("TriggerAction: TRPG Find Targets")]
    public sealed class TRPGFindTargetsTriggerAction : TriggerAction
    {
        protected override void OnExecute(EntityData<IEntityData> entity)
        {
            if (!(entity.Target is ActorEntity))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This entity({entity.Name}) is not a {nameof(ActorEntity)}.");
                return;
            }

            Entity<ActorEntity> actor = entity.As<IEntityData, ActorEntity>();
            ActorControllerAttribute ctr = actor.GetController();

            if (!actor.HasComponent<ActorControllerComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This entity({entity.Name}) doesn\'t have any {nameof(ActorControllerComponent)}.");
                return;
            }

            Instance<TRPGActorAttackProvider> attProvider = actor.GetComponent<ActorControllerComponent>().GetProvider<TRPGActorAttackProvider>();

            if (attProvider.IsEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This entity({entity.Name}) doesn\'t have any {nameof(TRPGActorAttackProvider)}.");
                return;
            }

            IReadOnlyList<Entity<IEntity>> list = attProvider.Object.GetTargetsInRange();
            CoreSystem.Logger.Log(Channel.Debug,
                $"Entity({entity.Name}) found {list.Count} targets.");
        }
    }
}