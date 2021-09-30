﻿using Syadeu.Database;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Proxy;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Playables;

namespace Syadeu.Presentation.Attributes
{
    [RequireComponent(typeof(Animator))]
    public sealed class AnimatorComponent : MonoBehaviour
    {
        [SerializeField] private bool m_EnableRootMotion = true;

        [NonSerialized] internal ITransform m_Transform;
        [NonSerialized] internal AnimatorAttribute m_AnimatorAttribute;
        [NonSerialized] internal Animator m_Animator;

        public Animator Animator => m_Animator;

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
            if (!m_AnimatorAttribute.AnimationTriggers.TryGetValue(hash, out List<Reference<AnimationTriggerAction>> actions))
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
        private void OnAnimatorMove()
        {
            if (m_AnimatorAttribute == null) return;

            if (m_EnableRootMotion)
            {
                m_Transform.position += (float3)m_Animator.deltaPosition;
                m_Transform.rotation *= m_Animator.deltaRotation;
            }
        }
    }
}
