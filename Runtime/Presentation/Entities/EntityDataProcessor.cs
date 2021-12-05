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
using Syadeu.Presentation.Internal;
using System;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Entities
{
    /// <summary>
    /// <see cref="EntityDataBase"/>의 동작부를 선언할 수 있습니다.
    /// </summary>
    [Preserve, Obsolete("Use EntityProcessor")]
    public abstract class EntityDataProcessor : ProcessorBase, IEntityDataProcessor
    {
        internal override void InternalOnCreated(ObjectBase obj)
        {
            throw new NotImplementedException();
        }
        internal override void InternalOnDestroy(ObjectBase obj)
        {
            throw new NotImplementedException();
        }

        void IEntityDataProcessor.OnCreated(IEntityData entity) => OnCreated(entity);
        void IEntityDataProcessor.OnDestroy(IEntityData entity) => OnDestory(entity);

        /// <summary>
        /// <see cref="Target"/>에 설정된 <see cref="EntityDataBase"/>가
        /// 생성되었을 때 실행되는 메소드입니다.
        /// </summary>
        /// <remarks>
        /// 비동기 작업입니다. Unity API에 접근하면 에러를 뱉습니다.
        /// </remarks>
        /// <param name="entity"></param>
        protected virtual void OnCreated(IEntityData entity) { }
        /// <summary>
        /// <see cref="Target"/>에 설정된 <see cref="EntityDataBase"/>가
        /// 파괴되었을 때 실행되는 메소드입니다.
        /// </summary>
        /// <param name="entity"></param>
        /// <remarks>
        /// 비동기 작업입니다. Unity API에 접근하면 에러를 뱉습니다.
        /// </remarks>
        protected virtual void OnDestory(IEntityData entity) { }
    }
    /// <inheritdoc cref="EntityDataProcessor"/>
    /// <typeparam name="T"></typeparam>
    [Preserve, Obsolete("Use EntityProcessor")]
    public abstract class EntityDataProcessor<T> : ProcessorBase, IEntityDataProcessor where T : EntityDataBase
    {
        internal override void InternalOnCreated(ObjectBase obj)
        {
            throw new NotImplementedException();
        }
        internal override void InternalOnDestroy(ObjectBase obj)
        {
            throw new NotImplementedException();
        }

        void IEntityDataProcessor.OnCreated(IEntityData entity) => OnCreated((T)entity);
        void IEntityDataProcessor.OnDestroy(IEntityData entity) => OnDestroy((T)entity);
        void IDisposable.Dispose() => OnDispose();

        public override Type Target => TypeHelper.TypeOf<T>.Type;

        /// <inheritdoc cref="EntityDataProcessor.OnCreated(EntityData{IEntityData})"/>
        protected virtual void OnCreated(T entity) { }
        /// <inheritdoc cref="EntityDataProcessor.OnDestory(EntityData{IEntityData})"/>
        protected virtual void OnDestroy(T entity) { }
    }
}
