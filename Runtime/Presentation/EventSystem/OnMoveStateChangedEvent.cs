// Copyright 2021 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Attributes;
using System;
using Syadeu.Collections;

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
        public override bool DisplayLog => false;

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
