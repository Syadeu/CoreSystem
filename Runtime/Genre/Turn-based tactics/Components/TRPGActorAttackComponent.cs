﻿#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
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

        private FixedList512Bytes<InstanceID> m_Targets;
        private int m_CurrentTargetIndex;
        private InstanceID m_CurrentTarget;

        public int TargetCount => m_Targets.Length;

        public void InitializeTargets(FixedList512Bytes<InstanceID> targets)
        {
            m_Targets = targets;
            if (!m_Targets.Contains(m_CurrentTarget))
            {
                m_CurrentTarget = InstanceID.Empty;
            }
        }
        public FixedList512Bytes<InstanceID> GetTargets() => m_Targets;
        public InstanceID GetTargetAt(int index) => m_Targets[index];
        public Entity<T> GetTargetAt<T>(int index) where T : class, IEntityData
        {
#if DEBUG_MODE
            if (m_Targets.Length == 0)
            {
                CoreSystem.Logger.LogError(LogChannel.Entity,
                    $"Doesn\'t have any targets.");

                return Entity<T>.Empty;
            }
            else if (m_Targets.Length >= index || index < 0)
            {
                CoreSystem.Logger.LogError(LogChannel.Entity,
                    $"Target range is out of range.");

                return Entity<T>.Empty;
            }
#endif
            return m_Targets[index].GetEntity<T>();
        }

        public void SetTarget(int i)
        {
            m_CurrentTarget = m_Targets[i];
            m_CurrentTargetIndex = i;
        }
        public InstanceID GetTarget()
        {
            if (!m_Targets.Contains(m_CurrentTarget))
            {
                m_CurrentTarget = InstanceID.Empty;
            }

            return m_CurrentTarget;
        }
        public int GetTargetIndex()
        {
            if (!m_Targets.Contains(m_CurrentTarget))
            {
                m_CurrentTarget = InstanceID.Empty;
                m_CurrentTargetIndex = -1;
            }

            return m_CurrentTargetIndex;
        }
    }
}