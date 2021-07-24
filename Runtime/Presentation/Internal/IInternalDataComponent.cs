﻿using Syadeu.Database;
using System;
using Unity.Mathematics;

namespace Syadeu.Presentation
{
    internal interface IInternalDataComponent : IEquatable<IInternalDataComponent>
    {
        Hash GameObject { get; }
        /// <summary>
        /// x = prefabIdx, y = internalListIdx, z = DataComponentType
        /// </summary>
        Hash Idx { get; }
        bool HasProxyObject { get; }
        bool ProxyRequested { get; }

        DataTransform transform { get; }
    }
}