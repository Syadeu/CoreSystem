using Syadeu.Mono;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Proxy;
using TMPro;
using UnityEngine;

namespace Syadeu.Presentation.TurnTable
{
    /// <summary>
    /// <seealso cref="TRPGActionPointUIAttribute"/>
    /// </summary>
    public sealed class TRPGActionPointOverlayUI : RecycleableMonobehaviour
    {
        [SerializeField] private CanvasGroup m_CanvasGroup;

        [Space]
        [SerializeField] private SimpleSwitch m_APSwitch;
        [SerializeField] private SimpleSwitch m_HPSwitch;

        [Space]
        [SerializeField] private TextMeshProUGUI[] m_FullApText;
        [SerializeField] private TextMeshProUGUI m_ApText;

        [Space]
        [SerializeField] private TextMeshProUGUI[] m_FullHpText;
        [SerializeField] private TextMeshProUGUI m_HpText;

        private int m_FullHP = 0;
        private int m_FullAP = 0;
        private bool m_IsFullAP = true;
        private bool m_IsFullHP = true;

        public void SetHPFullText(int fullHp)
        {
            m_FullHP = fullHp;
            for (int i = 0; i < m_FullHpText.Length; i++)
            {
                m_FullHpText[i].text = $"{fullHp}";
            }
        }
        public void SetAPFullText(int fullAp)
        {
            m_FullAP = fullAp;
            for (int i = 0; i < m_FullApText.Length; i++)
            {
                m_FullApText[i].text = $"{fullAp}";
            }
        }
        public void SetHPText(int hp)
        {
            if (hp.Equals(m_FullHP))
            {
                if (!m_IsFullHP)
                {
                    m_HPSwitch.On(0);
                    m_IsFullHP = true;
                }
            }
            else
            {
                if (m_IsFullHP)
                {
                    m_HPSwitch.On(1);
                    m_IsFullHP = false;
                }
                m_HpText.text = $"{hp}";
            }
        }
        public void SetAPText(int ap)
        {
            if (ap.Equals(m_FullAP))
            {
                if (!m_IsFullAP)
                {
                    m_APSwitch.On(0);

                    m_IsFullAP = true;
                }
            }
            else
            {
                if (m_IsFullAP)
                {
                    m_APSwitch.On(1);

                    m_IsFullAP = false;
                }

                m_ApText.text = $"{ap}";
            }
        }

        protected override void OnCreated()
        {
            if (m_CanvasGroup == null) m_CanvasGroup = GetComponent<CanvasGroup>();

            m_CanvasGroup.alpha = 0;
            m_CanvasGroup.blocksRaycasts = false;
        }
        protected override void OnInitialize()
        {
            //m_CanvasGroup.DOKill();
            //m_CanvasGroup.alpha = 0;
            m_CanvasGroup.alpha = 1;
            //m_CanvasGroup.DOFade(1, .25f);
            m_CanvasGroup.blocksRaycasts = true;
        }
        protected override void OnTerminate()
        {
            //m_CanvasGroup.DOKill();
            //m_CanvasGroup.DOFade(0, .25f);
            m_CanvasGroup.alpha = 0;
            m_CanvasGroup.blocksRaycasts = false;
        }
    }
}