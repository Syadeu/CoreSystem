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

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using System;

namespace Syadeu.Presentation
{
    /// <summary>
    /// <see cref="ObjectBase"/>의 동작부를 선언할 수 있습니다.
    /// </summary>
    public abstract class EntityProcessor : ProcessorBase
    {
        internal override void InternalOnCreated(IObject obj) => OnCreated(obj);
        internal override void InternalOnDestroy(IObject obj) => OnDestroy(obj);

        protected virtual void OnCreated(IObject obj) { }
        protected virtual void OnDestroy(IObject obj) { }
    }
    /// <summary>
    /// <typeparamref name="TEntity"/>의 동작부를 선언할 수 있습니다.
    /// </summary>
    public abstract class EntityProcessor<TEntity> : ProcessorBase
        where TEntity : class, IObject
    {
        public override sealed Type Target => TypeHelper.TypeOf<TEntity>.Type;

        internal override void InternalOnCreated(IObject obj) => OnCreated((TEntity)obj);
        internal override void InternalOnDestroy(IObject obj) => OnDestroy((TEntity)obj);

        protected virtual void OnCreated(TEntity obj) { }
        protected virtual void OnDestroy(TEntity obj) { }
    }
}
