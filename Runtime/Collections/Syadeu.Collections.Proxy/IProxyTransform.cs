// Copyright 2021 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Syadeu.Collections;
using Syadeu.Collections.Proxy;
using System;
using Unity.Mathematics;

namespace Syadeu.Collections.Proxy
{
    /// <summary>
    /// 프록시 트랜스폼의 인터페이스입니다.
    /// </summary>
    /// <remarks>
    /// 참조: <seealso cref="ProxyTransform"/>
    /// </remarks>
    public interface IProxyTransform : ITransform
    {
#pragma warning disable IDE1006 // Naming Styles
        int index { get; }
        int generation { get; }

        bool enableCull { get; }
        bool isVisible { get; }

        bool hasProxy { get; }
        bool hasProxyQueued { get; }
        IProxyMonobehaviour proxy { get; }
        bool isDestroyed { get; }
        bool isDestroyQueued { get; }
        IPrefabReference prefab { get; }

        float3 center { get; }
        float3 size { get; }
#pragma warning restore IDE1006 // Naming Styles

        void Synchronize(SynchronizeOption option);

        [Flags]
        public enum SynchronizeOption
        {
            Position = 0b001,
            Rotation = 0b010,
            Scale = 0b100,

            TR = 0b011,
            TRS = 0b111
        }
    }
}
