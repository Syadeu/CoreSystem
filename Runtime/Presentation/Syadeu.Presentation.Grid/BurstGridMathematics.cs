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
using Syadeu.Collections.Buffer.LowLevel;
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Syadeu.Presentation.Grid.LowLevel
{
    [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
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
            //if (BitArray64.IsExceedingRange(20, (uint)location.x, out ulong maxValue) ||
            //    BitArray64.IsExceedingRange(20, (uint)location.y, out maxValue) ||
            //    BitArray64.IsExceedingRange(20, (uint)location.z, out maxValue))
            //{
            //    UnityEngine.Debug.Log(location);
            //}
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
                maxX - 1,
                maxY,
                maxZ - 1
                );
        }
        [BurstCompile]
        public static void minMaxLocation(in UnsafeAllocator<int3> locations, int3* min, int3* max)
        {
            int length = locations.Length;

            int3
                target,
                tempMin = int.MaxValue,
                tempMax = int.MinValue;
            for (int i = 0; i < length; i++)
            {
                target = locations[i];

                tempMin = math.min(tempMin, target);
                tempMax = math.max(tempMax, target);
            }

            *min = tempMin;
            *max = tempMax;
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

            containLocation(in minY, in maxY, 0, in maxX, 0, in maxZ, in location, output);
        }
        [BurstCompile]
        public static void containLocation(
            [NoAlias] in int minY,
            [NoAlias] in int maxY,
            [NoAlias] in int minX,
            [NoAlias] in int maxX,
            [NoAlias] in int minZ,
            [NoAlias] in int maxZ,
            in int3 location, bool* output)
        {
            *output
                = location.x >= minX && location.x <= maxX &&
                location.y >= minY && location.y <= maxY &&
                location.z >= minZ && location.z <= maxZ;
        }

        [BurstCompile]
        public static void distanceBetweenLocation(in float cellSize, in int3 a, in int3 b, float* output)
        {
            int3 d = b - a;
            float
                x = d.x * cellSize,
                y = d.y * cellSize,
                z = d.z * cellSize,
                
                p = (x * x) + (y * y) + (z * z);

            *output = math.sqrt(p);
        }
        public static void distanceBetweenindex(in float cellSize, in ulong a, in int b, float* output)
        {
            // TODO : 인덱스만으로 거리계산이 안될까?
            int3 x, y;
            indexToLocation(in a, &x);
            indexToLocation(in a, &y);

            distanceBetweenLocation(in cellSize, in x, in y, output);
        }

        [BurstCompile]
        public static void getDirection(in int3 location, in Direction direction, int3* output)
        {
            int3 result = location;
            if ((direction & Direction.Up) == Direction.Up)
            {
                result.y += 1;
            }
            if ((direction & Direction.Down) == Direction.Down)
            {
                result.y -= 1;
            }
            if ((direction & Direction.Left) == Direction.Left)
            {
                result.x -= 1;
            }
            if ((direction & Direction.Right) == Direction.Right)
            {
                result.x += 1;
            }
            if ((direction & Direction.Forward) == Direction.Forward)
            {
                result.z += 1;
            }
            if ((direction & Direction.Backward) == Direction.Backward)
            {
                result.z -= 1;
            }

            *output = result;
        }
        [BurstCompile]
        public static void getMaxOutcoastLocationLength(in UnsafeAllocator<int3> locations, int* output)
        {
            int3 min, max;
            minMaxLocation(in locations, &min, &max);
            if (min.Equals(max))
            {
                *output = 0;
                return;
            }

            int
                relZ = max.z - min.z,
                relY = math.abs(max.y) - math.abs(min.y);

            *output = (relZ * 2) * relY;
        }
        /// <summary>
        /// <paramref name="locations"/> 의 테두리만 반환합니다.
        /// </summary>
        /// <param name="locations"></param>
        /// <param name="output"></param>
        [BurstCompile]
        public static void getOutcoastLocations(
            in AABB grid, in float cellSize,
            in UnsafeAllocator<GridIndex> locations, ref UnsafeFixedListWrapper<GridIndex> output)
        {
            output.Clear();
            //IndexComparer indexComparer = new IndexComparer();
            //locations.Sort(indexComparer);

            bool4 contains;
            int3x4 dir;
            for (int i = 0; i < locations.Length; i++)
            {
                int3 loc = locations[i].Location;

                dir.c0 = new int3(loc.x, loc.y, loc.z + 1); // down
                dir.c1 = new int3(loc.x, loc.y, loc.z - 1); // up
                dir.c2 = new int3(loc.x - 1, loc.y, loc.z); // left
                dir.c3 = new int3(loc.x + 1, loc.y, loc.z); // right

                containLocation(in grid, in cellSize, in dir.c0, &contains.x);
                containLocation(in grid, in cellSize, in dir.c1, &contains.y);
                containLocation(in grid, in cellSize, in dir.c2, &contains.z);
                containLocation(in grid, in cellSize, in dir.c3, &contains.w);

                if (!contains[0] || !contains[1] || !contains[2] || !contains[3])
                {
                    output.Add(locations[i]);
                }
                else if (!locations.Contains(dir.c0) || 
                    !locations.Contains(dir.c1) ||
                    !locations.Contains(dir.c2) ||
                    !locations.Contains(dir.c3))
                {
                    output.Add(locations[i]);
                }
            }
        }
        private struct IndexComparer : IComparer<GridIndex>
        {
            public int Compare(GridIndex xx, GridIndex yy)
            {
                int3 
                    x = xx.Location,
                    y = yy.Location;
                if (x.z == y.z)
                {
                    if (x.x == y.x)
                    {
                        if (x.y == y.y) return 0;

                        return x.y < y.y ? -1 : 1;
                    }

                    return x.x < y.x ? -1 : 1;
                }

                return x.z < y.z ? -1 : 1;
            }
        }
        private struct LocationComparer : IComparer<int3>
        {
            public int Compare(int3 x, int3 y)
            {
                if (x.z == y.z)
                {
                    if (x.x == y.x)
                    {
                        if (x.y == y.y) return 0;

                        return x.y < y.y ? -1 : 1;
                    }

                    return x.x < y.x ? -1 : 1;
                }
                
                return x.z < y.z ? -1 : 1;
            }
        }

        /// <summary>
        /// <paramref name="locations"/> 의 바깥 꼭지점을 연결하여 반환합니다.
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="cellSize"></param>
        /// <param name="locations"></param>
        /// <param name="output"></param>
        [SkipLocalsInit, BurstCompile]
        public static void getOutcoastLocationVertices(
            in AABB grid, in float cellSize,
            in UnsafeAllocator<GridIndex> locations, ref UnsafeFixedListWrapper<float3> output,
            in UnsafeAllocator<GridIndex> indicesMap)
        {
            output.Clear();

            float cellHalf = cellSize * .5f;

            float3
                upleft = new float3(-cellHalf, 0, cellHalf),
                upright = new float3(cellHalf, 0, cellHalf),
                downleft = new float3(-cellHalf, 0, -cellHalf),
                downright = new float3(cellHalf, 0, -cellHalf);

            UnsafeAllocator<GridIndex> map;
            if (!indicesMap.IsCreated)
            {
                map = locations;
            }
            else map = indicesMap;

            //IndexComparer comparer = new IndexComparer();
            //map.Sort(comparer);

            float3x2* tempBuffer = stackalloc float3x2[locations.Length * 4];
            UnsafeFixedListWrapper<float3x2> list
                = new UnsafeFixedListWrapper<float3x2>(tempBuffer, locations.Length * 4);

            int3 directional;
            float3 gridPos;
            bool contains;
            //GridIndex temp;
            for (int i = 0; i < locations.Length; i++)
            {
                indexToPosition(in grid, in cellSize, locations[i].Index, &gridPos);

                getDirection(locations[i].Location, Direction.Right, &directional);
                containLocation(in grid, in cellSize, in directional, &contains);
                //temp = new GridIndex(locations[i].m_CheckSum, directional);
                if (!contains || !map.Contains(directional))
                {
                    list.Add(new float3x2(
                        gridPos + upright,
                        gridPos + downright
                        ));
                }

                // Down
                getDirection(locations[i].Location, Direction.Forward, &directional);
                containLocation(in grid, in cellSize, in directional, &contains);
                //temp = new GridIndex(locations[i].m_CheckSum, directional);
                if (!contains || !map.Contains(directional))
                {
                    list.Add(new float3x2(
                        gridPos + downright,
                        gridPos + downleft
                        ));
                }

                getDirection(locations[i].Location, Direction.Left, &directional);
                containLocation(in grid, in cellSize, in directional, &contains);
                //temp = new GridIndex(locations[i].m_CheckSum, directional);
                if (!contains || !map.Contains(directional))
                {
                    list.Add(new float3x2(
                        gridPos + downleft,
                        gridPos + upleft
                        ));
                }

                getDirection(locations[i].Location, Direction.Backward, &directional);
                containLocation(in grid, in cellSize, in directional, &contains);
                //temp = new GridIndex(locations[i].m_CheckSum, directional);
                if (!contains || !map.Contains(directional))
                {
                    list.Add(new float3x2(
                        gridPos + upleft,
                        gridPos + upright
                        ));
                }
            }

            float3x2 current = list.Last;
            list.RemoveAtSwapback(list.Count - 1);
            output.Add(current.c0);
            float3 next = current.c1;

            while (FindFloat3x2(ref list, in next, out current))
            {
                if (!output.Last.Equals(current.c0))
                {
                    output.Add(current.c0);
                }

                next = current.c1;
            }
        }
        [BurstCompile]
        private static bool FindFloat3x2(ref UnsafeFixedListWrapper<float3x2> list, in float3 next, out float3x2 found)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].c0.Equals(next) || list[i].c1.Equals(next))
                {
                    found = list[i];
                    list.RemoveAtSwapback(i);
                    return true;
                }
            }
            found = 0;
            return false;
        }
    }
}
