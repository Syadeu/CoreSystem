using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

using EventSystem = Syadeu.Presentation.Events.EventSystem;

namespace Syadeu.Presentation.TurnTable.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class TRPGShortcutUI : PresentationBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private ShortcutType m_ShortcutType;

        [SerializeField] private TextMeshProUGUI m_ShortcutIndexText;

        private bool m_IsHide = false;
        private bool m_Enabled = true;

        private Button m_Button;
        private TRPGCanvasUISystem m_CanvasUISystem;
        private Events.EventSystem m_EventSystem;
        private int m_Index;

        public ShortcutType ShortcutType => m_ShortcutType;
        public bool Hide 
        { 
            get => m_IsHide;
            set
            {
                m_IsHide = value;
                gameObject.SetActive(!m_IsHide);
            }
        }
        public bool Enable
        {
            get => m_Enabled;
            set
            {
                m_Enabled = value;
                m_Button.interactable = value;
            }
        }
        public int Index => m_Index + 1;

        private void Awake()
        {
            m_Button = GetComponent<Button>();
            m_Button.onClick.AddListener(Click);

            Transform parent = transform.parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                if (parent.GetChild(i).Equals(transform))
                {
                    m_Index = i;
                    break;
                }
            }

            m_ShortcutIndexText.text = $"{m_Index + 1}";

            RequestSystem<DefaultPresentationGroup, EventSystem>(Bind);
            RequestSystem<TRPGIngameSystemGroup, TRPGCanvasUISystem>(Bind);
        }
        private void Bind(EventSystem other)
        {
            m_EventSystem = other;
        }
        private void Bind(TRPGCanvasUISystem other)
        {
            m_CanvasUISystem = other;

            //m_CanvasUISystem.AuthoringShortcut(this, m_ShortcutType);
        }

        private IEnumerator Start()
        {
            //yield return PresentationSystem<TRPGIngameSystemGroup, TRPGCanvasUISystem>.GetAwaiter();
            yield return new WaitUntil(() => m_CanvasUISystem != null);

            m_CanvasUISystem.AuthoringShortcut(this, m_ShortcutType);

            //PresentationSystem<TRPGIngameSystemGroup, TRPGCanvasUISystem>
            //    .System
            //    .AuthoringShortcut(this, m_ShortcutType);
        }
        //internal void Initialize(TRPGCanvasUISystem uiSystem, Events.EventSystem eventSystem)
        //{
        //    m_CanvasUISystem = uiSystem;
        //    m_EventSystem = eventSystem;
        //}
        private void OnDestroy()
        {
            // TODO : Temp code
            //var system = PresentationSystem<TRPGIngameSystemGroup, TRPGCanvasUISystem>
            //    .System;
            //if (system == null) return;

            m_CanvasUISystem.RemoveShortcut(this, m_ShortcutType);

            m_CanvasUISystem = null;
            m_EventSystem = null;
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
            if (!m_Enabled || m_IsHide) return;

            m_EventSystem.PostEvent(TRPGShortcutUIPressedEvent.GetEvent(this, m_ShortcutType));
        }

        internal void OnKeyboardPressed(InputAction.CallbackContext obj)
        {
            Click();
        }
    }
}