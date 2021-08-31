using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Attributes;
using System;

namespace Syadeu.Presentation.Events
{
    /// <summary>
    /// <see cref="Entity{T}"/>의 이동 상태가 변경되었을때 호출되는 이벤트 객체입니다.
    /// </summary>
    /// <remarks>
    /// 현재 <seealso cref="NavAgentAttribute"/> 에서만 호출중
    /// </remarks>
    public sealed class OnMoveStateChangedEvent : SynchronizedEvent<OnMoveStateChangedEvent>
    {
        [Flags]
        public enum MoveState
        {
            Idle        =   0b00001,
            AboutToMove =   0b00010,
            OnMoving    =   0b00100,
            Teleported  =   0b01000,
            Stopped     =   0b10000
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
        public override bool IsValid() => Entity.IsValid();
        protected override void OnTerminate()
        {
            Entity = Entity<IEntity>.Empty;
        }
    }
}
