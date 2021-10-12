using Syadeu.Collections;
using Syadeu.Presentation.Components;

namespace Syadeu.Presentation.Entities
{
    public struct UIObjectCanvasGroupComponent : IEntityComponent
    {
        internal bool m_Enabled;

        public bool Enabled => m_Enabled;
    }
}
