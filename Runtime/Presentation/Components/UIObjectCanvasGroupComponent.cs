using Syadeu.Collections;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Proxy;
using UnityEngine;

namespace Syadeu.Presentation.Entities
{
    public struct UIObjectCanvasGroupComponent : IEntityComponent
    {
        internal EntityID m_Parent;
        public bool m_Enabled;

        private float m_Alpha;

        public float Alpha
        {
            get => m_Alpha;
            set
            {
                m_Alpha = value;
                SetAlpha((ProxyTransform)m_Parent.GetEntity<IEntity>().transform);
            }
        }

        private void SetAlpha(ProxyTransform tr)
        {
            if (!tr.hasProxy) return;

            var cg = tr.proxy.GetComponent<CanvasGroup>();
            cg.alpha = m_Alpha;
        }
    }
}
