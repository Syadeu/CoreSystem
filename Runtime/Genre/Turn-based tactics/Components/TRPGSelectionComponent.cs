using Syadeu.Collections;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.TurnTable
{
    public struct TRPGSelectionComponent : IEntityComponent
    {
        internal bool m_Holdable, m_Selected;
        internal FixedReferenceList16<TriggerAction> m_OnSelect, m_OnDeselect;

        public bool Selected => m_Selected;
    }
}