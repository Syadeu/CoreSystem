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
using Syadeu.Collections.Proxy;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Proxy;
using System;

namespace Syadeu.Presentation.Events
{
    /// <summary>
    /// <see cref="Entity{T}"/>의 <see cref="ITransform"/>이 수정될 때 발생되는 이벤트입니다.
    /// </summary>
    /// <remarks>
    /// 현재는 <seealso cref="ITransform.position"/>, <seealso cref="ITransform.rotation"/>, <seealso cref="ITransform.scale"/> 값이 수정되었을때만 호출됩니다.
    /// </remarks>
    public sealed class OnTransformChangedEvent : SynchronizedEvent<OnTransformChangedEvent>
    {
        public override UpdateLoop Loop => UpdateLoop.Transform;

#pragma warning disable IDE1006 // Naming Styles
        public Entity<IEntity> entity { get; private set; }
        public ProxyTransform transform { get; private set; }
#pragma warning restore IDE1006 // Naming Styles

        public static OnTransformChangedEvent GetEvent(ProxyTransform tr)
        {
            var temp = Dequeue();

            if (tr is ProxyTransform proxyTr)
            {
                EntitySystem entitySystem = PresentationSystem<DefaultPresentationGroup, EntitySystem>.System;
                ObjectBase entity = entitySystem.GetEntityByTransform(proxyTr);
                if (entity == null)
                {
                    temp.entity = Entity<IEntity>.Empty;
                }
                else temp.entity = Entity<IEntity>.GetEntityWithoutCheck(entity.Idx);
            }
            //else if (tr is UnityTransform unityTr)
            //{
            //    temp.entity = unityTr.entity;
            //}
            else throw new NotImplementedException();

            temp.transform = tr;

            return temp;
        }
        public override bool IsValid() => entity.IsValid();
        protected override void OnTerminate()
        {
            entity = Entity<IEntity>.Empty;
            transform = ProxyTransform.Null;
        }
    }
}
