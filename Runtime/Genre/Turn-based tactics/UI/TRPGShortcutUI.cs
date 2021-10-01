using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

namespace Syadeu.Presentation.TurnTable.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class TRPGShortcutUI : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private ShortcutType m_ShortcutType;
        [SerializeField] private InputAction InputAction;

        private Button m_Button;

        public ShortcutType ShortcutType => m_ShortcutType;

        private void Awake()
        {
            m_Button = GetComponent<Button>();
            m_Button.onClick.AddListener(Click);
        }
        private IEnumerator Start()
        {
            yield return PresentationSystem<TRPGSystemGroup, TRPGCanvasUISystem>.GetAwaiter();

            PresentationSystem<TRPGSystemGroup, TRPGCanvasUISystem>.System.AuthoringShortcut(this);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            //"pointer down".ToLog();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            //"pointer in".ToLog();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            //"pointer exit".ToLog();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            //"pointer up".ToLog();
        }
        private void Click()
        {
            "Click".ToLog();
        }

        public void OnKeyboardPressed(InputAction.CallbackContext obj)
        {
            Click();
        }
    }
}