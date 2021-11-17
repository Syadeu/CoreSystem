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

using Syadeu.Presentation.Internal;
using System;
using UnityEngine;

namespace Syadeu.Presentation
{
    [Obsolete]
    public sealed class WaitForPresentationSystem<T> : CustomYieldInstruction
        where T : PresentationSystemEntity
    {
        public override bool keepWaiting
        {
            get
            {
                if (PresentationSystem<T>.IsValid()) return false;
                return true;
            }
        }
    }
    public sealed class WaitForPresentationSystem<TGroup, TSystem> : CustomYieldInstruction
        where TGroup : PresentationGroupEntity
        where TSystem : PresentationSystemEntity
    {
        public override bool keepWaiting
        {
            get
            {
                if (PresentationSystem<TGroup, TSystem>.IsValid()) return false;
                return true;
            }
        }
    }
}
