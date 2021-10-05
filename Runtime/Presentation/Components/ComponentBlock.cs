using System;
using Unity.Collections.LowLevel.Unsafe;

namespace Syadeu.Presentation.Components
{
    [Obsolete("In development")]
    internal unsafe struct ComponentBlock
    {
        private UnsafePtrList<ComponentBuffer> m_Buffers;


    }
}
