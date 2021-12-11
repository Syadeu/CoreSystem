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

namespace Syadeu.Presentation.Actor
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// AOT 방지를 위해 <see cref="ActorSystem.AOTCodeGenerator{TEvent}"/> 를 사용하세요.
    /// </remarks>
    [UnityEngine.Scripting.RequireImplementors]
    public interface IActorEvent
    {
        void OnExecute(Entity<ActorEntity> from);
    }
}
