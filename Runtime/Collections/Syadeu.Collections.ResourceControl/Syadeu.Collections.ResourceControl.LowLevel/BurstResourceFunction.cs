// Copyright 2022 Ikina Games
// Author : Seung Ha Kim (Syadeu)
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

#if UNITY_2020_1_OR_NEWER
#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !POINT_DISABLE_CHECKS
#define DEBUG_MODE
#endif
#define UNITYENGINE

using Unity.Burst;

namespace Syadeu.Collections.ResourceControl.LowLevel
{
    [BurstCompile(CompileSynchronously = true)]
    internal static unsafe class BurstResourceFunction
    {
        //[BurstCompile(CompileSynchronously = true)]
        //public static void reserve_assets(UnsafeAssetBundleInfo* bundle, Hash* key, in int count)
        //{
        //    int index = bundle->index;

        //    for (int i = 0; i < count; i++)
        //    {
        //        UnsafeAssetInfo info = bundle->assets[key[i]];

        //        info.referencedCount--;

        //        bundle->assets[key[i]] = info;
        //    }
        //}
    }
}

#endif