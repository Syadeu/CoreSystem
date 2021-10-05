using System;
using Unity.Collections.LowLevel.Unsafe;

namespace Syadeu.Presentation.Components
{
    public struct HashGenerator
    {
        private static readonly Unity.Mathematics.Random m_Random;

        static HashGenerator()
        {
            m_Random = new Unity.Mathematics.Random();
            m_Random.InitState();
        }

        public static int CreateHashCode() => m_Random.NextInt(int.MinValue, int.MaxValue);
    }

    [Obsolete("In development")]
    internal unsafe struct ComponentBlock
    {
        private UnsafePtrList<ComponentBuffer> m_Buffers;


    }
}
