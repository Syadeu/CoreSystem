using BehaviorDesigner.Runtime.Tasks;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Components;

namespace Syadeu.Presentation.BehaviorTree
{
    using Syadeu.Collections;
#if CORESYSTEM_TURNBASESYSTEM
    using Syadeu.Presentation.TurnTable;

    [TaskCategory("Entity/Actor/TRPG")]
    [TaskDescription(
        "이 Actor 의 턴을 넘깁니다.")]
    public sealed class NextTurnAction : ActionBase
    {
        public override TaskStatus OnUpdate()
        {
            if (!Entity.IsValid())
            {
                CoreSystem.Logger.LogError(LogChannel.Entity,
                    $"Invalid Target at {nameof(NextTurnAction)}.");
                return TaskStatus.Failure;
            }

            if (!Entity.HasComponent<TurnPlayerComponent>())
            {
                CoreSystem.Logger.LogError(LogChannel.Entity,
                    $"{Entity.RawName} doesn\'t have any {nameof(TurnPlayerComponent)}.");
                return TaskStatus.Failure;
            }

            TurnPlayerComponent turnPlayer = Entity.GetComponent<TurnPlayerComponent>();

            if (!turnPlayer.IsMyTurn)
            {
                CoreSystem.Logger.LogError(LogChannel.Entity,
                    $"It is not a entity({Entity.RawName}) turn.");
                return TaskStatus.Failure;
            }

            PresentationSystem<DefaultPresentationGroup, EventSystem>
                .System.ScheduleEvent(TRPGEndTurnEvent.GetEvent());
            "next turn call".ToLog();

            return TaskStatus.Success;
        }
    }
#endif
}
