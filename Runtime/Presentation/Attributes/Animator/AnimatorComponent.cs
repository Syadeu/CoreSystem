using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

namespace Syadeu.Presentation.Attributes
{
    [RequireComponent(typeof(Animator))]
    public sealed class AnimatorComponent : MonoBehaviour
    {
        private Animator m_Animator;

        private void Awake()
        {
            m_Animator = GetComponent<Animator>();
        }

        public void SetActive(bool enable)
        {
            m_Animator.enabled = enable;
        }
        public void TriggerEvent(AnimationEvent evt)
        {
            $"{evt.stringParameter} triggered".ToLog();
        }
    }
}
