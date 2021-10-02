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
        internal void Initialize(TRPGTurnTableSystem turnTableSystem)
        {
            m_TurnTableSystem = turnTableSystem;
        }

        internal void Click()
        {
            "Click endturn".ToLog();

            m_TurnTableSystem.NextTurn();
        }
        internal void OnKeyboardPressed(InputAction.CallbackContext obj)
        {
            Click();
        }
    }
}