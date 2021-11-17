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

namespace Syadeu.Presentation.Events
{
    public sealed class OnGridDetectEntityEvent : SynchronizedEvent<OnGridDetectEntityEvent>
    {
        public Entity<IEntity> Detector { get; private set; }
        public Entity<IEntity> Target { get; private set; }

        public bool Detected { get; private set; }

        public static OnGridDetectEntityEvent GetEvent(Entity<IEntity> detector, Entity<IEntity> target, bool isDetect)
        {
            var temp = Dequeue();
            temp.Detector = detector;
            temp.Target = target;
            temp.Detected = isDetect;
            return temp;
        }
        public override bool IsValid() => Detector.IsValid() && Target.IsValid();
        protected override void OnTerminate()
        {
            Detector = Entity<IEntity>.Empty;
            Target = Entity<IEntity>.Empty;
        }
    }
}
