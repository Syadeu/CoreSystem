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

namespace Syadeu.Presentation.Actions
{
    public abstract class StateBase<TAction> : ITerminate
        where TAction : ActionBase
    {
        public enum State
        {
            Wait            =   0,
            AboutToExecute  =   1,
            Executing       =   2,

            Success         =   3,
            Failure         =   4
        }

        internal TAction Action { get; set; }
        public Entity<IEntityData> Entity { get; internal set; }
        public State CurrentState { get; internal set; } = 0;

        protected abstract void OnTerminate();

        void ITerminate.Terminate()
        {
            OnTerminate();

            Entity = Entity<IEntityData>.Empty;
            CurrentState = 0;
        }
    }
}
