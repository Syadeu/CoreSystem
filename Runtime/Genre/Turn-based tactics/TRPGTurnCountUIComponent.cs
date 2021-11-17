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
using Syadeu.Presentation.Entities;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Syadeu.Presentation.TurnTable
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class TRPGTurnCountUIComponent : MonoBehaviour
    {
        [SerializeField] private string m_TextFormat = "TURN {0}\nPLACE";

        private TextMeshProUGUI m_Text;
        private TRPGTurnTableSystem m_TurnTableSystem;

        private IEnumerator Start()
        {
            m_Text = GetComponent<TextMeshProUGUI>();

            yield return PresentationSystem<TRPGIngameSystemGroup, TRPGTurnTableSystem>.GetAwaiter();

            m_TurnTableSystem = PresentationSystem<TRPGIngameSystemGroup, TRPGTurnTableSystem>.System;
            m_TurnTableSystem.OnStartTurn += System_OnStartTurn;
        }

        private void System_OnStartTurn(EntityData<IEntityData> obj)
        {
            m_Text.text = string.Format(m_TextFormat, m_TurnTableSystem.TurnCount);
        }
    }
}