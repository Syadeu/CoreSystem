using Newtonsoft.Json;
using Syadeu.Presentation.Entities;
using System;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    [Serializable]
    public sealed class LogicTriggerAction
    {
        [JsonProperty(Order = 0, PropertyName = "Name")] private string m_Name = string.Empty;

        [JsonProperty(Order = 1, PropertyName = "If")]
        private Reference<TriggerPredicateAction>[] m_If = Array.Empty<Reference<TriggerPredicateAction>>();
        [JsonProperty(Order = 2, PropertyName = "If Target")]
        private Reference<TriggerPredicateAction>[] m_IfTarget = Array.Empty<Reference<TriggerPredicateAction>>();

        [Space]
        [JsonProperty(Order = 3, PropertyName = "Else If")]
        private LogicTriggerAction[] m_ElseIf = Array.Empty<LogicTriggerAction>();

        [Space]
        [JsonProperty(Order = 4, PropertyName = "Do")]
        private Reference<TriggerAction>[] m_Do = Array.Empty<Reference<TriggerAction>>();
        [JsonProperty(Order = 5, PropertyName = "Do Target")]
        private Reference<TriggerAction>[] m_DoTarget = Array.Empty<Reference<TriggerAction>>();

        [JsonIgnore] public string Name => m_Name;

        private bool IsExecutable()
        {
            if (m_If.Length == 0 && m_IfTarget.Length == 0) return false;
            return true;
        }
        public bool Predicate(EntityData<IEntityData> entity, EntityData<IEntityData> target)
        {
            if (!IsExecutable()) return true;

            if ((m_If.Execute(entity, out bool thisPredicate) && thisPredicate) &&
               (m_IfTarget.Execute(target, out bool targetPredicate) && targetPredicate))
            {
                return true;
            }

            for (int i = 0; i < m_ElseIf.Length; i++)
            {
                if (m_ElseIf[i].Predicate(entity, target)) return true;
            }
            return false;
        }
        public bool Execute(EntityData<IEntityData> entity, EntityData<IEntityData> target)
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

            for (int i = 0; i < m_ElseIf.Length; i++)
            {
                bool result = m_ElseIf[i].Execute(entity, target);
                if (result) return true;
            }
            return false;
        }
    }
}
