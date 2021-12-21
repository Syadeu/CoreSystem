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

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif


using Syadeu.Collections;
using System;
using Unity.Burst;
using Unity.Mathematics;

namespace Syadeu.Presentation.Grid
{
    public sealed class WorldGridSystem : PresentationSystemEntity<WorldGridSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;
    }

    [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    public unsafe static class BurstGridMathematics
    {
        [BurstCompile]
        public static void positionToLocation(in AABB aabb, in float cellSize, in float3 position, int3* output)
        {
            float
                half = cellSize * .5f,
                firstCenterX = aabb.min.x + half,
                firstCenterZ = aabb.max.z - half;

            int
                x = math.abs(Convert.ToInt32((position.x - firstCenterX) / cellSize)),
                z = math.abs(Convert.ToInt32((position.z - firstCenterZ) / cellSize)),
                y = Convert.ToInt32(math.round(position.y));

            *output = new int3(x, y, z);
        }
        [BurstCompile]
        public static void locationToIndex(in AABB aabb, in float cellSize, in int3 location, int* output)
        {
            int
                zSize = Convert.ToInt32(math.floor(aabb.size.z / cellSize)),
                calculated = zSize * location.z + location.x;
            
            if (location.y == 0)
            {
                *output = calculated;
                *output ^= 397;
                return;
            }

            int
                xSize = Convert.ToInt32(math.floor(aabb.size.x / cellSize)),
                dSize = xSize * zSize;

            *output = calculated + (dSize * math.abs(location.y));
            *output ^= 397;

            if (location.y < 0)
            {
                *output *= -1;
            }
        }
        [BurstCompile]
        public static void indexToLocation(in AABB aabb, in float cellSize, in int index, int3* output)
        {
            if (index == 0)
            {
                *output = int3.zero;
                return;
            }
            
            int
                temp = math.abs(index) ^ 397,
                xSize = Convert.ToInt32(math.floor(aabb.size.x / cellSize)),
                zSize = Convert.ToInt32(math.floor(aabb.size.z / cellSize)),
                dSize = xSize * zSize,

                y = temp / dSize,
                calculated = temp % dSize;

            if (index < 0) y *= -1;

            if (calculated == 0)
            {
                *output = new int3(0, y, 0);
                return;
            }

            int
                z = calculated / zSize,
                x = calculated - (zSize * z);

            *output = new int3(x, y, z);
        }
        [BurstCompile]
        public static void locationToPosition(in AABB aabb, in float cellSize, in int3 location, float3* output)
        {
            float
                half = cellSize * .5f,
                targetX = aabb.min.x + half + (cellSize * location.x),
                //targetY = aabb.center.y,
                targetZ = aabb.max.z - half - (cellSize * location.z);

            *output = new float3(targetX, location.y, targetZ);
        }
    }
}
