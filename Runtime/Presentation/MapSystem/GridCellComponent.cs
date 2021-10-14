using Syadeu.Collections;
using Syadeu.Presentation.Components;

namespace Syadeu.Presentation.Map
{
    public struct GridCellComponent : IEntityComponent
    {
        public GridPosition m_GridPosition;
        public bool m_IsDetectionCell;
    }
}
