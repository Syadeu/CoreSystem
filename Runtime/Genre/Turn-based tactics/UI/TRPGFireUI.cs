using DG.Tweening;
using Syadeu.Presentation.Events;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Syadeu.Presentation.TurnTable.UI
{
    public sealed class TRPGFireUI : MonoBehaviour
    {
        [SerializeField] private Button m_Button;
        [SerializeField] private AnimationCurve m_OpenAnimationCurve;
        [SerializeField] private AnimationCurve m_CloseAnimationCurve;

        [SerializeField] private float m_OpenTime = .25f;
        [SerializeField] private float m_CloseTime = .25f;

        private RectTransform m_Transform;
        private Vector2 
            m_DefaultSizeDelta,
            m_ClosedSizeDelta;
        private bool m_Opened = false;

        private EventSystem m_EventSystem;

        public bool Opened => m_Opened;

        private void Awake()
        {
            m_Button.onClick.AddListener(Click);

            m_Transform = (RectTransform)transform;

            m_DefaultSizeDelta = m_Transform.sizeDelta;
            m_ClosedSizeDelta = new Vector2(0, m_DefaultSizeDelta.y);
        }
        private IEnumerator Start()
        {
            yield return PresentationSystem<DefaultPresentationGroup, EventSystem>.GetAwaiter();
            yield return PresentationSystem<TRPGIngameSystemGroup, TRPGCanvasUISystem>.GetAwaiter();

            m_EventSystem = PresentationSystem<DefaultPresentationGroup, EventSystem>.System;
            PresentationSystem<TRPGIngameSystemGroup, TRPGCanvasUISystem>.System.AuthoringFire(this);
            
            Open(false);
        }
        private void OnDestroy()
        {
            m_Transform = null;
            m_EventSystem = null;
        }

        private void Click()
        {
            if (!m_Opened) return;

            m_EventSystem.PostEvent(TRPGFireUIPressedEvent.GetEvent());
        }

        public void Open(bool open)
        {
            m_Transform.DOKill();

            if (open)
            {
                m_Transform
                    .DOSizeDelta(m_DefaultSizeDelta, m_OpenTime)
                    .SetEase(m_OpenAnimationCurve);
            }
            else
            {
                m_Transform
                    .DOSizeDelta(m_ClosedSizeDelta, m_CloseTime)
                    .SetEase(m_CloseAnimationCurve);
            }

            m_Opened = open;
        }
    }
}