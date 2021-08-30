using UnityEngine;

namespace Syadeu.Presentation
{
    public sealed class WaitForProxy : CustomYieldInstruction
    {
        private ProxyTransform m_Transform;

        public ProxyTransform Transform
        {
            get => m_Transform;
            set => m_Transform = value;
        }
        public override bool keepWaiting
        {
            get
            {
                if (m_Transform.hasProxy && !m_Transform.hasProxyQueued) return false;
                return true;
            }
        }

        public WaitForProxy(ProxyTransform tr)
        {
            m_Transform = tr;
        }
    }
}
