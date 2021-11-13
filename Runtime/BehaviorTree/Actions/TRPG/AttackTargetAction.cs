#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using BehaviorDesigner.Runtime.Tasks;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Actor;
using System;
using Syadeu.Presentation.Components;

namespace Syadeu.Presentation.BehaviorTree
{
#if CORESYSTEM_TURNBASESYSTEM
    using Syadeu.Presentation.TurnTable;
    using UnityEngine;

    [TaskCategory("Entity/Actor/TRPG")]
    [TaskDescription(
        "AttackOptions 값에 따라 찾은 타겟을 공격합니다." +
        "FindTargetsAction 이 이전에 먼저 수행되어 타겟을 찾아야됩니다.")]
    public sealed class AttackTargetAction : ActionBase
    {
        public enum AttackOptions
        {
            Closest,
            Distant,
            Middle
        }

        [SerializeField] private AttackOptions m_AttackOptions = AttackOptions.Closest;

        public override TaskStatus OnUpdate()
        {
#if DEBUG_MODE
            if (!Entity.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Invalid Target at {nameof(FindTargetsAction)}.");
                return TaskStatus.Failure;
            }
            else if (!Entity.HasComponent<ActorControllerComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({Entity.RawName}) doeesn\'t have {nameof(ActorControllerAttribute)}");
                return TaskStatus.Failure;
            }
            else if (!Entity.HasComponent<TRPGActorAttackComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({Entity.RawName}) doeesn\'t have {nameof(TRPGActorAttackComponent)}");
                return TaskStatus.Failure;
            }
#endif

            var att = Entity.GetComponent<TRPGActorAttackComponent>();
            if (att.TargetCount == 0) return TaskStatus.Failure;

            var ctr = Entity.GetComponent<ActorControllerComponent>();
#if DEBUG_MODE
            if (!ctr.HasProvider<TRPGActorAttackProvider>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({Entity.RawName}) doeesn\'t have {nameof(TRPGActorAttackProvider)}");
                return TaskStatus.Failure;
            }
#endif
            var attProvider = ctr.GetProvider<TRPGActorAttackProvider>();

            switch (m_AttackOptions)
            {
                default:
                case AttackOptions.Closest:
                    attProvider.GetObject().Attack(att.GetTargetAt(0).GetEntity<ActorEntity>());
                    break;
                case AttackOptions.Distant:
                    attProvider.GetObject().Attack(att.GetTargetAt(att.TargetCount - 1).GetEntity<ActorEntity>());
                    break;
                case AttackOptions.Middle:
                    int index = att.TargetCount / 2;
                    attProvider.GetObject().Attack(att.GetTargetAt(index).GetEntity<ActorEntity>());
                    break;
            }

            return TaskStatus.Success;
        }
    }
#endif
}
