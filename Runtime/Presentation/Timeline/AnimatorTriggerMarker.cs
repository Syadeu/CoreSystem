using Syadeu.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Syadeu.Presentation.Timeline
{
    public sealed class AnimatorTriggerMarker : Marker, INotification
    {
        [SerializeField] private string m_TriggerString = string.Empty;

        private PropertyName m_ID = default(PropertyName);
        public PropertyName id
        {
            get
            {
                if (PropertyName.IsNullOrEmpty(m_ID))
                {
                    m_ID = new PropertyName(GetHashCode());
                }
                return m_ID;
            }
        }

        private int m_TriggerKey = 0;
        public int TriggerKey
        {
            get
            {
                if (!string.IsNullOrEmpty(m_TriggerString) && m_TriggerKey == 0)
                {
                    m_TriggerKey = Animator.StringToHash(m_TriggerString);
                }
                return m_TriggerKey;
            }
        }
    }
}
