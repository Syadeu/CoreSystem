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

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Presentation.Entities;
using System;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    /// <summary>
    /// Use <see cref="FixedLogicTriggerAction"/> in component can be gather <see cref="GetFixedLogicTriggerAction"/>
    /// </summary>
    [Serializable]
    public sealed class LogicTriggerAction
    {
        [SerializeField, JsonProperty(Order = 0, PropertyName = "Name")] private string m_Name = string.Empty;

        [Space]
        [SerializeField, JsonProperty(Order = 1, PropertyName = "If")]
        private ArrayWrapper<Reference<TriggerPredicateAction>> m_If = Array.Empty<Reference<TriggerPredicateAction>>();
        [SerializeField, JsonProperty(Order = 2, PropertyName = "If Target")]
        private ArrayWrapper<Reference<TriggerPredicateAction>> m_IfTarget = Array.Empty<Reference<TriggerPredicateAction>>();

        [Space]
        [SerializeField, JsonProperty(Order = 3, PropertyName = "Do")]
        private ArrayWrapper<Reference<TriggerAction>> m_Do = Array.Empty<Reference<TriggerAction>>();
        [SerializeField, JsonProperty(Order = 4, PropertyName = "Do Target")]
        private ArrayWrapper<Reference<TriggerAction>> m_DoTarget = Array.Empty<Reference<TriggerAction>>();

        [JsonIgnore] public string Name => m_Name;

        public FixedLogicTriggerAction GetFixedLogicTriggerAction()
        {
            return new FixedLogicTriggerAction(
                m_Name,
                m_If,
                m_IfTarget,
                m_Do,
                m_DoTarget
                );
        }

        private bool IsExecutable()
        {
            if (m_If.Length == 0 && m_IfTarget.Length == 0) return false;
            return true;
        }
        [Obsolete("Need to deprecate this. Use FixedLogicTriggerAction Instead")]
        public bool Predicate(Entity<IObject> entity, Entity<IObject> target)
        {
            if (!IsExecutable()) return true;

            if ((m_If.Execute(entity, out bool thisPredicate) && thisPredicate) &&
               (m_IfTarget.Execute(target, out bool targetPredicate) && targetPredicate))
            {
                return true;
            }

            return false;
        }
        [Obsolete("Need to deprecate this. Use FixedLogicTriggerAction Instead")]
        public bool Execute(Entity<IObject> entity, Entity<IObject> target)
        {
            if (!IsExecutable())
            {
                return m_Do.Execute(entity) && m_DoTarget.Execute(target);
            }

            if ((m_If.Execute(entity, out bool thisPredicate) && thisPredicate) &&
               (m_IfTarget.Execute(target, out bool targetPredicate) && targetPredicate))
            {
                return m_Do.Execute(entity) && m_DoTarget.Execute(target);
            }

            return false;
        }
        [Obsolete("Need to deprecate this. Use FixedLogicTriggerAction Instead")]
        public bool Schedule(Entity<IObject> entity, Entity<IObject> target)
        {
            if (!IsExecutable())
            {
                m_Do.Schedule(entity); m_DoTarget.Schedule(target);
                return true;
            }

            if ((m_If.Execute(entity, out bool thisPredicate) && thisPredicate) &&
               (m_IfTarget.Execute(target, out bool targetPredicate) && targetPredicate))
            {
                m_Do.Schedule(entity); m_DoTarget.Schedule(target);
                return true;
            }

            return false;
        }
    }
}
