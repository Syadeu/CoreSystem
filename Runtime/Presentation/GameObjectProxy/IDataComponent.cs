using System;
using Unity.Mathematics;

namespace Syadeu.Presentation
{
    internal interface IDataComponent : IEquatable<IDataComponent>
    {
        /// <summary>
        /// x = prefabIdx, y = internalListIdx, z = DataComponentType
        /// </summary>
        int3 Idx { get; }
        DataComponentType Type { get; }

        IReadOnlyTransform transform { get; }
    }
}
