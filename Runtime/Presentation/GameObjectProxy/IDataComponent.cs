﻿using Syadeu.Database;
using System;
using Unity.Mathematics;

namespace Syadeu.Presentation
{
    internal interface IDataComponent : IEquatable<IDataComponent>
    {
        /// <summary>
        /// x = prefabIdx, y = internalListIdx, z = DataComponentType
        /// </summary>
        Hash Idx { get; }
        DataComponentType Type { get; }
        bool HasProxyObject { get; }
        bool ProxyRequested { get; }

        IReadOnlyTransform transform { get; }
    }
}
