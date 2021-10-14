using Syadeu.Collections;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.TurnTable
{
    public struct TRPGSelectionComponent : IEntityComponent
    {
        internal bool m_Selected;

        public bool Selected => m_Selected;
    }
}