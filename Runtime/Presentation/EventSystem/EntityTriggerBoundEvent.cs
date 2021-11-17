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
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Events
{
    /// <summary>
    /// <see cref="TriggerBoundAttribute"/> 를 가진 <see cref="Entity{T}"/>가 설정한 바운더리에 들어왔을때 트리거되는 이벤트입니다.
    /// </summary>
    /// <remarks>
    /// <see cref="Source"/>를 기준으로 <see cref="Target"/>이 들어왔음을 알립니다.
    /// </remarks>
    public sealed class EntityTriggerBoundEvent : SynchronizedEvent<EntityTriggerBoundEvent>
    {
        public Entity<IEntity> Source { get; private set; }
        public Entity<IEntity> Target { get; private set; }
        public bool IsEnter { get; private set; }

        public static EntityTriggerBoundEvent GetEvent(Entity<IEntity> source, Entity<IEntity> target, bool isEnter)
        {
            var temp = Dequeue();

            temp.Source = source;
            temp.Target = target;
            temp.IsEnter = isEnter;

            return temp;
        }
        public override bool IsValid() => Source.IsValid() && Target.IsValid();
        protected override void OnTerminate()
        {
            Source = Entity<IEntity>.Empty;
            Target = Entity<IEntity>.Empty;
        }
    }
}
