// Copyright 2021 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Syadeu.Collections;
using Syadeu.Collections.Proxy;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Proxy;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Playables;

namespace Syadeu.Presentation.Attributes
{
    [RequireComponent(typeof(Animator))]
    public sealed class AnimatorComponent : MonoBehaviour, IProxyComponent
    {
        [SerializeField] private bool m_EnableRootMotion = true;
        [SerializeField] private bool m_ManualRootMotionUpdate = false;

        [NonSerialized] internal ITransform m_Transform;
        [NonSerialized] internal AnimatorAttribute m_AnimatorAttribute;
        [NonSerialized] internal Animator m_Animator;

        public Animator Animator => m_Animator;
        public bool RootMotion => m_EnableRootMotion;
        public bool ManualRootMotionUpdate
        {
            get => m_ManualRootMotionUpdate;
            set => m_ManualRootMotionUpdate = value;
        }

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
                CoreSystem.Logger.LogError(LogChannel.Entity,
                    $"Cannot found {evt.stringParameter} {nameof(AnimationTriggerAction)} at {m_AnimatorAttribute.ParentEntity.Name}");
                return;
            }

            for (int i = 0; i < actions.Count; i++)
            {
                actions[i].Execute(m_AnimatorAttribute.Parent);
            }
        }
        private void OnAnimatorMove()
        {
            if (m_AnimatorAttribute == null || !m_EnableRootMotion || m_ManualRootMotionUpdate) return;

            m_Transform.position += (float3)m_Animator.deltaPosition;
            m_Transform.rotation *= m_Animator.deltaRotation;
        }

        public void OnProxyCreated(RecycleableMonobehaviour obj)
        {
            obj.OnVisible += Obj_OnVisible;
            obj.OnInvisible += Obj_OnInvisible;
        }
        private void Obj_OnVisible(Entity<IEntity> obj)
        {
            m_Animator.enabled = true;
        }
        private void Obj_OnInvisible(Entity<IEntity> obj)
        {
            m_Animator.enabled = false;
        }
    }
}
