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

using Syadeu.Collections;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Map;
using Unity.Collections;

namespace Syadeu.Presentation.Events
{
    [System.Obsolete("Use OnGridLocationChangedEvent Instead", true)]
    public sealed class OnGridPositionChangedEvent : SynchronizedEvent<OnGridPositionChangedEvent>
    {
        public Entity<IEntity> Entity { get; private set; }
        public FixedList32Bytes<GridPosition> To { get; private set; }

        public static OnGridPositionChangedEvent GetEvent(Entity<IEntity> entity, FixedList32Bytes<GridPosition> to)
        {
            var temp = Dequeue();
            temp.Entity = entity;
            //temp.From = from;
            temp.To = to;
            return temp;
        }
        public override bool IsValid() => Entity.IsValid();
        protected override void OnTerminate()
        {
            Entity = Entity<IEntity>.Empty;
            //From = null;
            To = default(FixedList32Bytes<GridPosition>);
        }
    }
}
