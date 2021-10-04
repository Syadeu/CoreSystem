using DG.Tweening;
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
            yield return PresentationSystem<TRPGSystemGroup, TRPGCanvasUISystem>.GetAwaiter();

            PresentationSystem<TRPGSystemGroup, TRPGCanvasUISystem>.System.AuthoringFire(this);
            //gameObject.SetActive(false);
            Open(false);
        }

        private void Click()
        {
            if (!m_Opened) return;
            
            //m_EventSystem.PostEvent(TRPGShortcutUIPressedEvent.GetEvent(this, m_ShortcutType));
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