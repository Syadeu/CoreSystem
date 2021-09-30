using BehaviorDesigner.Runtime.Tasks;
using Syadeu.Presentation.TurnTable;
using UnityEngine;

namespace Syadeu.Presentation.BehaviorTree
{
    [TaskCategory("Entity/Actor")]
    public sealed class NextTurn : Action
    {
        [SerializeField] private SharedRecycleableMonobehaviour m_This;

        public override TaskStatus OnUpdate()
        {
            if (!m_This.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Invalid Target at {nameof(NextTurn)}.");
                return TaskStatus.Failure;
            }

            
            if (!m_This.HasComponent<TurnPlayerComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"{m_This.GetEntity().Name} doesn\'t have any {nameof(TurnPlayerComponent)}.");
                return TaskStatus.Failure;
            }

            TurnPlayerComponent turnPlayer = m_This.GetComponent<TurnPlayerComponent>();

            if (!turnPlayer.IsMyTurn)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"It is not a entity({m_This.GetEntity().Name}) turn.");
                return TaskStatus.Failure;
            }

            TurnTableManager.NextTurn();

            return TaskStatus.Success;
        }
    }
}
