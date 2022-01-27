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
using System.Runtime.InteropServices;

namespace Syadeu.Presentation.Actions
{
    /// <summary>
    /// Code-level 에서 미리 정의된 런타임 값을 반환하는 액션입니다.
    /// </summary>
    /// <remarks>
    /// 모든 ConstAction 은 <see cref="GuidAttribute"/> 를 가지고 있어야 합니다.
    /// </remarks>
    /// <typeparam name="TValue"></typeparam>
    public abstract class ConstAction<TValue> : IConstAction
    {
        Type IConstAction.ReturnType => TypeHelper.TypeOf<TValue>.Type;
        object IConstAction.Execute() => Execute();

        public ConstAction()
        {
        }

        protected abstract TValue Execute();
    }
    public interface IConstAction
    {
        Type ReturnType { get; }
        object Execute();
    }

    [Guid("0C11829E-730C-4082-B6E5-2ED487607F2E")]
    public sealed class TestConstAction : ConstAction<int>
    {
        protected override int Execute()
        {
            "test const action".ToLog();
            return 1;
        }
    }
}
