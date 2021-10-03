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
            yield return PresentationSystem<TRPGSystemGroup, TRPGCanvasUISystem>.GetAwaiter();

            PresentationSystem<TRPGSystemGroup, TRPGCanvasUISystem>.System.AuthoringEndTurn(this);
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