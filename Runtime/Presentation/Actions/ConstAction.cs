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

using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Collections.Buffer;
using System;
using System.Buffers;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace Syadeu.Presentation.Actions
{
    /// <summary>
    /// Code-level 에서 미리 정의된 런타임 값을 반환하는 액션입니다.
    /// </summary>
    /// <remarks>
    /// 모든 ConstAction 은 <see cref="GuidAttribute"/> 를 가지고 있어야 합니다. 
    /// 정의된 <see cref="ConstAction{TValue}"/> 는 <seealso cref="ConstActionReference{TValue}"/> 를 통해 
    /// 레퍼런스 될 수 있습니다.
    /// </remarks>
    /// <typeparam name="TValue">
    /// 의미있는 값을 반환하지 않는다면 int 를 사용하세요
    /// </typeparam>
    [Serializable]
    public abstract class ConstAction<TValue> : IConstAction
    {
        Type IConstAction.ReturnType => TypeHelper.TypeOf<TValue>.Type;

        public ConstAction()
        {
        }

        ConstActionUtilities.Info IConstAction.GetInfo() => ConstActionUtilities.HashMap[GetType()];
        void IConstAction.SetArguments(params object[] args) => InternalSetArguments(args);

        void IConstAction.Initialize() => OnInitialize();
        object IConstAction.Execute() => InternalExecute();
        void IConstAction.OnShutdown() => OnShutdown();
        void IDisposable.Dispose() => OnDispose();

        protected virtual void InternalSetArguments(params object[] args)
        {
            ConstActionUtilities.HashMap[GetType()].SetArguments(this, args);
        }
        protected virtual TValue InternalExecute() => Execute();

        protected virtual void OnInitialize() { }
        protected abstract TValue Execute();

        protected virtual void OnShutdown() { }
        protected virtual void OnDispose() { }

        #region Utils

        /// <summary>
        /// <inheritdoc cref="PresentationManager.RegisterRequest{TGroup, TSystem}(Action{TSystem}, string)"/>
        /// </summary>
        /// <remarks>
        /// <seealso cref="OnInitialize"/> 혹은 <seealso cref="OnInitializeAsync"/> 에서만 수행되어야합니다.
        /// </remarks>
        /// <typeparam name="TGroup"></typeparam>
        /// <typeparam name="TSystem"></typeparam>
        /// <param name="setter"></param>
        protected void RequestSystem<TGroup, TSystem>(Action<TSystem> setter
#if DEBUG_MODE
            , [System.Runtime.CompilerServices.CallerFilePath] string methodName = ""
#endif
            )
            where TGroup : PresentationGroupEntity
            where TSystem : Internal.PresentationSystemEntity
        {
            PresentationManager.RegisterRequest<TGroup, TSystem>(setter
#if DEBUG_MODE
                , methodName
#endif
                );
        }

        #endregion
    }
    public abstract class ConstTriggerAction<TValue> : ConstAction<TValue>, IConstTriggerAction
    {
        [JsonIgnore] private InstanceID m_Entity;

        protected override sealed void InternalSetArguments(params object[] args)
        {
            object[] arr = ArrayPool<object>.Shared.Rent(args.Length - 1);
            Array.Copy(args, 1, arr, 0, args.Length - 1);

            m_Entity = (InstanceID)args[0];

            ConstActionUtilities.HashMap[GetType()].SetArguments(this, arr);

            ArrayPool<object>.Shared.Return(arr);
        }
        protected override TValue InternalExecute() => Execute(m_Entity);

        protected abstract TValue Execute(InstanceID entity);
        protected override sealed TValue Execute()
        {
            throw new NotImplementedException();
        }
    }
}
