﻿#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using Syadeu.Presentation.Entities;
using Unity.Collections;

namespace Syadeu.Presentation.Actions
{
    public struct FixedLogicTriggerAction
    {
        private FixedString128Bytes m_Name;

        private FixedReferenceList64<TriggerPredicateAction> m_If;
        private FixedReferenceList64<TriggerPredicateAction> m_IfTarget;

        private FixedReferenceList64<TriggerAction> m_Do;
        private FixedReferenceList64<TriggerAction> m_DoTarget;

        public FixedString128Bytes Name => m_Name;

        internal FixedLogicTriggerAction(
            string name,
            Reference<TriggerPredicateAction>[] @if,
            Reference<TriggerPredicateAction>[] ifTarget,

            Reference<TriggerAction>[] @do,
            Reference<TriggerAction>[] doTarget
            )
        {
            m_Name = name;
            m_If = @if.ToFixedList64();
            m_IfTarget = ifTarget.ToFixedList64();
            m_Do = @do.ToFixedList64();
            m_DoTarget = doTarget.ToFixedList64();
        }

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

            return false;
        }
        public bool Schedule(EntityData<IEntityData> entity, EntityData<IEntityData> target)
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
