using Syadeu.Collections;
using Unity.Collections;

namespace Syadeu.Presentation.Actor
{
    public struct ActorOverlayUIComponent : IEntityComponent
    {
        internal FixedList512Bytes<Reference<ActorOverlayUIEntry>> m_OpenedUI;
    }
}
