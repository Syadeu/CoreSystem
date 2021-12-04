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

using Newtonsoft.Json.Utilities;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Syadeu.Collections
{
    [Preserve]
    internal static class AotResolver
    {
        public static void AotListCodeGenerator()
        {
            AotHelper.EnsureList<int4>();
            AotHelper.EnsureList<int3>();
            AotHelper.EnsureList<int2>();
            AotHelper.EnsureList<int>();
            AotHelper.EnsureList<float4>();
            AotHelper.EnsureList<float3>();
            AotHelper.EnsureList<float2>();
            AotHelper.EnsureList<float>();
            AotHelper.EnsureList<bool4>();
            AotHelper.EnsureList<bool3>();
            AotHelper.EnsureList<bool2>();
            AotHelper.EnsureList<bool>();
            AotHelper.EnsureList<float4x4>();
            AotHelper.EnsureList<float4x3>();
            AotHelper.EnsureList<float4x2>();
            AotHelper.EnsureList<int4x4>();
            AotHelper.EnsureList<int4x3>();
            AotHelper.EnsureList<int4x2>();
            AotHelper.EnsureList<float3x4>();
            AotHelper.EnsureList<float3x3>();
            AotHelper.EnsureList<float3x2>();
            AotHelper.EnsureList<int3x4>();
            AotHelper.EnsureList<int3x3>();
            AotHelper.EnsureList<int3x2>();

            AotHelper.EnsureList<IFixedReference>();
            AotHelper.EnsureList<FixedReference>();
            AotHelper.EnsureList<IEntityDataID>();
            //AotHelper.EnsureList<EntityID>();
            AotHelper.EnsureList<EntityShortID>();
            AotHelper.EnsureList<IInstance>();
            AotHelper.EnsureList<Instance>();
            AotHelper.EnsureList<InstanceID>();
            AotHelper.EnsureList<IObject>();
        }
    }
}
