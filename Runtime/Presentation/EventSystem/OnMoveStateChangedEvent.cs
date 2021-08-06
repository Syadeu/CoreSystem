using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Attributes;

namespace Syadeu.Presentation.Event
{
    /// <summary>
    /// <see cref="Entity{T}"/>의 이동 상태가 변경되었을때 호출되는 이벤트 객체입니다.
    /// </summary>
    /// <remarks>
    /// 현재 <seealso cref="NavAgentAttribute"/> 에서만 호출중
    /// </remarks>
    public sealed class OnMoveStateChangedEvent : SynchronizedEvent<OnMoveStateChangedEvent>
    {
        public enum MoveState
        {
            None,

            Idle,

            AboutToMove,
            OnMoving,

            Teleported,
            Stopped
        }

        public Entity<IEntity> Entity { get; private set; }
        public MoveState State { get; private set; }

        public static OnMoveStateChangedEvent GetEvent(Entity<IEntity> entity, MoveState state)
        {
            var temp = Dequeue();
            temp.Entity = entity;
            temp.State = state;
            return temp;
        }
        protected override void OnTerminate()
        {
            Entity = Entity<IEntity>.Empty;
            State = MoveState.None;
        }
    }
}
