// Copyright 2022 Seung Ha Kim
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

using GraphProcessor;

namespace Syadeu.Collections.Graphs
{
    /// <summary>
    /// 모든 <see cref="VisualGraph"/> 기본 프로세서입니다.
    /// </summary>
    /// <remarks>
    /// 
    /// </remarks>
    public abstract class VisualGraphProcessor : BaseGraphProcessor
    {
        protected VisualGraphProcessor(BaseGraph graph) : base(graph) { }

        internal void Initialize(object caller)
        {
            OnInitialize(caller);
        }
        internal void Reserve()
        {
            OnReserve();
        }

        /// <summary>
        /// 프로세서가 실행되기전 가장 처음으로 실행되는 함수입니다.
        /// </summary>
        /// <param name="caller"></param>
        protected virtual void OnInitialize(object caller) { }
        /// <summary>
        /// 프로세서가 모두 동작하고, 맨 마지막에 실행되는 함수입니다.
        /// </summary>
        protected virtual void OnReserve() { }
    }
}
