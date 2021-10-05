using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using System.Collections.Generic;
using Unity.Collections;

namespace Syadeu.Presentation.TurnTable
{
    public struct TRPGActorAttackComponent : IEntityComponent
    {
        internal bool m_HasTarget;
        //internal NativeArray<Entity<IEntity>>.Enumerator m_CurrentTargets;
        internal int m_TargetCount;

        public int m_AttackRange;
        public int m_ConsumeAP;

        //public NativeArray<Entity<IEntity>>.Enumerator CurrentTargets => m_CurrentTargets;
        public int TargetCount => m_TargetCount;
    }
}