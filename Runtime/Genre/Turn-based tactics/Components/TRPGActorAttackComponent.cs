using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using System.Collections.Generic;

namespace Syadeu.Presentation.TurnTable
{
    public struct TRPGActorAttackComponent : IEntityComponent
    {
        internal bool m_HasTarget;
        internal List<Entity<IEntity>>.Enumerator m_CurrentTargets;
        internal int m_TargetCount;

        public int m_AttackRange;
        public int m_ConsumeAP;

        public List<Entity<IEntity>>.Enumerator CurrentTargets => m_CurrentTargets;
        public int TargetCount => m_TargetCount;
    }
}