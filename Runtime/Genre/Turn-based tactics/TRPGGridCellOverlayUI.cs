#if CORESYSTEM_DOTWEEN
using DG.Tweening;
#endif

using Syadeu;
using Syadeu.Database;
using Syadeu.Presentation.Input;
using Syadeu.Presentation.Proxy;

using System;
using System.Collections;

using TMPro;

using Unity.Mathematics;

using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

namespace Syadeu.Presentation.TurnTable
{
    public sealed class TRPGGridCellOverlayUI : OnScreenControl, ITerminate, IValidation,
    IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public enum State
        {
            Normal,
            Highlighted,
            Selected,
            Disabled
        }

        [SerializeField] private ManagedRecycleObject m_RecycleComponent;
        [SerializeField] private CanvasGroup m_CanvasGroup;

        [Space]
        [SerializeField] private Image m_Outline;
        [SerializeField] private Image m_BackgroundImg;
        [SerializeField] private Image m_SelectedImg;
        [SerializeField] private Image m_DetectionImg;

        [Space]
        [SerializeField] private Color m_NormalColor;
        [SerializeField] private Color m_HighlightedColor;
        [SerializeField] private Color m_SelectedColor;
        [SerializeField] private Color m_DisabledColor;

        [Space]
        [InputControl(layout = "Button")]
        [SerializeField] private string m_ControlPath;

        [Space]
        [SerializeField] private bool m_IsDetectionTile = false;
        private bool m_Initialized = false;

        protected override string controlPathInternal
        {
            get => m_ControlPath;
            set => m_ControlPath = value;
        }

        public Color Color => m_BackgroundImg.color;

        public int GridIndex { get; private set; }
        public int RequireAP { get; private set; }

        public State CurrentState { get; private set; } = State.Normal;
        public bool IsValid() => m_Initialized && m_RecycleComponent.IsValid();

        private TextMeshProUGUI testAP;
        public void Initialize(int gridIndex, int requireAP)
        {
            GridIndex = gridIndex;
            RequireAP = requireAP;

            if (testAP == null)
            {
                GameObject obj = new GameObject("test");
                testAP = obj.AddComponent<TextMeshProUGUI>();
                obj.transform.SetParent(transform);

                Transform tr = testAP.transform;

                tr.localScale = new Vector3(.02f, .02f, .02f);
                tr.rotation = Quaternion.identity;
                tr.localPosition = new Vector3(0, 0, -.2f);

                testAP.alignment = TextAlignmentOptions.Center;
            }
            testAP.text = $"{gridIndex}:{requireAP}";

            m_Initialized = true;
        }
        private void SetState(State _state)
        {
            if (!IsValid()) return;

            SetStateColor(_state);

            CurrentState = _state;
        }
        private void SetStateColor(State _state)
        {
            Color color;
            if (_state == State.Selected)
            {
                color = m_SelectedColor;
            }
            else if (_state == State.Highlighted)
            {
                color = m_HighlightedColor;
            }
            else if (_state == State.Normal)
            {
                color = m_NormalColor;
            }
            else color = m_DisabledColor;

#if CORESYSTEM_DOTWEEN
            m_BackgroundImg.DOKill();
            m_BackgroundImg.DOColor(color, .25f);
#endif
        }

        public void OnPointerUp(PointerEventData data)
        {
            if (!IsValid()) return;

            if (!PresentationSystem<InputSystem>.System.EnableInput ||
                data.button != PointerEventData.InputButton.Left) return;

            DeSelect();
        }
        public void OnPointerDown(PointerEventData data)
        {
            if (!IsValid()) return;
        }
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!IsValid()) return;

            SetState(State.Highlighted);
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            if (!IsValid()) return;

            SetState(State.Normal);
        }

        public void Select()
        {
            if (!IsValid()) throw new Exception();

            SetState(State.Selected);
        }
        public void DeSelect()
        {
            if (!IsValid()) throw new Exception();

            SetState(State.Normal);
        }

        #region Recycleable Monobehaviour
        public void OnCreated()
        {
            m_CanvasGroup.alpha = 0;
            m_CanvasGroup.blocksRaycasts = false;

            if (m_RecycleComponent == null) m_RecycleComponent = GetComponent<ManagedRecycleObject>();

            SetStateColor(State.Normal);
        }
        public void OnInitialize()
        {
            if (m_Initialized) throw new Exception();

#if CORESYSTEM_DOTWEEN
            m_CanvasGroup.DOKill(true);
            m_CanvasGroup.alpha = 0;
            m_CanvasGroup.DOFade(1, .25f);
#endif

            m_CanvasGroup.blocksRaycasts = true;
        }
        public void Terminate()
        {
#if CORESYSTEM_DOTWEEN
            m_CanvasGroup.DOKill(false);
            m_CanvasGroup.DOFade(0, .25f);
#endif
            m_CanvasGroup.blocksRaycasts = false;

            SetStateColor(State.Normal);

            m_Initialized = false;
        }
        #endregion
    }
}