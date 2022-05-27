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

using System;
using Unity.Mathematics;

namespace Syadeu.Collections.Proxy
{
    /// <summary>
    /// <see cref="Entities.Entity{T}"/>의 트랜스폼입니다.
    /// </summary>
    public interface ITransform : IEquatable<ITransform>
    {
#pragma warning disable IDE1006 // Naming Styles
        bool hasProxy { get; }
        UnityEngine.Object proxy { get; }

        float3 position { get; set; }
        quaternion rotation { get; set; }
        float3 eulerAngles { get; set; }
        float3 scale { get; set; }

        float3 right { get; }
        float3 up { get; }
        float3 forward { get; }

        float4x4 localToWorldMatrix { get; }
        float4x4 worldToLocalMatrix { get; }

        AABB aabb { get; }
#pragma warning restore IDE1006 // Naming Styles

        void Destroy();

        int GetHashCode();
    }
}
