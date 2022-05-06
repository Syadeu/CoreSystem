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

using Syadeu.Presentation.Events;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Syadeu.Presentation.TurnTable.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class TRPGEndTurnUI : MonoBehaviour
    {
        private Button m_Button;
        private TRPGTurnTableSystem m_TurnTableSystem;
        private EventSystem m_EventSystem;

        private bool m_IsHide = false;

        public bool Hide
        {
            get => m_IsHide;
            set
            {
                m_IsHide = value;
                gameObject.SetActive(!m_IsHide);
            }
        }

        private void Awake()
        {
            m_Button = GetComponent<Button>();
            m_Button.onClick.AddListener(Click);
        }
        private IEnumerator Start()
        {
            yield return PresentationSystem<TRPGIngameSystemGroup, TRPGCanvasUISystem>.GetAwaiter();

            PresentationSystem<TRPGIngameSystemGroup, TRPGCanvasUISystem>.System.AuthoringEndTurn(this);
        }
        private void OnDestroy()
        {
            m_TurnTableSystem = null;
            m_EventSystem = null;
        }

        internal void Initialize(TRPGTurnTableSystem turnTableSystem, EventSystem eventSystem)
        {
            m_TurnTableSystem = turnTableSystem;
            m_EventSystem = eventSystem;
        }

        internal void Click()
        {
            if (m_IsHide) return;

            m_EventSystem.PostEvent(TRPGEndTurnUIPressedEvent.GetEvent());
        }
        internal void OnKeyboardPressed(InputAction.CallbackContext obj)
        {
            Click();
        }
    }
}