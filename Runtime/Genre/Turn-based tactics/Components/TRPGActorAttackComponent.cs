#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

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
        public EntityData<T> GetTargetAt<T>(int index) where T : class, IEntityData
        {
#if DEBUG_MODE
            if (m_Targets.Length == 0)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Doesn\'t have any targets.");

                return EntityData<T>.Empty;
            }
            else if (m_Targets.Length >= index || index < 0)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Target range is out of range.");

                return EntityData<T>.Empty;
            }
#endif
            return m_Targets[index].GetEntityData<T>();
        }

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