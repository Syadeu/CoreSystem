#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Presentation.Entities;
using System;
using Unity.Collections;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    /// <summary>
    /// Use <see cref="FixedLogicTriggerAction"/> in component can be gather <see cref="GetFixedLogicTriggerAction"/>
    /// </summary>
    [Serializable]
    public sealed class LogicTriggerAction
    {
        [JsonProperty(Order = 0, PropertyName = "Name")] private string m_Name = string.Empty;

        [JsonProperty(Order = 1, PropertyName = "If")]
        private Reference<TriggerPredicateAction>[] m_If = Array.Empty<Reference<TriggerPredicateAction>>();
        [JsonProperty(Order = 2, PropertyName = "If Target")]
        private Reference<TriggerPredicateAction>[] m_IfTarget = Array.Empty<Reference<TriggerPredicateAction>>();

        [Space]
        [JsonProperty(Order = 3, PropertyName = "Do")]
        private Reference<TriggerAction>[] m_Do = Array.Empty<Reference<TriggerAction>>();
        [JsonProperty(Order = 4, PropertyName = "Do Target")]
        private Reference<TriggerAction>[] m_DoTarget = Array.Empty<Reference<TriggerAction>>();

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

    public struct FixedLogicTriggerAction8
    {
        private FixedLogicTriggerAction
            a, b, c, d,
            e, f, g, h;

        private int m_Length;

        public int Length => m_Length;
        public FixedLogicTriggerAction this[int index]
        {
            get
            {
                if (index >= m_Length)
                {
                    throw new ArgumentOutOfRangeException();
                }

                switch (index)
                {
                    case 0: return a;
                    case 1: return b;
                    case 2: return c;
                    case 3: return d;
                    case 4: return e;
                    case 5: return f;
                    case 6: return g;
                    case 7: return h;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            set
            {
                if (index >= m_Length)
                {
                    throw new ArgumentOutOfRangeException();
                }

                switch (index)
                {
                    case 0: { a = value; return; }
                    case 1: { b = value; return; }
                    case 2: { c = value; return; }
                    case 3: { d = value; return; }
                    case 4: { e = value; return; }
                    case 5: { f = value; return; }
                    case 6: { g = value; return; }
                    case 7: { h = value; return; }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public FixedLogicTriggerAction8(LogicTriggerAction[] logicTriggers)
        {
            if (logicTriggers.Length > 7)
            {
                throw new ArgumentOutOfRangeException();
            }

            this = default(FixedLogicTriggerAction8);

            m_Length = logicTriggers.Length;
            for (int i = 0; i < logicTriggers.Length; i++)
            {
                this[i] = logicTriggers[i].GetFixedLogicTriggerAction();
            }
        }
    }

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
