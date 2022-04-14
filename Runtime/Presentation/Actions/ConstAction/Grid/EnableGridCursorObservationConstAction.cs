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
using Syadeu.Presentation.Grid;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Syadeu.Presentation.Actions
{
    [DisplayName("Grid/Enable Cursor Observation")]
    [Description(
        "그리드 커서 좌표 감시 여부를 설정합니다.")]
    [Guid("CA91790D-3731-48D7-8555-5D4F1A24AD22")]
    internal sealed class EnableGridCursorObservationConstAction : ConstAction<int>
    {
        [JsonProperty(Order = 0, PropertyName = "Enable")]
        public bool m_Enable = false;

        [JsonIgnore]
        private WorldGridSystem m_GridSystem;

        protected override void OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, WorldGridSystem>(Bind);
        }
        protected override void OnDispose()
        {
            m_GridSystem = null;
        }

        private void Bind(WorldGridSystem other)
        {
            m_GridSystem = other;
        }

        protected override int Execute()
        {
            m_GridSystem.EnableCursorObserve(m_Enable);

            return 0;
        }
    }
}
