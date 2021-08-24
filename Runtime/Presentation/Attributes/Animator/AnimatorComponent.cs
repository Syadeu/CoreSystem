using Syadeu.Database;
using Syadeu.Presentation.Entities;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Presentation.Attributes
{
    [RequireComponent(typeof(Animator))]
    public sealed class AnimatorComponent : MonoBehaviour
    {
        internal AnimatorAttribute m_AnimatorAttribute;
        internal Animator m_Animator;

        private void Awake()
        {
            m_Animator = GetComponent<Animator>();
        }

        public void SetActive(bool enable)
        {
            m_Animator.enabled = enable;
        }
        public void TriggerAction(AnimationEvent evt)
        {
            if (m_AnimatorAttribute == null) return;

            Hash hash = Hash.NewHash(evt.stringParameter);
            if (!m_AnimatorAttribute.Actions.TryGetValue(hash, out List<Reference<AnimationTriggerAction>> actions))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot found {evt.stringParameter} {nameof(AnimationTriggerAction)} at {m_AnimatorAttribute.Parent.Name}");
                return;
            }

            for (int i = 0; i < actions.Count; i++)
            {
                actions[i].Execute(m_AnimatorAttribute.Parent);
            }
        }
    }
}
