using Syadeu.Collections;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using System.Collections.Generic;
using Unity.Collections;

namespace Syadeu.Presentation.TurnTable
{
    public struct TRPGActorAttackComponent : IEntityComponent
    {
        public int m_SearchRange;

        private FixedList512Bytes<EntityID> m_Targets;
        private int m_CurrentTargetIndex;
        private EntityID m_CurrentTarget;

        public int TargetCount => m_Targets.Length;

        public void InitializeTargets(FixedList512Bytes<EntityID> targets)
        {
            m_Targets = targets;
            if (!m_Targets.Contains(m_CurrentTarget))
            {
                m_CurrentTarget = EntityID.Empty;
            }
        }
        public FixedList512Bytes<EntityID> GetTargets() => m_Targets;
        public EntityID GetTargetAt(int index) => m_Targets[index];

        public void SetTarget(int i)
        {
            m_CurrentTarget = m_Targets[i];
            m_CurrentTargetIndex = i;
        }
        public EntityID GetTarget()
        {
            if (!m_Targets.Contains(m_CurrentTarget))
            {
                m_CurrentTarget = EntityID.Empty;
            }

            return m_CurrentTarget;
        }
        public int GetTargetIndex()
        {
            if (!m_Targets.Contains(m_CurrentTarget))
            {
                m_CurrentTarget = EntityID.Empty;
                m_CurrentTargetIndex = -1;
            }

            return m_CurrentTargetIndex;
        }
    }
}