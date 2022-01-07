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
using Unity.Collections;
using Unity.Mathematics;

namespace Syadeu.Presentation.Grid.LowLevel
{
    [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    public static unsafe class BurstGridMathematics
    {
        #region Indexing

        // https://www.koreascience.or.kr/article/JAKO201113663898554.pdf
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
                y = Convert.ToInt32(math.round(position.y) / cellSize);

            *output = new int3(x, y, z);
        }
        public static void positionToIndex(in AABB aabb, in float cellSize, in float3 position, ulong* output)
        {
            int3 location;
            positionToLocation(in aabb, in cellSize, in position, &location);
            locationToIndex(in location, output);
        }
        [BurstCompile]
        public static void locationToIndex(in int3 location, ulong* output)
        {
            BitArray64 bits = new BitArray64();
            bits.SetValue(0, (uint)location.x, 20);
            bits.SetValue(20, (uint)location.z, 20);
            bits.SetValue(40, (uint)math.abs(location.y), 20);

            bits[62] = location.y < 0;
            bits[63] = true;

            *output = bits.Value;
        }
        [BurstCompile]
        public static void indexToLocation(in ulong index, int3* output)
        {
            BitArray64 bits = index;

            int
                x = (int)bits.ReadValue(0, 20),
                z = (int)bits.ReadValue(20, 20),
                y = (int)bits.ReadValue(40, 20);

            if (bits[62]) y *= -1;

            *output = new int3(x, y, z);
        }
        public static void indexToPosition(in AABB aabb, in float cellSize, in ulong index, float3* output)
        {
            int3 location;
            indexToLocation(in index, &location);
            locationToPosition(in aabb, in cellSize, in location, output);
        }
        [BurstCompile]
        public static void locationToPosition(in AABB aabb, in float cellSize, in int3 location, float3* output)
        {
            float
                half = cellSize * .5f,
                x = aabb.min.x + half + (cellSize * location.x),
                z = aabb.max.z - half - (cellSize * location.z);

            *output = new float3(x, location.y * cellSize, z);
        }

        [BurstCompile]
        public static void minMaxLocation(in AABB aabb, in float cellSize, int3* min, int3* max)
        {
            float3
                _min = aabb.min,
                _max = aabb.max;

            float
                half = cellSize * .5f;
            int
                // Left Up
                minY = Convert.ToInt32(math.round(_min.y / cellSize)),

                // Right Down
                maxX = math.abs(Convert.ToInt32((aabb.size.x - half) / cellSize)),
                maxY = Convert.ToInt32(math.round(_max.y / cellSize)),
                maxZ = math.abs(Convert.ToInt32((aabb.size.z + half) / cellSize));

            *min = new int3(
                0,
                minY,
                0
                );
            *max = new int3(
                maxX,
                maxY,
                maxZ
                );
        }

        #endregion

        public static void indexToAABB(in AABB aabb, in float cellSize, in ulong index, AABB* output)
        {
            float3 position;
            indexToPosition(in aabb, in cellSize, in index, &position);

            *output = new AABB(position, cellSize);
        }
        [BurstCompile]
        public static void indexToAABB(in AABB aabb, in float cellSize, [NoAlias] in ulong min, [NoAlias] in ulong max, AABB* output)
        {
            float3 minPos, maxPos;
            indexToPosition(in aabb, in cellSize, in min, &minPos);
            indexToPosition(in aabb, in cellSize, in max, &maxPos);

            AABB temp = new AABB(minPos, cellSize);
            temp.Encapsulate(new AABB(maxPos, cellSize));

            *output = temp;
        }

        [BurstCompile]
        public static void aabbToIndices(in AABB grid, in float cellSize, in AABB aabb, 
            FixedList4096Bytes<ulong>* output)
        {
            int3 minLocation, maxLocation, tempMin, tempMax;

            {
                positionToLocation(in grid, in cellSize, aabb.min, &tempMin);
                positionToLocation(in grid, in cellSize, aabb.max, &tempMax);

                minLocation = math.min(tempMin, tempMax);
                maxLocation = math.max(tempMin, tempMax);

                if (minLocation.Equals(maxLocation))
                {
                    ulong index;
                    locationToIndex(in minLocation, &index);
                    (*output).Add(index);
                    return;
                }
            }

            for (int y = minLocation.y; y <= maxLocation.y; y++)
            {
                for (int x = minLocation.x; x <= maxLocation.x; x++)
                {
                    for (int z = minLocation.z; z <= maxLocation.z; z++)
                    {
                        ulong index;
                        locationToIndex(new int3(x, y, z), &index);

                        (*output).Add(index);
                    }
                }
            }
        }

        [BurstCompile]
        public static void containIndex(in AABB aabb, in float cellSize, in ulong index, bool* output)
        {
            int3 location;
            indexToLocation(in index, &location);
            containLocation(in aabb, in cellSize, in location, output);
        }
        [BurstCompile]
        public static void containLocation(in AABB aabb, in float cellSize, in int3 location, bool* output)
        {
            float3
                _size = aabb.size,
                _min = aabb.min,
                _max = aabb.max;
            float
                half = cellSize * .5f;
            int
                // Left Up
                minY = Convert.ToInt32(math.round(_min.y)),

                // Right Down
                maxX = math.abs(Convert.ToInt32((_size.x - half) / cellSize)),
                maxY = Convert.ToInt32(math.round(_max.y)),
                maxZ = math.abs(Convert.ToInt32((_size.z + half) / cellSize));

            containLocation(in minY, in maxY, in maxX, in maxZ, in location, output);
        }
        [BurstCompile]
        public static void containLocation(
            [NoAlias] in int minY,
            [NoAlias] in int maxY,
            [NoAlias] in int maxX,
            [NoAlias] in int maxZ,
            in int3 location, bool* output)
        {
            *output
                = location.x >= 0 && location.x <= maxX &&
                location.y >= minY && location.y <= maxY &&
                location.z >= 0 && location.z <= maxZ;
        }

        [BurstCompile]
        public static void distanceBetweenLocation(in AABB aabb, in float cellSize, in int3 a, in int3 b, float* output)
        {
            int3 d = b - a;
            float
                x = d.x * cellSize,
                y = d.y * cellSize,
                z = d.z * cellSize,
                
                p = (x * x) + (y * y) + (z * z);

            *output = math.sqrt(p);
        }
        public static void distanceBetweenindex(in AABB aabb, in float cellSize, in ulong a, in int b, float* output)
        {
            // TODO : 인덱스만으로 거리계산이 안될까?
            int3 x, y;
            indexToLocation(in a, &x);
            indexToLocation(in a, &y);

            distanceBetweenLocation(in aabb, in cellSize, in x, in y, output);
        }
    }
}
