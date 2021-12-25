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
using Unity.Burst;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace Syadeu.Presentation
{
    public sealed class UtilitySystem : PresentationSystemEntity<UtilitySystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private Random m_Random;

        protected override PresentationResult OnInitialize()
        {
            m_Random = new Random();
            m_Random.InitState();

            return base.OnInitialize();
        }
        protected override void OnDispose()
        {
        }

        public int CreateHashCode() => m_Random.NextInt(int.MinValue, int.MaxValue);
    }
}
