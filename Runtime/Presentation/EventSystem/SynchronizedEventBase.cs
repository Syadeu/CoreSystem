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
using System;
using System.Collections;

namespace Syadeu.Presentation
{
    /// <summary>
    /// 직접 상속은 허용하지 않습니다. <seealso cref="SynchronizedEvent{TEvent}"/>를 상속하여 사용하세요.
    /// </summary>
    public abstract class SynchronizedEventBase : IValidation
    {
        internal abstract Hash EventHash { get; }
        internal string InternalName => Name;
        internal Type InternalEventType => EventType;
        internal bool InternalDisplayLog => DisplayLog;
        internal UpdateLoop InternalLoop => Loop;

        protected abstract string Name { get; }
        protected abstract Type EventType { get; }
        protected virtual bool DisplayLog => true;

        protected virtual UpdateLoop Loop => UpdateLoop.Default;

        //internal abstract void InternalPost();
        internal abstract void InternalTerminate();

        public virtual bool IsValid() => true;
    }
}
