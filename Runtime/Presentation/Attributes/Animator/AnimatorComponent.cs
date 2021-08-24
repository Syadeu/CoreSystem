using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Presentation.Entities;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

namespace Syadeu.Presentation.Attributes
{
    [RequireComponent(typeof(Animator))]
    public sealed class AnimatorComponent : MonoBehaviour
    {
        internal AnimatorAttribute m_AnimatorAttribute;
        private Animator m_Animator;

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

    public sealed class AnimationTriggerAction : ActionBase<AnimationTriggerAction>
    {
        [JsonProperty(Order = 0, PropertyName = "TriggerName")] public string m_TriggerName;
        [JsonProperty(Order = 1, PropertyName = "TriggerActions")]
        public Reference<ActionBase>[] m_TriggerActions = Array.Empty<Reference<ActionBase>>();

        protected override void OnExecute(EntityData<IEntityData> entity)
        {
            for (int i = 0; i < m_TriggerActions.Length; i++)
            {
                m_TriggerActions[i].Execute(entity);
            }
        }
    }
}
