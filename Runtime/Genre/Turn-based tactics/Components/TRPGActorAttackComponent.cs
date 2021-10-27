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
        public int m_ConsumeAP;

        public FixedList512Bytes<EntityID> m_Targets;

        public int TargetCount => m_Targets.Length;
    }
}