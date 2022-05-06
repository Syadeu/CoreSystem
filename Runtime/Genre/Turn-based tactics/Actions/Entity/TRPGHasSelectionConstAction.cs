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

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Presentation.Actions;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Syadeu.Presentation.TurnTable
{
    [DisplayName("Entity/TRPG/Has Selection")]
    [Guid("2AD1F9F6-2FE1-47DF-88C6-BFBDBE6553B4")]
    internal sealed class TRPGHasSelectionConstAction : ConstAction<bool>
    {
        private TRPGSelectionSystem m_SelectionSystem;

        protected override void OnInitialize()
        {
            RequestSystem<TRPGIngameSystemGroup, TRPGSelectionSystem>(Bind);
        }
        protected override void OnDispose()
        {
            m_SelectionSystem = null;
        }
        private void Bind(TRPGSelectionSystem other) 
        {
            m_SelectionSystem = other;
        }

        protected override bool Execute()
        {
            return !m_SelectionSystem.CurrentSelection.IsEmpty();
        }
    }
}