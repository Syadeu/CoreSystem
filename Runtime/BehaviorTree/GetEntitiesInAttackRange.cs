using BehaviorDesigner.Runtime.Tasks;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Map;
using Syadeu.Presentation.TurnTable;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Presentation.BehaviorTree
{
    [TaskCategory("Entity/Actor")]
    [TaskDescription(
        "주변 엔티티들을 TRPGActorAttackProvider의 AttackRange 값으로 불러옵니다.\n" +
        "만약 하나도 찾지 못했다면 Failure 를 반환합니다."
        )]
    public sealed class GetEntitiesInAttackRange : GetEntitiesActionBase
    {
        [SerializeField] private LoadActorControllerAttribute m_ThisActorController;

        private IReadOnlyList<Entity<IEntity>> m_Result;

        public override IReadOnlyList<Entity<IEntity>> Result => m_Result;

        public override TaskStatus OnUpdate()
        {
            Instance<TRPGActorAttackProvider> attackProvider = m_ThisActorController.GetProvider<TRPGActorAttackProvider>();
            if (!attackProvider.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({m_ThisActorController.ActorController.Parent.Name}) " +
                    $"doesn\'t have any {nameof(TRPGActorAttackProvider)}");

                return TaskStatus.Failure;
            }

            m_Result = attackProvider.Object.GetTargetsInRange();

            if (m_Result.Count > 0) return TaskStatus.Success;
            return TaskStatus.Failure;
        }
    }
}
