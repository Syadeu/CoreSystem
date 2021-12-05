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
using Syadeu.Internal;
using Syadeu.Mono;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Internal;
using System;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Attributes
{
    /// <summary>
    /// <see cref="AttributeBase"/>의 동작부를 선언할 수 있습니다.
    /// </summary>
    /// <remarks>
    /// 참조: <seealso cref="IAttributeOnProxy"/>, <seealso cref="IAttributeOnProxyCreated"/>, <seealso cref="IAttributeOnProxyRemoved"/>
    /// </remarks>
    [Preserve, Obsolete("Use EntityProcessor")]
    public abstract class AttributeProcessor : ProcessorBase, IAttributeProcessor
    {
        internal override void InternalOnCreated(ObjectBase obj)
        {
            throw new NotImplementedException();
        }
        internal override void InternalOnDestroy(ObjectBase obj)
        {
            throw new NotImplementedException();
        }

        void IAttributeProcessor.OnCreated(IAttribute attribute, EntityData<IEntityData> entity) => OnCreated(attribute, entity);
        void IAttributeProcessor.OnDestroy(IAttribute attribute, EntityData<IEntityData> entity) => OnDestroy(attribute, entity);

        /// <summary>
        /// <see cref="Target"/>에 설정된 <see cref="ObjectBase"/>가 부착된 <see cref="IEntityData"/>가
        /// 생성되었을 때 실행되는 메소드입니다.
        /// </summary>
        /// <remarks>
        /// 비동기 작업입니다. Unity API에 접근하면 에러를 뱉습니다.
        /// </remarks>
        /// <param name="attribute"><see cref="Target"/></param>
        /// <param name="entity"></param>
        protected virtual void OnCreated(IAttribute attribute, EntityData<IEntityData> entity) { }
        /// <summary>
        /// <see cref="Target"/>에 설정된 <see cref="AttributeBase"/>가 부착된 <see cref="IEntityData"/>가
        /// 파괴되었을 때 실행되는 메소드입니다.
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="entity"></param>
        /// <remarks>
        /// 비동기 작업입니다. Unity API에 접근하면 에러를 뱉습니다.
        /// </remarks>
        protected virtual void OnDestroy(IAttribute attribute, EntityData<IEntityData> entity) { }
    }
    /// <inheritdoc cref="AttributeProcessor"/>
    /// <typeparam name="T"></typeparam>
    [Preserve, Obsolete("Use EntityProcessor")]
    public abstract class AttributeProcessor<T> : ProcessorBase, IAttributeProcessor 
        where T : AttributeBase
    {
        internal override void InternalOnCreated(ObjectBase obj)
        {
            throw new NotImplementedException();
        }
        internal override void InternalOnDestroy(ObjectBase obj)
        {
            throw new NotImplementedException();
        }

        void IAttributeProcessor.OnCreated(IAttribute attribute, EntityData<IEntityData> entity) => OnCreated((T)attribute, entity);
        void IAttributeProcessor.OnDestroy(IAttribute attribute, EntityData<IEntityData> entity) => OnDestroy((T)attribute, entity);

        public override Type Target => TypeHelper.TypeOf<T>.Type;

        /// <inheritdoc cref="IAttributeProcessor.OnCreated(AttributeBase, EntityData{IEntityData})"/>
        protected virtual void OnCreated(T attribute, EntityData<IEntityData> entity) { }
        /// <inheritdoc cref="IAttributeProcessor.OnCreatedSync(AttributeBase, EntityData{IEntityData})"/>
        /// <inheritdoc cref="IAttributeProcessor.OnDestroy(AttributeBase, EntityData{IEntityData})"/>
        protected virtual void OnDestroy(T attribute, EntityData<IEntityData> entity) { }
        /// <inheritdoc cref="IAttributeProcessor.OnDestroySync(AttributeBase, EntityData{IEntityData})"/>
    }
}
